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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

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
            services.AddProblemDetails();
            services.AddRouting();
            var authentication=services.AddAuthentication(options=>{options.DefaultAuthenticateScheme="ConfiguredAuthentication";options.DefaultChallengeScheme="ConfiguredAuthentication";});
            if (_configuration["Authentication:Mode"]?.Equals("Jwt",StringComparison.OrdinalIgnoreCase)==true)
                authentication.AddJwtBearer("ConfiguredAuthentication",options=>{options.Authority=_configuration["Authentication:Authority"];options.Audience=_configuration["Authentication:Audience"];options.RequireHttpsMetadata=!_environment.IsDevelopment();});
            else authentication.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,DevelopmentHeaderAuthenticationHandler>("ConfiguredAuthentication",_=>{});
            services.AddAuthorization(options=>
            {
                options.AddPolicy(SecurityPolicies.ResearchReader,p=>p.RequireRole(SecurityPolicies.ResearchReader,SecurityPolicies.ModelGovernanceReviewer,SecurityPolicies.ModelGovernanceApprover,SecurityPolicies.SystemAdministrator));
                options.AddPolicy(SecurityPolicies.ModelGovernanceReviewer,p=>p.RequireRole(SecurityPolicies.ModelGovernanceReviewer,SecurityPolicies.ModelGovernanceApprover,SecurityPolicies.SystemAdministrator));
                options.AddPolicy(SecurityPolicies.ModelGovernanceApprover,p=>p.RequireRole(SecurityPolicies.ModelGovernanceApprover,SecurityPolicies.SystemAdministrator));
                options.AddPolicy(SecurityPolicies.SystemAdministrator,p=>p.RequireRole(SecurityPolicies.SystemAdministrator));
            });
            var rateSettings=_configuration.GetSection("RateLimiting").Get<RateLimitSettings>()??new();services.AddSingleton(rateSettings);services.AddSingleton(TimeProvider.System);services.AddSingleton<IRequestRateLimitGate,FixedWindowRequestRateLimitGate>();
            services.AddSingleton<INextStateEntryPredictionService, UnavailablePredictionService>();
            services.AddPharmaAccessResearchAssistant(_configuration, FindRepositoryRoot(_environment.ContentRootPath));
            AddDriftGovernance(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            if(!_environment.IsDevelopment())app.UseHsts();
            app.UseMiddleware<ApiSafetyMiddleware>();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.Run((RequestDelegate)(async context =>
            {
                if(context.Request.ContentLength>65_536){await WriteProblemAsync(context,StatusCodes.Status413PayloadTooLarge,"Request body is too large.");return;}
                if((context.Request.Headers.ContainsKey("Authorization")||context.Request.Headers.ContainsKey("X-Development-User"))&&!Acquire(context,"auth"))return;
                if (context.Request.Path == "/health/live") { await WriteAsync(context,new{status="Healthy"}); return; }
                if (context.Request.Path == "/health/ready") { await WriteReadinessAsync(context); return; }
                if (context.Request.Path == "/health")
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"Healthy\",\"milestone\":0}");
                    return;
                }

                if (context.Request.Path == "/api/v1/predictions/next-state-entry" && HttpMethods.IsPost(context.Request.Method))
                {
                    if(!Acquire(context,"reporting"))return;var started=Stopwatch.GetTimestamp();
                    var request = await JsonSerializer.DeserializeAsync<NextStateEntryPredictionRequest>(context.Request.Body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, context.RequestAborted);
                    if (request is null || request.FeatureRowId <= 0 || request.FeatureSetVersionId <= 0) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    try
                    {
                        var service = context.RequestServices.GetRequiredService<INextStateEntryPredictionService>(); var response = await service.PredictAsync(request, context.RequestAborted);
                        if (response is null) { context.Response.StatusCode = StatusCodes.Status404NotFound; return; }
                        context.Response.ContentType = "application/json"; await JsonSerializer.SerializeAsync(context.Response.Body, response, cancellationToken: context.RequestAborted); Log(context,"Prediction",new Dictionary<string,object?>{{"ModelVersionId",response.ModelVersion},{"DatasetVersionId",response.DatasetVersionId},{"FeatureVersion",response.FeatureSetVersionId},{"RequestTimestamp",DateTime.UtcNow},{"LatencyMs",Stopwatch.GetElapsedTime(started).TotalMilliseconds},{"Success",true}}); return;
                    }
                    catch (FileNotFoundException) { context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable; return; }
                    catch (InvalidOperationException) { context.Response.StatusCode = StatusCodes.Status409Conflict; return; }
                }

                if (context.Request.Path == "/api/v1/assistant/ask" && HttpMethods.IsPost(context.Request.Method))
                {
                    if(!Acquire(context,"rag"))return;
                    if(context.Request.ContentLength>16_384){await WriteProblemAsync(context,StatusCodes.Status413PayloadTooLarge,"Research question request is too large.");return;}var started=Stopwatch.GetTimestamp();
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
                    OperationalTelemetry.GeminiCalls.Add(1);if(response.ValidationStatus!="validated")OperationalTelemetry.GeminiFallbacks.Add(1);if(response.ValidationStatus=="validation-failed")OperationalTelemetry.ValidationFailures.Add(1);Log(context,"ResearchAssistant",new Dictionary<string,object?>{{"PromptVersion","rag-v1"},{"Provider",response.Provider},{"ModelName",response.Model},{"RetrievedDocumentIdentifiers",string.Join(',',response.Citations.Select(x=>x.SourceIdentifier))},{"ValidationResult",response.ValidationStatus},{"FallbackStatus",response.ValidationStatus!="validated"},{"LatencyMs",Stopwatch.GetElapsedTime(started).TotalMilliseconds},{"TokenUsage",null}});
                    return;
                }

                if (context.Request.Path == "/api/v1/model-governance/drift-reports" && HttpMethods.IsPost(context.Request.Method))
                {
                    if(!await RequirePolicyAsync(context,SecurityPolicies.ModelGovernanceReviewer)||!Acquire(context,"governance"))return;
                    var request = await ReadAsync<DriftReportRequest>(context);
                    if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    try { var report = context.RequestServices.GetRequiredService<IDriftDetector>().Detect(request); await context.RequestServices.GetRequiredService<IDriftReportStore>().SaveAsync(report, context.RequestAborted);var tags=new System.Diagnostics.TagList{{"severity",report.Severity.ToString()}};OperationalTelemetry.DriftSeverity.Add(1,tags);LogGovernance(context,"GenerateDriftReport",report.Id.ToString(),report.Severity.ToString(),true); await WriteAsync(context, report); }
                    catch (ArgumentException) { await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "The drift report request is invalid."); }
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
                    if(!await RequirePolicyAsync(context,SecurityPolicies.ModelGovernanceReviewer)||!Acquire(context,"governance"))return;
                    var request = await ReadAsync<ComparisonRequest>(context); if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    var champion = VerifyArtifact(context, request.Champion); var challenger = VerifyArtifact(context, request.Challenger);
                    var comparison = context.RequestServices.GetRequiredService<IChampionChallengerComparer>().Compare(champion, challenger) with { SubmitterIdentifier=Actor(context) };
                    await context.RequestServices.GetRequiredService<IHumanGovernedModelManager>().RegisterComparisonAsync(comparison, context.RequestAborted);
                    LogGovernance(context,"RegisterComparison",comparison.Id.ToString(),comparison.PromotionEligible?"Eligible":"Blocked",true);
                    await WriteAsync(context, comparison with { Champion = comparison.Champion with { ArtifactPath = "" }, Challenger = comparison.Challenger with { ArtifactPath = "" } }); return;
                }
                if (context.Request.Path == "/api/v1/model-governance/promotions/approve" && HttpMethods.IsPost(context.Request.Method))
                { if(!await RequirePolicyAsync(context,SecurityPolicies.ModelGovernanceApprover)||!Acquire(context,"governance"))return;await ExecuteGovernanceAsync(context, true); return; }
                if (context.Request.Path == "/api/v1/model-governance/promotions/reject" && HttpMethods.IsPost(context.Request.Method))
                { if(!await RequirePolicyAsync(context,SecurityPolicies.ModelGovernanceReviewer)||!Acquire(context,"governance"))return;await ExecuteGovernanceAsync(context, false); return; }
                if (context.Request.Path == "/api/v1/model-governance/rollback" && HttpMethods.IsPost(context.Request.Method))
                {
                    if(!await RequirePolicyAsync(context,SecurityPolicies.ModelGovernanceApprover)||!Acquire(context,"governance"))return;
                    var request = await ReadAsync<RollbackRequest>(context); if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
                    try { var actor=Actor(context);var result=await context.RequestServices.GetRequiredService<IHumanGovernedModelManager>().RollbackAsync(actor, request.ApprovalTimestampUtc, request.Reason, context.RequestAborted);OperationalTelemetry.Rollbacks.Add(1);LogGovernance(context,"Rollback",result.ChallengerVersion,result.Decision.ToString(),true);await WriteAsync(context,result); }
                    catch (Exception error) when (error is ArgumentException or InvalidOperationException) { LogGovernance(context,"Rollback","Unavailable","Failed",false);await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Rollback could not be completed."); } return;
                }
                if (context.Request.Path == "/api/v1/model-governance/state" && HttpMethods.IsGet(context.Request.Method))
                { if(!await RequirePolicyAsync(context,SecurityPolicies.ModelGovernanceReviewer))return;await WriteAsync(context, await context.RequestServices.GetRequiredService<IHumanGovernedModelManager>().GetStateAsync(context.RequestAborted)); return; }

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
        private static async Task ExecuteGovernanceAsync(HttpContext context, bool approve)
        {
            var request = await ReadAsync<PromotionActionRequest>(context); if (request is null) { context.Response.StatusCode = StatusCodes.Status400BadRequest; return; }
            var actor=Actor(context);request=request with{ApproverIdentifier=actor};
            try { var manager = context.RequestServices.GetRequiredService<IHumanGovernedModelManager>(); var result = approve ? await manager.ApproveAsync(request, context.RequestAborted) : await manager.RejectAsync(request, context.RequestAborted);if(approve)OperationalTelemetry.GovernanceApprovals.Add(1);else OperationalTelemetry.GovernanceRejections.Add(1);LogGovernance(context,approve?"Approve":"Reject",request.ComparisonId.ToString(),result.Decision.ToString(),true);await WriteAsync(context, result); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException) { LogGovernance(context,approve?"Approve":"Reject",request.ComparisonId.ToString(),"Failed",false);await WriteProblemAsync(context, StatusCodes.Status409Conflict, error.Message); }
        }

        private static string Actor(HttpContext context)=>context.User.FindFirstValue(ClaimTypes.NameIdentifier)??throw new InvalidOperationException("Authenticated actor identifier is unavailable.");
        private static async Task<bool> RequirePolicyAsync(HttpContext context,string policy)
        {
            if(context.User.Identity?.IsAuthenticated!=true){await WriteProblemAsync(context,StatusCodes.Status401Unauthorized,"Authentication is required.");return false;}var authorized=await context.RequestServices.GetRequiredService<IAuthorizationService>().AuthorizeAsync(context.User,null,policy);if(authorized.Succeeded)return true;await WriteProblemAsync(context,StatusCodes.Status403Forbidden,"The authenticated actor is not authorized for this operation.");return false;
        }
        private static bool Acquire(HttpContext context,string bucket)
        {
            var partition=context.User.FindFirstValue(ClaimTypes.NameIdentifier)??context.Connection.RemoteIpAddress?.ToString()??"anonymous";if(context.RequestServices.GetRequiredService<IRequestRateLimitGate>().TryAcquire(partition,bucket,out var retry))return true;context.Response.StatusCode=StatusCodes.Status429TooManyRequests;context.Response.Headers.RetryAfter=retry.ToString(System.Globalization.CultureInfo.InvariantCulture);context.Response.ContentType="application/problem+json";context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails{Status=429,Title="Rate limit exceeded.",Detail="Retry after the number of seconds indicated by the Retry-After header."}),context.RequestAborted).GetAwaiter().GetResult();return false;
        }
        private static async Task WriteProblemAsync(HttpContext context,int status,string title){context.Response.StatusCode=status;context.Response.ContentType="application/problem+json";await JsonSerializer.SerializeAsync(context.Response.Body,new ProblemDetails{Status=status,Title=title,Extensions={{"correlationId",context.TraceIdentifier}}},cancellationToken:context.RequestAborted);}
        private static void Log(HttpContext context,string eventName,IReadOnlyDictionary<string,object?> fields){var logger=context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PharmaAccess.Operations");var scope=new Dictionary<string,object?>(fields){{"CorrelationId",context.TraceIdentifier},{"EventName",eventName}};using(logger.BeginScope(scope))logger.LogInformation("Operational event {EventName} completed",eventName);}
        private static void LogGovernance(HttpContext context,string action,string identifier,string decision,bool success)=>Log(context,"Governance",new Dictionary<string,object?>{{"AuthenticatedActorIdentifier",context.User.FindFirstValue(ClaimTypes.NameIdentifier)},{"Action",action},{"ComparisonOrModelIdentifier",identifier},{"Decision",decision},{"Timestamp",DateTime.UtcNow},{"Success",success}});
        private static async Task WriteReadinessAsync(HttpContext context)
        {
            var gemini=context.RequestServices.GetRequiredService<ILanguageModelClient>();var db=context.RequestServices.GetService<PharmaAccessDbContext>();var sqlConfigured=db is not null;var sqlHealthy=true;if(db is not null){try{sqlHealthy=await db.Database.CanConnectAsync(context.RequestAborted);}catch{sqlHealthy=false;}}var status=sqlHealthy?StatusCodes.Status200OK:StatusCodes.Status503ServiceUnavailable;context.Response.StatusCode=status;await WriteAsync(context,new{status=sqlHealthy?(gemini.IsAvailable?"Healthy":"Degraded"):"Unhealthy",dependencies=new{sql=sqlConfigured?(sqlHealthy?"Healthy":"Unavailable"):"NotConfigured",gemini=gemini.IsAvailable?"Healthy":"OptionalUnavailable"}});
        }

        private sealed class UnavailablePredictionService : INextStateEntryPredictionService
        {
            public Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request, CancellationToken cancellationToken = default) => throw new FileNotFoundException("No model registry is configured.");
        }
    }
}
