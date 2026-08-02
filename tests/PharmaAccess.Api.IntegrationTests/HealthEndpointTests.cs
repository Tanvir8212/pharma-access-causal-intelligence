using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using System.Text;
using System.Text.Json;
using PharmaAccess.Application.MachineLearning;

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

        [Fact]
        public void Prediction_contract_serializes_raw_calibrated_uncertainty_and_lineage_fields()
        {
            var response = new NextStateEntryPredictionResponse("NextQuarterStateEntry", .7f, .65f, CalibrationStatus.Validated, 1.2f, true, .5, UncertaintyStatus.Moderate, ["near threshold"], ["historical volume associated with prediction"], "model-v1", ModelApprovalStatus.Approved, 2, 3, 4, ["development only"], DateTime.UnixEpoch);
            var json = JsonSerializer.Serialize(response);
            Assert.Contains("RawProbability", json); Assert.Contains("CalibratedProbability", json); Assert.Contains("CalibrationStatus", json); Assert.Contains("UncertaintyStatus", json); Assert.Contains("ModelApprovalStatus", json); Assert.Contains("DatasetVersionId", json); Assert.DoesNotContain("ConfidenceScore", json);
        }

        [Fact]
        public async Task Assistant_endpoint_returns_safe_fallback_without_a_Gemini_key()
        {
            using var host = await new HostBuilder().ConfigureWebHost(webBuilder => webBuilder.UseTestServer().UseStartup<Api.Startup>()).StartAsync();
            using var client = host.GetTestClient();
            var response = await client.PostAsync("/api/v1/assistant/ask",
                new StringContent("{\"question\":\"What was the locked-test ROC AUC?\"}", Encoding.UTF8, "application/json"));
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("provider-unavailable", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("apiKey", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Drift_report_endpoints_generate_list_and_view_reports_without_promoting()
        {
            using var host = await new HostBuilder().ConfigureWebHost(webBuilder => webBuilder.UseEnvironment("Development").UseTestServer().UseStartup<Api.Startup>()).StartAsync(); using var client = host.GetTestClient();client.DefaultRequestHeaders.Add("X-Development-User","reviewer");client.DefaultRequestHeaders.Add("X-Development-Roles","ModelGovernanceReviewer");
            const string json = """{"championVersion":"fasttree-published","evaluationWindow":"2026-Q3","numericFeatures":[{"name":"Volume","reference":[0.1,0.2,0.3],"current":[0.1,0.2,0.3]}],"categoricalFeatures":[],"referencePredictions":[0.1,0.2,0.3],"currentPredictions":[0.1,0.2,0.3]}""";
            var generated = await client.PostAsync("/api/v1/model-governance/drift-reports", new StringContent(json, Encoding.UTF8, "application/json")); var body = await generated.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, generated.StatusCode); using var document = JsonDocument.Parse(body); var id = document.RootElement.GetProperty("Id").GetGuid(); Assert.Contains("advisory", body, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/model-governance/drift-reports")).StatusCode); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/model-governance/drift-reports/{id}")).StatusCode);
            var state = await client.GetStringAsync("/api/v1/model-governance/state"); Assert.Contains("fasttree-published-threshold-0.08", state); Assert.DoesNotContain("Human-approved promotion", state);
        }
    }
}
