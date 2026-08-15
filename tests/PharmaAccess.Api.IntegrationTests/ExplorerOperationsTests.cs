using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Llm;
using Xunit;

namespace PharmaAccess.Api.IntegrationTests;

public sealed class ExplorerOperationsTests
{
    [Fact]
    public async Task Disabled_research_is_ready_without_private_resources()
    {
        var result = await new Api.ExplorerPreflight(true, false, "", "", "", new MissingGemini()).CheckAsync();
        Assert.Equal(Api.ExplorerComponentStatus.Ready, result.Status);
        Assert.Equal(Api.ExplorerComponentStatus.Disabled, result.Components["VerifiedModel"]);
        Assert.Equal(Api.ExplorerComponentStatus.Degraded, result.Components["GeminiExplanation"]);
    }

    [Fact]
    public async Task Production_and_missing_or_mismatched_resources_fail_closed_without_path_details()
    {
        var production = await new Api.ExplorerPreflight(false, true, "private-root/snapshot.csv", "private-root/r2b.csv", "private-root/bundle", new MissingGemini()).CheckAsync();
        Assert.Equal(Api.ExplorerComponentStatus.Unavailable, production.Status);
        Assert.DoesNotContain("private-root", JsonSerializer.Serialize(production), StringComparison.OrdinalIgnoreCase);

        var missing = await new Api.ExplorerPreflight(true, true, "missing-snapshot", "missing-r2b", "missing-bundle", new MissingGemini()).CheckAsync();
        Assert.Equal(Api.ExplorerComponentStatus.Unavailable, missing.Status);
        Assert.Equal(Api.ExplorerComponentStatus.Unavailable, missing.Components["VerifiedModel"]);
        Assert.Equal(Api.ExplorerComponentStatus.Unavailable, missing.Components["HistoricalReplay"]);
        Assert.Equal(Api.ExplorerComponentStatus.Unavailable, missing.Components["DynamicResearchSource"]);
    }

    [Fact]
    public async Task Readiness_exposes_safe_component_names_and_optional_Gemini_degradation()
    {
        using var host = await Host(services =>
        {
            services.RemoveAll<Api.IExplorerPreflight>();
            services.AddSingleton<Api.IExplorerPreflight>(new FixedPreflight(Api.ExplorerComponentStatus.Degraded));
        });
        using var response = await host.GetTestClient().GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("VerifiedModel", body);
        Assert.Contains("GeminiExplanation", body);
        Assert.DoesNotContain("ConnectionString", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-root", body);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Gemini_failures_are_sanitized_and_do_not_affect_prediction(bool timeout)
    {
        using var host = await Host(services =>
        {
            services.RemoveAll<IHistoricalPredictionReplayService>();
            services.RemoveAll<IHistoricalReplayExplanationService>();
            services.AddSingleton<IHistoricalPredictionReplayService, Replay>();
            services.AddScoped<IHistoricalReplayExplanationService>(_ => new FailingExplanation(timeout));
        }, enabled: true);
        using var client = host.GetTestClient();
        var content = () => new StringContent("{\"genericLaunchId\":291,\"asOfQuarter\":20244}", Encoding.UTF8, "application/json");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/drug-state-prediction-explorer/historical-replay", content())).StatusCode);
        var explanation = await client.PostAsync("/api/v1/drug-state-prediction-explorer/historical-replay/explain", content());
        var body = await explanation.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, explanation.StatusCode);
        Assert.Contains("Prediction results are unaffected", body);
        Assert.DoesNotContain("secret-key", body);
        Assert.DoesNotContain("private-root", body);
        Assert.DoesNotContain("429", body);
    }

    [Fact]
    public async Task Concurrent_scoring_is_deterministic()
    {
        using var host = await Host(services =>
        {
            services.RemoveAll<IHistoricalPredictionReplayService>();
            services.AddSingleton<IHistoricalPredictionReplayService, Replay>();
        }, enabled: true);
        using var client = host.GetTestClient();
        var requests = Enumerable.Range(0, 12).Select(_ => client.PostAsync("/api/v1/drug-state-prediction-explorer/historical-replay",
            new StringContent("{\"genericLaunchId\":291,\"asOfQuarter\":20244}", Encoding.UTF8, "application/json")));
        var responses = await Task.WhenAll(requests);
        Assert.All(responses, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));
        var bodies = await Task.WhenAll(responses.Select(x => x.Content.ReadAsStringAsync()));
        Assert.Single(bodies.Distinct(StringComparer.Ordinal));
    }

    private static async Task<IHost> Host(Action<IServiceCollection> configure, bool enabled = false)
    {
        var values = new Dictionary<string, string?>
        {
            ["ModelServing:ResearchDevelopmentModeEnabled"] = enabled.ToString(),
            ["ModelServing:ResearchReplaySnapshotPath"] = "missing-snapshot",
            ["ModelServing:R2bVerificationPath"] = "missing-r2b",
            ["ModelServing:ServingBundlePath"] = "missing-bundle",
            ["ConnectionStrings:PharmaAccess"] = "",
            ["Gemini:ApiKey"] = ""
        };
        return await new HostBuilder().ConfigureAppConfiguration(x => x.AddInMemoryCollection(values)).ConfigureWebHost(x =>
            x.UseEnvironment("Development").UseTestServer().UseStartup<Api.Startup>().ConfigureTestServices(configure)).StartAsync();
    }

    private sealed class MissingGemini : ILanguageModelClient
    {
        public string Provider => "Google Gemini";
        public string Model => "test";
        public bool IsAvailable => false;
        public Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FixedPreflight(Api.ExplorerComponentStatus status) : Api.IExplorerPreflight
    {
        public Task<Api.ExplorerPreflightResult> CheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(new Api.ExplorerPreflightResult(status,
            new Dictionary<string, Api.ExplorerComponentStatus> { ["VerifiedModel"] = Api.ExplorerComponentStatus.Ready, ["HistoricalReplay"] = Api.ExplorerComponentStatus.Ready, ["DynamicResearchSource"] = Api.ExplorerComponentStatus.Ready, ["GeminiExplanation"] = Api.ExplorerComponentStatus.Degraded }));
    }

    private sealed class FailingExplanation(bool timeout) : IHistoricalReplayExplanationService
    {
        public Task<HistoricalReplayExplanationResponse> ExplainAsync(HistoricalReplayExplanationRequest request, CancellationToken cancellationToken = default) =>
            timeout ? throw new TaskCanceledException("timeout private-root/snapshot.csv secret-key") : throw new HttpRequestException("429 secret-key private-root/snapshot.csv");
    }

    private sealed class Replay : IHistoricalPredictionReplayService
    {
        public Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<HistoricalReplayLaunch>>([new(291, "Verified launch")]);
        public Task<IReadOnlyList<int>> GetQuartersAsync(int genericLaunchId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<int>>([20244]);
        public Task<HistoricalReplayResponse> ReplayAsync(HistoricalReplayRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new HistoricalReplayResponse(HistoricalReplayMode.Name, 291, "Verified launch", 20244, 20251, "model-v1", "safe-hash", "dataset-v1", 2, "features-v1", 1, .08, 1, [new(6, "CA", "California", .027497245f, false, 1)], ["Research only"], ModelApprovalStatus.ValidationSelected, false, false));
    }
}
