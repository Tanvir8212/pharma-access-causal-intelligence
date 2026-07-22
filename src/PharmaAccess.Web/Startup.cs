using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using PharmaAccess.Application.Research;
using PharmaAccess.Data;
using PharmaAccess.Data.Research;
using PharmaAccess.Infrastructure.Research;

namespace PharmaAccess.Web;

public sealed class Startup(IConfiguration configuration,IWebHostEnvironment environment)
{
    public void ConfigureServices(IServiceCollection services)
    {
        var connection=configuration.GetConnectionString("PharmaAccess")??throw new InvalidOperationException("User Secrets ConnectionStrings:PharmaAccess is required.");
        services.AddSingleton<ReadOnlyResearchCommandInterceptor>();
        services.AddDbContext<PharmaAccessDbContext>((sp,o)=>o.UseSqlServer(connection).AddInterceptors(sp.GetRequiredService<ReadOnlyResearchCommandInterceptor>()));
        services.AddScoped<IFinalizedResearchReadService,FinalizedResearchReadService>();
        services.AddSingleton<IFinalResearchArtifactService>(new FinalResearchArtifactService(FinalResearchArtifactService.Locate(environment.ContentRootPath)));
        services.AddScoped<IResearchAnswerService,DeterministicResearchAnswerService>();services.AddRazorPages();
    }
    public void Configure(IApplicationBuilder app,IWebHostEnvironment env)
    {
        if(!env.IsDevelopment())app.UseExceptionHandler("/Error");app.UseStaticFiles();app.UseRouting();app.UseEndpoints(e=>
        {
            e.MapGet("/health",async context=>{try{var d=await context.RequestServices.GetRequiredService<IFinalizedResearchReadService>().GetAsync(context.RequestAborted);var a=context.RequestServices.GetRequiredService<IFinalResearchArtifactService>().Read();await JsonSerializer.SerializeAsync(context.Response.Body,new{status="Healthy",databaseConnected=true,datasetVersion=d.DatasetVersion,datasetStatus=d.Status,validationStatus=d.ValidationStatus,totalRows=d.TotalRows,finalizedAtUtc=d.FinalizedAtUtc,artifactsAvailable=a.ArtifactsAvailable},cancellationToken:context.RequestAborted);}catch{context.Response.StatusCode=503;await JsonSerializer.SerializeAsync(context.Response.Body,new{status="Unhealthy",databaseConnected=false,artifactsAvailable=false},cancellationToken:context.RequestAborted);}});
            e.MapPost("/api/ask",async context=>{AskRequest? request;try{request=await JsonSerializer.DeserializeAsync<AskRequest>(context.Request.Body,new JsonSerializerOptions{PropertyNameCaseInsensitive=true},context.RequestAborted);}catch(JsonException){context.Response.StatusCode=400;return;}if(request is null||string.IsNullOrWhiteSpace(request.Question)||request.Question.Length>500){context.Response.StatusCode=400;return;}var answer=await context.RequestServices.GetRequiredService<IResearchAnswerService>().AnswerAsync(request.Question,context.RequestAborted);await JsonSerializer.SerializeAsync(context.Response.Body,answer,cancellationToken:context.RequestAborted);});
            e.MapRazorPages();
        });
    }
    private sealed record AskRequest(string Question);
}
