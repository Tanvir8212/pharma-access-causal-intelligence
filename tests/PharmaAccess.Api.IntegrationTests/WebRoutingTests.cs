using System.Diagnostics;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Llm;
using PharmaAccess.Web;
using Xunit;

namespace PharmaAccess.Api.IntegrationTests;

public sealed class WebRoutingTests
{
    [Fact]
    public async Task Research_assistant_renders_readable_text()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, NullLoggerFactory.Instance);

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<Web.Components.ResearchAssistant>()).ToHtmlString());

        Assert.Contains("Research Assistant", html);
        Assert.Contains("Ask a question", html);
        Assert.Contains("type=\"button\"", html);
        Assert.Contains(">Ask</button>", html);
        Assert.NotNull(typeof(Web.Components.ResearchAssistant).GetMethod(
            "AskAsync", BindingFlags.Instance | BindingFlags.NonPublic));
    }

    [Fact]
    public async Task Initial_page_does_not_wait_for_optional_dependencies()
    {
        var delayedGovernance = new NeverCompletingGovernanceServices();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var host = await new HostBuilder()
            .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:Mode"] = "DevelopmentHeader",
                    ["ConnectionStrings:PharmaAccess"] = null,
                    ["Gemini:ApiKey"] = null
                }))
            .ConfigureWebHost(webBuilder => webBuilder
                .UseEnvironment("Development")
                .UseTestServer()
                .UseStartup<Web.Startup>()
                .ConfigureTestServices(services =>
                {
                    services.AddDataProtection().UseEphemeralDataProtectionProvider();
                    services.RemoveAll<IDocumentIngestionService>();
                    services.AddSingleton<IDocumentIngestionService, ThrowingDocumentIngestionService>();
                    services.RemoveAll<IDriftReportStore>();
                    services.RemoveAll<IHumanGovernedModelManager>();
                    services.AddSingleton<IDriftReportStore>(delayedGovernance);
                    services.AddSingleton<IHumanGovernedModelManager>(delayedGovernance);
                }))
            .StartAsync(timeout.Token);
        using var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Development-User", "reviewer");
        client.DefaultRequestHeaders.Add("X-Development-Roles", "ModelGovernanceReviewer");

        using var requestTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var stopwatch = Stopwatch.StartNew();
        using var home = await client.GetAsync("/", requestTimeout.Token);
        stopwatch.Stop();

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
        Assert.Contains("Research Assistant", await home.Content.ReadAsStringAsync());
        Assert.Equal(0, delayedGovernance.LoadCallCount);

        using var liveness = await client.GetAsync("/health/live", timeout.Token);
        Assert.Equal(HttpStatusCode.OK, liveness.StatusCode);

        using var readiness = await client.GetAsync("/health/ready", timeout.Token);
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
        Assert.Contains("NotConfigured", await readiness.Content.ReadAsStringAsync());

        using var unknown = await client.GetAsync("/route-that-does-not-exist", timeout.Token);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task Governance_timeout_returns_safe_unavailable_state()
    {
        var delayedGovernance = new NeverCompletingGovernanceServices();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ModelGovernance:LoadTimeoutMilliseconds"] = "50" }).Build();
        var loader = new GovernanceStateLoader(
            delayedGovernance,
            delayedGovernance,
            configuration,
            NullLogger<GovernanceStateLoader>.Instance);

        var stopwatch = Stopwatch.StartNew();
        var result = await loader.LoadAsync();
        stopwatch.Stop();

        Assert.False(result.IsAvailable);
        Assert.Equal("Governance status temporarily unavailable", result.Message);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3));
        Assert.Equal(2, delayedGovernance.LoadCallCount);
    }

    private sealed class ThrowingDocumentIngestionService : IDocumentIngestionService
    {
        public Task<IReadOnlyList<ResearchChunk>> IngestAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Document ingestion must not run during initial page rendering.");
    }

    private sealed class NeverCompletingGovernanceServices : IDriftReportStore, IHumanGovernedModelManager
    {
        private int _loadCallCount;
        public int LoadCallCount => _loadCallCount;
        public ModelGovernanceState State { get; } = new("Unavailable", null, "Unavailable", false, [], [], []);

        public Task<IReadOnlyList<DriftReport>> ListAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _loadCallCount);
            return new TaskCompletionSource<IReadOnlyList<DriftReport>>().Task;
        }

        public Task<ModelGovernanceState> GetStateAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _loadCallCount);
            return new TaskCompletionSource<ModelGovernanceState>().Task;
        }

        public Task SaveAsync(DriftReport report, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DriftReport?> GetAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RegisterComparisonAsync(ChampionChallengerComparison comparison, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PromotionAuditRecord> ApproveAsync(PromotionActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PromotionAuditRecord> RejectAsync(PromotionActionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PromotionAuditRecord> RollbackAsync(string approverIdentifier, DateTime approvalTimestampUtc, string reason, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
