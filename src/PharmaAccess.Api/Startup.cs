using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

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
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
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

                context.Response.StatusCode = StatusCodes.Status404NotFound;
            });
        }

        private static string FindRepositoryRoot(string start)
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PharmaAccess.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? start;
        }

        private sealed class UnavailablePredictionService : INextStateEntryPredictionService
        {
            public Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request, CancellationToken cancellationToken = default) => throw new FileNotFoundException("No model registry is configured.");
        }
    }
}
