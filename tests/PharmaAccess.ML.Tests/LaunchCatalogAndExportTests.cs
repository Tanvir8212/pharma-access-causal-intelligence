using PharmaAccess.Application.MachineLearning;
using PharmaAccess.ML;
using Xunit;
namespace PharmaAccess.ML.Tests;
public sealed class LaunchCatalogAndExportTests
{
 static HistoricalReplayLaunch[] Catalog=>[new(7,"ANDA-CODE-7 — ANDA 123456 — 2023-08-14","ANDA-CODE-7","123456",new(2023,8,14),2023,17),new(8,"ANDA-CODE-8 — ANDA 654321 — 2024-01-02","ANDA-CODE-8","654321",new(2024,1,2),2024,18)];
 [Theory][InlineData("123456",7)][InlineData("ANDA-CODE-8",8)][InlineData("7",7)][InlineData("2024",8)]public void Search_uses_verified_fields_and_resolves_deterministically(string query,int id){Assert.Equal(id,Assert.Single(ResearchLaunchSearch.Find(Catalog,query)).GenericLaunchId);}
 [Fact]public void Search_does_not_fuzzily_resolve_ambiguous_or_unknown_input(){Assert.Empty(ResearchLaunchSearch.Find(Catalog,"12345x"));Assert.Equal(2,ResearchLaunchSearch.Find(Catalog,"ANDA-CODE").Count);}
 [Fact]public void Safe_exports_preserve_probabilities_and_exclude_private_material(){var files=PredictionResultExport.Create(new(DynamicAsOfMode.Name,7,Catalog[0].DisplayIdentifier,20244,20251,[new(6,"CA","California",.027497245f,false,1)],.08,"model-v1","dataset-v1","features-v1",ModelApprovalStatus.ValidationSelected,false));Assert.Contains("StateCode",files.Csv);Assert.Contains("0.027497245",files.Csv);Assert.Contains("\"StateName\": \"California\"",files.Json);Assert.DoesNotContain("ObservedPrescriptionCount",files.Csv+files.Json);Assert.DoesNotContain("Label",files.Csv+files.Json);Assert.DoesNotContain("PharmaAccessCausalIntelligence_ResearchDev",files.Csv+files.Json);Assert.DoesNotContain("ConnectionString",files.Csv+files.Json);Assert.DoesNotContain(":\\",files.Csv+files.Json);Assert.EndsWith(".csv",files.CsvFileName);Assert.DoesNotContain(' ',files.CsvFileName);}
 [Fact][Trait("Category","LocalResearchIntegration")]public async Task Authoritative_launch_catalog_has_261_unique_deterministic_labels(){var service=new ResearchLaunchCatalogService(true,true);var rows=await service.GetLaunchesAsync();Assert.Equal(261,rows.Count);Assert.Equal(261,rows.Select(x=>x.GenericLaunchId).Distinct().Count());Assert.All(rows,x=>{Assert.NotNull(x.GenericLaunchCode);Assert.Matches("^[0-9]{6}$",x.Anda!);Assert.Contains($"ANDA {x.Anda}",x.DisplayIdentifier);Assert.Contains(x.ApprovalDate!.Value.ToString("yyyy-MM-dd"),x.DisplayIdentifier);});}
}
