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
                endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
            });
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
