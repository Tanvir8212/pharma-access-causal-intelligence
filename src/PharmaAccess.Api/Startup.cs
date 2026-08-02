using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using PharmaAccess.ML;
using PharmaAccess.Data;

namespace PharmaAccess.Api
{
    public sealed class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<INextStateEntryPredictionService, UnavailablePredictionService>();
            services.AddPharmaAccessResearchAssistant(_configuration, FindRepositoryRoot(_environment.ContentRootPath));
            AddDriftGovernance(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run((RequestDelegate)(async context =>
            {
                if (context.Request.Path == "/health")
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"Healthy\",\"milestone\":0}");
                    return;
                }

                if (context.Request.Path == "/api/v1/predictions/next-state-entry" && HttpMethods.IsPost(context.Request.Method))
                {
                    var request = await JsonSerializer.DeserializeAsync<NextStateEntryPredictionRequest>(context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, context.RequestAborted);
                    if (request is null || request.FeatureRowId <= 0 || request.FeatureSetVersionId <= 0) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    try
                    {
                        var service = context.RequestServices.GetRequiredService<INextStateEntryPredictionService>(); var response = await service.PredictAsync(request, context.RequestAborted);
                        if (response is null) { context.Response.StatusCode = StatusCodes.Status404NotFound; return; }
                        context.Response.ContentType = "application/json"; await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted); return;
                    }
                    catch (FileNotFoundException) { context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable; return; }
                    catch (InvalidOperationException) { context.Response.StatusCode = StatusCodes.Status409Conflict; return; }
                }

                if (context.Request.Path == "/api/v1/assistant/ask" && HttpMethods.IsPost(context.Request.Method))
                {
                    var request = await JsonSerializer.DeserializeAsync<AssistantAskRequest>(
                        context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, context.RequestAborted);
                    if (request is null || string.IsNullOrWhiteSpace(request.Question))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsJsonAsync(new { error = "A non-empty question is required." }, context.RequestAborted);
                        return;
                    }
                    var service = context.RequestServices.GetRequiredService<ResearchAssistantService>();
                    var response = await service.AskAsync(request.Question, context.RequestAborted);
                    context.Response.ContentType = "application/json";
                    await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted);
                    return;
                }

                if (context.Request.Path == "/api/v1/model-governance/drift-reports" && HttpMethods.IsPost(context.Request.Method))
                {
                    var request = await ReadAsync<DriftReportRequest>(context);
                    if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    try { var report = context.RequestServices.GetRequiredService<IDriftDetector>().Detect(request); await context.RequestServices.GetRequiredService<IDriftReportStore>().SaveAsync(report, context.RequestAborted); await WriteAsync(context, report); }
                    catch (ArgumentException error) { await WriteErrorAsync(context, StatusCodes.Status400BadRequest, error.Message); }
                    return;
                }
                if (context.Request.Path == "/api/v1/model-governance/drift-reports" && HttpMethods.IsGet(context.Request.Method))
                { await WriteAsync(context, await context.RequestServices.GetRequiredService<IDriftReportStore>().ListAsync(context.RequestAborted)); return; }
                if (context.Request.Path.StartsWithSegments("/api/v1/model-governance/drift-reports", out var reportRemainder) && HttpMethods.IsGet(context.Request.Method))
                {
                    if (!Guid.TryParse(reportRemainder.Value?.Trim('/'), out var id)) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    var report = await context.RequestServices.GetRequiredService<IDriftReportStore>().GetAsync(id, context.RequestAborted);
                    if (report is null) { context.Response.StatusCode = StatusCodes.Status404NotFound; return; } await WriteAsync(context, report); return;
                }
                if (context.Request.Path == "/api/v1/model-governance/comparisons" && HttpMethods.IsPost(context.Request.Method))
                {
                    var request = await ReadAsync<ComparisonRequest>(context); if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    var champion = VerifyArtifact(context, request.Champion); var challenger = VerifyArtifact(context, request.Challenger);
                    var comparison = context.RequestServices.GetRequiredService<IChampionChallengerComparer>().Compare(champion, challenger);
                    await context.RequestServices.GetRequiredService<IHumanGovernedModelManager>().RegisterComparisonAsync(comparison, context.RequestAborted);
                    await WriteAsync(context, comparison with { Champion = comparison.Champion with { ArtifactPath = "" }, Challenger = comparison.Challenger with { ArtifactPath = "" } }); return;
                }
                if (context.Request.Path == "/api/v1/model-governance/promotions/approve" && HttpMethods.IsPost(context.Request.Method))
                { await ExecuteGovernanceAsync(context, true); return; }
                if (context.Request.Path == "/api/v1/model-governance/promotions/reject" && HttpMethods.IsPost(context.Request.Method))
                { await ExecuteGovernanceAsync(context, false); return; }
                if (context.Request.Path == "/api/v1/model-governance/rollback" && HttpMethods.IsPost(context.Request.Method))
                {
                    var request = await ReadAsync<RollbackRequest>(context); if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    try { await WriteAsync(context, await context.RequestServices.GetRequiredService<IHumanGovernedModelManager>().RollbackAsync(request.ApproverIdentifier, request.ApprovalTimestampUtc, request.Reason, context.RequestAborted)); }
                    catch (Exception error) when (error is ArgumentException or InvalidOperationException) { await WriteErrorAsync(context, StatusCodes.Status409Conflict, error.Message); } return;
                }
                if (context.Request.Path == "/api/v1/model-governance/state" && HttpMethods.IsGet(context.Request.Method))
                { await WriteAsync(context, await context.RequestServices.GetRequiredService<IHumanGovernedModelManager>().GetStateAsync(context.RequestAborted)); return; }

                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }));
        }

        private static string FindRepositoryRoot(string start)
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PharmaAccess.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? start;
        }

        private void AddDriftGovernance(IServiceCollection services)
        {
            services.AddSingleton(new DriftThresholds());
            services.AddSingleton<IDriftDetector, DriftDetector>();
            services.AddSingleton<IChampionChallengerComparer, ChampionChallengerComparer>();
            var connection = _configuration.GetConnectionString("PharmaAccess");
            if (!string.IsNullOrWhiteSpace(connection))
            {
                services.AddPharmaAccessData(connection);
                services.AddPersistentModelGovernance(_configuration.GetSection("ModelGovernance:ApprovedArtifactRoots").Get<string[]>() ?? []);
            }
            else
            {
                services.AddSingleton<IDriftReportStore, InMemoryDriftReportStore>();
                services.AddSingleton<IHumanGovernedModelManager>(_ => new HumanGovernedModelManager("fasttree-published-threshold-0.08"));
            }
        }

        private sealed record ComparisonRequest(GovernedModelSnapshot Champion, GovernedModelSnapshot Challenger);
        private static GovernedModelSnapshot VerifyArtifact(HttpContext context, GovernedModelSnapshot model)
        {
            var verifier = context.RequestServices.GetService<IArtifactIntegrityVerifier>();
            return verifier is null ? model : model with { ArtifactHashValid = verifier.Verify(new(model.ArtifactPath, model.ArtifactSha256)).IsValid };
        }
        private sealed record RollbackRequest(string ApproverIdentifier, DateTime ApprovalTimestampUtc, string Reason);
        private static async Task<T?> ReadAsync<T>(HttpContext context) => await JsonSerializer.DeserializeAsync<T>(context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, context.RequestAborted);
        private static async Task WriteAsync<T>(HttpContext context, T value) { context.Response.ContentType = "application/json"; await JsonSerializer.SerializeAsync(context.Response.Body, value, cancellationToken: context.RequestAborted); }
        private static async Task WriteErrorAsync(HttpContext context, int status, string message) { context.Response.StatusCode = status; await context.Response.WriteAsJsonAsync(new { error = message }, context.RequestAborted); }
        private static async Task ExecuteGovernanceAsync(HttpContext context, bool approve)
        {
            var request = await ReadAsync<PromotionActionRequest>(context); if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
            try { var manager = context.RequestServices.GetRequiredService<IHumanGovernedModelManager>(); var result = approve ? await manager.ApproveAsync(request, context.RequestAborted) : await manager.RejectAsync(request, context.RequestAborted); await WriteAsync(context, result); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException) { await WriteErrorAsync(context, StatusCodes.Status409Conflict, error.Message); }
        }

        private sealed class UnavailablePredictionService : INextStateEntryPredictionService
        {
            public Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request, CancellationToken cancellationToken = default) => throw new FileNotFoundException("No model registry is configured.");
        }
    }
}
