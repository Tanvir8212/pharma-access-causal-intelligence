using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.Api
{
    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<INextStateEntryPredictionService, UnavailablePredictionService>();
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

                context.Response.StatusCode = StatusCodes.Status404NotFound;
            });
        }

        private sealed class UnavailablePredictionService : INextStateEntryPredictionService
        {
            public Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request, CancellationToken cancellationToken = default) => throw new FileNotFoundException("No model registry is configured.");
        }
    }
}
