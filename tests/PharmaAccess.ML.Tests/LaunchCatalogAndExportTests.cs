using PharmaAccess.Application.MachineLearning;
using PharmaAccess.ML;
using Xunit;

namespace PharmaAccess.ML.Tests;

public sealed class LaunchCatalogAndExportTests
{
    private static HistoricalReplayLaunch[] Catalog =>
    [
        new(7,"Prucalopride Succinate — ANDA 123456 — 2023-08-14","ANDA-CODE-7","123456",new(2023,8,14),2023,17)
        {
            DrugNames=["PRUCALOPRIDE SUCCINATE"], ActiveIngredients=["PRUCALOPRIDE SUCCINATE"],
            Forms=["TABLET;ORAL"], Strengths=["EQ 1MG BASE","EQ 2MG BASE"], ProductCount=2
        },
        new(8,"Example Product — ANDA 654321 — 2024-01-02","ANDA-CODE-8","654321",new(2024,1,2),2024,18)
        {
            DrugNames=["EXAMPLE PRODUCT","EXAMPLE PRODUCT XR"], ActiveIngredients=["EXAMPLE INGREDIENT"],
            Forms=["CAPSULE;ORAL"], Strengths=["10MG"], ProductCount=2
        },
        new(9,"ANDA 777777 — 2022-01-01","ANDA-CODE-9","777777",new(2022,1,1),2022,19)
    ];

    [Theory]
    [InlineData("prucalopride",7)] [InlineData("succinate",7)] [InlineData("123456",7)]
    [InlineData("ANDA-CODE-8",8)] [InlineData("7",7)] [InlineData("2024",8)]
    public void Search_uses_all_verified_business_fields(string query,int id) =>
        Assert.Contains(ResearchLaunchSearch.Find(Catalog,query),x=>x.GenericLaunchId==id);

    [Fact]
    public void Search_requires_explicit_selection_and_does_not_fuzzily_match()
    {
        Assert.Empty(ResearchLaunchSearch.Find(Catalog,"12345x"));
        Assert.Equal(3,ResearchLaunchSearch.Find(Catalog,"ANDA-CODE").Count);
    }

    [Fact]
    public void Recent_year_and_form_filters_are_deterministic()
    {
        Assert.Equal([8,7,9],ResearchLaunchSearch.Recent(Catalog,3).Select(x=>x.GenericLaunchId));
        Assert.Equal(8,Assert.Single(ResearchLaunchSearch.Filter(Catalog,2024)).GenericLaunchId);
        Assert.Equal(7,Assert.Single(ResearchLaunchSearch.Filter(Catalog,form:"TABLET;ORAL")).GenericLaunchId);
        Assert.Empty(ResearchLaunchSearch.Find(Catalog,""));
    }

    [Theory]
    [InlineData("218492",218492L,true)] [InlineData("000218492",218492L,true)]
    [InlineData("ANDA218492",0L,false)] [InlineData("218492-1",0L,false)] [InlineData("",0L,false)]
    public void Application_number_normalization_never_false_matches(string value,long expected,bool valid)
    {
        Assert.Equal(valid,FdaApplicationNumber.TryNormalize(value,out var actual));
        Assert.Equal(expected,actual);
    }

    [Fact]
    public void Safe_exports_include_FDA_identity_and_preserve_full_probability_without_private_material()
    {
        var files=PredictionResultExport.Create(new(DynamicAsOfMode.Name,7,Catalog[0].DisplayIdentifier,20244,20251,[new(6,"CA","California",.027497245f,false,1)],.08,"model-v1","dataset-v1","features-v1",ModelApprovalStatus.ValidationSelected,false)
        {DrugName="PRUCALOPRIDE SUCCINATE",ActiveIngredient="PRUCALOPRIDE SUCCINATE",Anda="218492",ApprovalDate=new(2024,12,26)});
        Assert.Contains("DrugName,ActiveIngredient,ANDA,ApprovalDate",files.Csv);
        Assert.Contains("PRUCALOPRIDE SUCCINATE",files.Csv);
        Assert.Contains("218492",files.Json);
        Assert.Contains("0.027497245",files.Csv);
        Assert.Contains("\"StateName\": \"California\"",files.Json);
        Assert.DoesNotContain("ObservedPrescriptionCount",files.Csv+files.Json);
        Assert.DoesNotContain("Label",files.Csv+files.Json);
        Assert.DoesNotContain("PharmaAccessCausalIntelligence_ResearchDev",files.Csv+files.Json);
        Assert.DoesNotContain("ConnectionString",files.Csv+files.Json);
        Assert.DoesNotContain(":\\",files.Csv+files.Json);
    }

    [Fact]
    public void Presentation_preserves_canonical_rank_full_precision_threshold_and_business_quarters()
    {
        HistoricalReplayStateResult[] states=[new(36,"NY","New York",.0163f,false,2),new(6,"CA","California",.027497245f,false,1)];
        Assert.Equal([1,2],ExplorerPresentation.CanonicalTop(states).Select(x=>x.Rank));
        Assert.Equal(.027497245f,ExplorerPresentation.CanonicalTop(states)[0].Probability);
        Assert.Equal("2024 Q4",ExplorerPresentation.Quarter(20244));
        Assert.Equal("2025 Q1",ExplorerPresentation.Quarter(20251));
        Assert.Contains("2.75%",ExplorerPresentation.Summary(states,.08));
        Assert.Contains("None of the 2 eligible states",ExplorerPresentation.Summary(states,.08));
    }

    [Fact][Trait("Category","LocalResearchIntegration")]
    public async Task Authoritative_FDA_catalog_has_one_entry_per_eligible_launch_and_verified_launch_291()
    {
        var rows=await new ResearchLaunchCatalogService(true,true).GetLaunchesAsync();
        Assert.Equal(261,rows.Count);Assert.Equal(261,rows.Select(x=>x.GenericLaunchId).Distinct().Count());
        var launch=Assert.Single(rows,x=>x.GenericLaunchId==291);
        Assert.Equal("218492",launch.Anda);Assert.Equal(new DateOnly(2024,12,26),launch.ApprovalDate);
        Assert.Equal(["PRUCALOPRIDE SUCCINATE"],launch.DrugNames);
        Assert.Equal(["PRUCALOPRIDE SUCCINATE"],launch.ActiveIngredients);
        Assert.Equal(["TABLET;ORAL"],launch.Forms);
        Assert.Equal(["EQ 1MG BASE","EQ 2MG BASE"],launch.Strengths);
        Assert.Equal(2,launch.ProductCount);
        Assert.Contains(ResearchLaunchSearch.Find(rows,"prucalopride"),x=>x.GenericLaunchId==291);
        Assert.Equal(291,Assert.Single(ResearchLaunchSearch.Find(rows,"218492")).GenericLaunchId);
        Assert.Contains(ResearchLaunchSearch.Find(rows,"291"),x=>x.GenericLaunchId==291);
    }
}
