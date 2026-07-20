using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace PharmaAccess.Web
{
    public sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync("<h1>PharmaAccess AI</h1><p>Milestone 0 foundation is running.</p>");
            });
        }
    }
}
