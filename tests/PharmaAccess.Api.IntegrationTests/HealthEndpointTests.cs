using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Application.Research;
using PharmaAccess.Data.Research;
using PharmaAccess.Infrastructure.Research;
using Xunit;

namespace PharmaAccess.Api.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact] public async Task Health_endpoint_reports_finalized_read_only_research()
    {
        using var host=await ApiHost();using var client=host.GetTestClient();var response=await client.GetAsync("/health");var body=await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("\"databaseConnected\":true",body);Assert.Contains("\"datasetStatus\":\"Finalized\"",body);Assert.Contains("\"totalRows\":174471",body);
    }

    [Fact] public async Task Ask_api_is_conservative_and_validates_empty_question()
    {
        using var host=await ApiHost();using var client=host.GetTestClient();var bad=await client.PostAsync("/api/ask",new StringContent("{\"question\":\"\"}",Encoding.UTF8,"application/json"));Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);
        var response=await client.PostAsync("/api/ask",new StringContent("{\"question\":\"What was the causal result?\"}",Encoding.UTF8,"application/json"));var body=await response.Content.ReadAsStringAsync();Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("0.00157",body);Assert.Contains("includes zero",body);Assert.Contains("does not establish",body);
    }

    [Theory][InlineData("/")][InlineData("/Dashboard")][InlineData("/Ask")] public async Task Web_application_starts_and_serves_required_pages(string path)
    {
        using var host=await WebHost();using var response=await host.GetTestClient().GetAsync(path);Assert.Equal(HttpStatusCode.OK,response.StatusCode);
    }

    [Fact] public async Task Prediction_endpoint_accepts_only_feature_references_and_reports_missing_registry()
    {
        using var host=await ApiHost();using var client=host.GetTestClient();var bad=await client.PostAsync("/api/v1/predictions/next-state-entry",new StringContent("{}",Encoding.UTF8,"application/json"));Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);var unavailable=await client.PostAsync("/api/v1/predictions/next-state-entry",new StringContent("{\"featureRowId\":1,\"featureSetVersionId\":1}",Encoding.UTF8,"application/json"));Assert.Equal(HttpStatusCode.ServiceUnavailable,unavailable.StatusCode);
    }

    [Fact] public void Prediction_contract_serializes_raw_calibrated_uncertainty_and_lineage_fields()
    {
        var response=new NextStateEntryPredictionResponse("NextQuarterStateEntry",.7f,.65f,CalibrationStatus.Validated,1.2f,true,.5,UncertaintyStatus.Moderate,["near threshold"],["historical volume associated with prediction"],"model-v1",ModelApprovalStatus.Approved,2,3,4,["development only"],DateTime.UnixEpoch);var json=JsonSerializer.Serialize(response);Assert.Contains("RawProbability",json);Assert.Contains("DatasetVersionId",json);Assert.DoesNotContain("ConfidenceScore",json);
    }

    [Theory][InlineData("INSERT INTO x VALUES(1)")][InlineData(" update x set y=1")][InlineData("DROP TABLE x")] public void Read_only_guard_rejects_writes(string sql)=>Assert.Throws<InvalidOperationException>(()=>ReadOnlyResearchCommandInterceptor.Guard(sql));
    [Fact] public void Dashboard_metrics_parse_from_frozen_artifacts(){var results=new FinalResearchArtifactService(FinalResearchArtifactService.Locate(FindRoot())).Read();Assert.Equal(.8221,results.Predictive.RocAuc,4);Assert.Equal(.00157,results.Causal.Aipw,5);Assert.Equal(-.01382,results.Causal.PythonAipw,5);}

    private static async Task<IHost> ApiHost()=>await new HostBuilder().ConfigureWebHost(w=>w.UseTestServer().UseStartup<Api.Startup>().ConfigureTestServices(RegisterFakes)).StartAsync();
    private static async Task<IHost> WebHost()=>await new HostBuilder().ConfigureAppConfiguration(c=>c.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:PharmaAccess","Server=.;Database=unused;Trusted_Connection=True"}})).ConfigureWebHost(w=>w.UseTestServer().UseContentRoot(FindRoot()).UseStartup<Web.Startup>().ConfigureTestServices(RegisterFakes)).StartAsync();
    private static void RegisterFakes(IServiceCollection s){s.AddScoped<IFinalizedResearchReadService,_Data>();s.AddSingleton<IFinalResearchArtifactService>(new _Artifacts());s.AddScoped<IResearchAnswerService,DeterministicResearchAnswerService>();}
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"PharmaAccess.sln")))d=d.Parent;return d?.FullName??AppContext.BaseDirectory;}
    private sealed class _Data:IFinalizedResearchReadService { public Task<FinalizedDatasetSnapshot> GetAsync(CancellationToken ct=default)=>Task.FromResult(new FinalizedDatasetSnapshot("real-2021-2025-v1","Finalized","Passed",174471,new DateTime(2026,7,22,20,27,6,DateTimeKind.Utc),261,147,66,48)); }
    private sealed class _Artifacts:IFinalResearchArtifactService { public FinalResearchResults Read()=>new(new(.8221,.1112,.0848,.0195,.1388,.3398,.1971,.9558),new(.00157,-.00377,.00928,.00202,.000003,.00266,-.01382),true); }
}
