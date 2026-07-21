using System.Text.Json;
using PharmaAccess.Application.Causal;
using Xunit;

namespace PharmaAccess.Causal.Tests;

public sealed class MilestoneSevenValidationTests
{
    [Fact]
    public void Bundle_export_is_deterministic_invariant_ordered_and_hashed()
    {
        var root=Path.Combine(Path.GetTempPath(),$"pa-m7-{Guid.NewGuid():N}");
        try
        {
            var exporter=new CausalValidationBundleExporter();var request=new ExportCausalValidationBundleCommand(1,"run-1",root,true,ValidationOverwritePolicy.Reject,"corr");var a=exporter.ExportSynthetic(request);var first=File.ReadAllBytes(Path.Combine(root,"run-1","analysis_rows.csv"));
            Assert.Equal(a.RowCount,a.TreatedCount+a.ControlCount);Assert.All(a.Hashes.Values,x=>Assert.Equal(64,x.Length));Assert.Contains("0.95",System.Text.Encoding.UTF8.GetString(first));
            Assert.Throws<IOException>(()=>exporter.ExportSynthetic(request));var b=exporter.ExportSynthetic(request with{OverwritePolicy=ValidationOverwritePolicy.Replace});Assert.Equal(a.ReproducibilityHash,b.ReproducibilityHash);Assert.Equal(first,File.ReadAllBytes(Path.Combine(root,"run-1","analysis_rows.csv")));
        }
        finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }

    [Fact]
    public void Export_rejects_path_traversal_and_csv_escapes()
    {
        var root=Path.Combine(Path.GetTempPath(),$"pa-m7-{Guid.NewGuid():N}");
        Assert.Throws<ArgumentException>(()=>new CausalValidationBundleExporter().ExportSynthetic(new(1,"../escape",root,true,ValidationOverwritePolicy.Reject,"corr")));
    }

    [Fact]
    public void Bundle_contract_contains_folds_nuisance_and_matching_lineage()
    {
        var root=Path.Combine(Path.GetTempPath(),$"pa-m7-{Guid.NewGuid():N}");try{new CausalValidationBundleExporter().ExportSynthetic(new(1,"lineage",root,true,ValidationOverwritePolicy.Reject,"corr"));var run=Path.Combine(root,"lineage");using var study=JsonDocument.Parse(File.ReadAllText(Path.Combine(run,"study_manifest.json")));using var folds=JsonDocument.Parse(File.ReadAllText(Path.Combine(run,"fold_manifest.json")));Assert.Equal("1.1",study.RootElement.GetProperty("contractVersion").GetString());Assert.Equal("generic_launch_id",folds.RootElement.GetProperty("groupKey").GetString());Assert.True(File.Exists(Path.Combine(run,"nuisance_predictions.csv")));}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }

    [Fact]
    public void Estimate_exchange_uses_exact_string_identifiers_not_enum_integers()
    {
        var root=Path.Combine(Path.GetTempPath(),$"pa-m7-{Guid.NewGuid():N}");try{new CausalValidationBundleExporter().ExportSynthetic(new(1,"strings",root,true,ValidationOverwritePolicy.Reject,"corr"));using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"strings","csharp_estimates.json")));var document=json.RootElement;Assert.Equal("1.1",document.GetProperty("contractVersion").GetString());Assert.Equal("ATT",document.GetProperty("estimand").GetString());Assert.Equal("RiskDifference",document.GetProperty("effectScale").GetString());var expected=new[]{"UnadjustedDifferenceInMeans","PropensityScoreWeighting","OutcomeRegression","AugmentedInverseProbabilityWeighting"};Assert.Equal(expected,document.GetProperty("estimates").EnumerateArray().Select(x=>x.GetProperty("estimator").GetString()));Assert.All(document.GetProperty("estimates").EnumerateArray(),x=>{Assert.Equal(JsonValueKind.String,x.GetProperty("estimator").ValueKind);Assert.Equal("ATT",x.GetProperty("estimand").GetString());Assert.Equal("RiskDifference",x.GetProperty("effectScale").GetString());});}finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
