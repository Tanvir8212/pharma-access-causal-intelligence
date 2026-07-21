using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using System.Text;

namespace PharmaAccess.Api.IntegrationTests
{
    public sealed class HealthEndpointTests
    {
        [Fact]
        public async Task Health_endpoint_reports_the_milestone_zero_host_as_healthy()
        {
            using (var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .UseStartup<Api.Startup>())
                .StartAsync())
            using (var client = host.GetTestClient())
            {
                var response = await client.GetAsync("/health");
                var body = await response.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("{\"status\":\"Healthy\",\"milestone\":0}", body);
            }
        }


        [Fact]
        public async Task Prediction_endpoint_accepts_only_feature_references_and_reports_missing_registry()
        {
            using var host = await new HostBuilder().ConfigureWebHost(webBuilder => webBuilder.UseTestServer().UseStartup<Api.Startup>()).StartAsync();
            using var client = host.GetTestClient();
            var bad = await client.PostAsync("/api/v1/predictions/next-state-entry", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            var unavailable = await client.PostAsync("/api/v1/predictions/next-state-entry", new StringContent("{\"featureRowId\":1,\"featureSetVersionId\":1}", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        }
    }
}
