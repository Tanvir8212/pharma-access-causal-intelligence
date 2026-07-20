using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace PharmaAccess.Api
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
                if (context.Request.Path == "/health")
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"Healthy\",\"milestone\":0}");
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status404NotFound;
            });
        }
    }
}
