using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Application.Research;
using PharmaAccess.Data;
using PharmaAccess.Data.Research;
using PharmaAccess.Infrastructure.Research;

namespace PharmaAccess.Api;

public sealed class Startup(IConfiguration configuration,IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INextStateEntryPredictionService,UnavailablePredictionService>();
        var connection=configuration.GetConnectionString("PharmaAccess");
        if(!string.IsNullOrWhiteSpace(connection))
        {
            services.AddSingleton<ReadOnlyResearchCommandInterceptor>();services.AddDbContext<PharmaAccessDbContext>((sp,o)=>o.UseSqlServer(connection).AddInterceptors(sp.GetRequiredService<ReadOnlyResearchCommandInterceptor>()));
            services.AddScoped<IFinalizedResearchReadService,FinalizedResearchReadService>();services.AddSingleton<IFinalResearchArtifactService>(new FinalResearchArtifactService(FinalResearchArtifactService.Locate(environment.ContentRootPath)));services.AddScoped<IResearchAnswerService,DeterministicResearchAnswerService>();
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context=>
        {
            if(context.Request.Path=="/health"&&HttpMethods.IsGet(context.Request.Method))
            {
                try{var d=await context.RequestServices.GetRequiredService<IFinalizedResearchReadService>().GetAsync(context.RequestAborted);var a=context.RequestServices.GetRequiredService<IFinalResearchArtifactService>().Read();await Json(context,new{status="Healthy",databaseConnected=true,datasetVersion=d.DatasetVersion,datasetStatus=d.Status,validationStatus=d.ValidationStatus,totalRows=d.TotalRows,finalizedAtUtc=d.FinalizedAtUtc,artifactsAvailable=a.ArtifactsAvailable});}
                catch{context.Response.StatusCode=StatusCodes.Status503ServiceUnavailable;await Json(context,new{status="Unhealthy",databaseConnected=false,datasetVersion="real-2021-2025-v1",datasetStatus=(string?)null,validationStatus=(string?)null,totalRows=0,finalizedAtUtc=(DateTime?)null,artifactsAvailable=false});}return;
            }
            if(context.Request.Path=="/api/ask"&&HttpMethods.IsPost(context.Request.Method))
            {
                AskRequest? request;try{request=await JsonSerializer.DeserializeAsync<AskRequest>(context.Request.Body,new JsonSerializerOptions{PropertyNameCaseInsensitive=true},context.RequestAborted);}catch(JsonException){context.Response.StatusCode=400;return;}
                if(request is null||string.IsNullOrWhiteSpace(request.Question)||request.Question.Length>500){context.Response.StatusCode=400;await Json(context,new{error="Question must contain 1 to 500 characters."});return;}
                var answer=await context.RequestServices.GetRequiredService<IResearchAnswerService>().AnswerAsync(request.Question,context.RequestAborted);await Json(context,answer);return;
            }
            if(context.Request.Path=="/api/v1/predictions/next-state-entry"&&HttpMethods.IsPost(context.Request.Method))
            {
                var request=await JsonSerializer.DeserializeAsync<NextStateEntryPredictionRequest>(context.Request.Body,new JsonSerializerOptions{PropertyNameCaseInsensitive=true},context.RequestAborted);if(request is null||request.FeatureRowId<=0||request.FeatureSetVersionId<=0){context.Response.StatusCode=400;return;}try{var response=await context.RequestServices.GetRequiredService<INextStateEntryPredictionService>().PredictAsync(request,context.RequestAborted);if(response is null){context.Response.StatusCode=404;return;}await Json(context,response);return;}catch(FileNotFoundException){context.Response.StatusCode=503;return;}catch(InvalidOperationException){context.Response.StatusCode=409;return;}
            }
            context.Response.StatusCode=404;
        });
    }
    private static async Task Json(HttpContext c,object value){c.Response.ContentType="application/json";await JsonSerializer.SerializeAsync(c.Response.Body,value,cancellationToken:c.RequestAborted);}
    public sealed record AskRequest(string Question);
    private sealed class UnavailablePredictionService : INextStateEntryPredictionService { public Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request,CancellationToken cancellationToken=default)=>throw new FileNotFoundException("No model registry is configured."); }
}
