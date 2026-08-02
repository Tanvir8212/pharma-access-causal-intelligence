using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using PharmaAccess.Llm;
using PharmaAccess.ML;
using PharmaAccess.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace PharmaAccess.Web
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
            var authentication=services.AddAuthentication(o=>{o.DefaultAuthenticateScheme="ConfiguredAuthentication";o.DefaultChallengeScheme="ConfiguredAuthentication";});
            if(_configuration["Authentication:Mode"]?.Equals("Jwt",StringComparison.OrdinalIgnoreCase)==true)authentication.AddJwtBearer("ConfiguredAuthentication",o=>{o.Authority=_configuration["Authentication:Authority"];o.Audience=_configuration["Authentication:Audience"];o.RequireHttpsMetadata=!_environment.IsDevelopment();});else authentication.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,WebDevelopmentAuthenticationHandler>("ConfiguredAuthentication",_=>{});
            services.AddAuthorization(o=>o.AddPolicy(WebPolicies.ModelGovernanceReviewer,p=>p.RequireRole("ModelGovernanceReviewer","ModelGovernanceApprover","SystemAdministrator")));
            services.AddCascadingAuthenticationState();services.AddAntiforgery();
            services.AddHttpClient("ApiReadiness",client=>client.Timeout=TimeSpan.FromSeconds(3));
            services.AddRazorComponents().AddInteractiveServerComponents();
            services.AddPharmaAccessResearchAssistant(_configuration, FindRepositoryRoot(_environment.ContentRootPath));
            services.AddSingleton(new PharmaAccess.Application.MachineLearning.DriftThresholds());
            services.AddSingleton<PharmaAccess.Application.MachineLearning.IDriftDetector, DriftDetector>();
            services.AddSingleton<PharmaAccess.Application.MachineLearning.IChampionChallengerComparer, ChampionChallengerComparer>();
            var connection = _configuration.GetConnectionString("PharmaAccess");
            if (!string.IsNullOrWhiteSpace(connection))
            {
                services.AddPharmaAccessData(connection);
                services.AddPersistentModelGovernance(_configuration.GetSection("ModelGovernance:ApprovedArtifactRoots").Get<string[]>() ?? []);
            }
            else
            {
                services.AddSingleton<PharmaAccess.Application.MachineLearning.IDriftReportStore, InMemoryDriftReportStore>();
                services.AddSingleton<PharmaAccess.Application.MachineLearning.IHumanGovernedModelManager>(_ => new HumanGovernedModelManager("fasttree-published-threshold-0.08"));
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            if(!_environment.IsDevelopment())app.UseHsts();app.UseMiddleware<WebSafetyMiddleware>();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();app.UseAuthorization();app.UseAntiforgery();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health/live",async context=>await context.Response.WriteAsJsonAsync(new{status="Healthy"},context.RequestAborted));
                endpoints.MapGet("/health/ready",WriteReadinessAsync);
                endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            });
        }

        private async Task WriteReadinessAsync(HttpContext context)
        {
            var apiBaseUrl=_configuration["PharmaAccess:ApiBaseUrl"];
            if(string.IsNullOrWhiteSpace(apiBaseUrl)){await context.Response.WriteAsJsonAsync(new{status="Healthy",dependencies=new{api="NotConfigured"}},context.RequestAborted);return;}
            try
            {
                var client=context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("ApiReadiness");
                using var response=await client.GetAsync(new Uri(new Uri(apiBaseUrl),"health/ready"),context.RequestAborted);
                context.Response.StatusCode=response.IsSuccessStatusCode?StatusCodes.Status200OK:StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new{status=response.IsSuccessStatusCode?"Healthy":"Unavailable",dependencies=new{api=response.IsSuccessStatusCode?"Healthy":"Unavailable"}},context.RequestAborted);
            }
            catch(Exception error) when(error is HttpRequestException or TaskCanceledException or UriFormatException)
            {context.Response.StatusCode=StatusCodes.Status503ServiceUnavailable;await context.Response.WriteAsJsonAsync(new{status="Unavailable",dependencies=new{api="Unavailable"}},context.RequestAborted);}
        }

        private static string FindRepositoryRoot(string start)
        {
            var directory = new DirectoryInfo(start);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PharmaAccess.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? start;
        }
    }
}
