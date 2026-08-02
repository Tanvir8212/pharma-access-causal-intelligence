using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using PharmaAccess.Llm;
using PharmaAccess.ML;

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
            services.AddRazorComponents().AddInteractiveServerComponents();
            services.AddPharmaAccessResearchAssistant(_configuration, FindRepositoryRoot(_environment.ContentRootPath));
            services.AddSingleton(new PharmaAccess.Application.MachineLearning.DriftThresholds());
            services.AddSingleton<PharmaAccess.Application.MachineLearning.IDriftDetector, DriftDetector>();
            services.AddSingleton<PharmaAccess.Application.MachineLearning.IDriftReportStore, InMemoryDriftReportStore>();
            services.AddSingleton<PharmaAccess.Application.MachineLearning.IChampionChallengerComparer, ChampionChallengerComparer>();
            services.AddSingleton<PharmaAccess.Application.MachineLearning.IHumanGovernedModelManager>(_ => new HumanGovernedModelManager("fasttree-published-threshold-0.08"));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStaticFiles();
            app.UseRouting();
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
