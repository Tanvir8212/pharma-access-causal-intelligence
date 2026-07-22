using System.Text.Json;
using PharmaAccess.Application.Research;

namespace PharmaAccess.Infrastructure.Research;

public sealed class FinalResearchArtifactService(string artifactRoot) : IFinalResearchArtifactService
{
    public static string Locate(string start)
    {
        var directory=new DirectoryInfo(Path.GetFullPath(start));while(directory is not null){var candidate=Path.Combine(directory.FullName,"artifacts","final");if(File.Exists(Path.Combine(candidate,"final-artifact-manifest.json")))return candidate;directory=directory.Parent;}throw new DirectoryNotFoundException("Final research artifact directory was not found.");
    }
    public FinalResearchResults Read()
    {
        var predictivePath=Path.Combine(artifactRoot,"final-predictive-metrics.json");var causalPath=Path.Combine(artifactRoot,"final-causal-estimates.json");var pythonPath=Path.Combine(artifactRoot,"final-python-validation-report.json");
        if(!File.Exists(predictivePath)||!File.Exists(causalPath)||!File.Exists(pythonPath))throw new FileNotFoundException("Final research artifacts are unavailable.");
        using var p=JsonDocument.Parse(File.ReadAllText(predictivePath));using var c=JsonDocument.Parse(File.ReadAllText(causalPath));using var y=JsonDocument.Parse(File.ReadAllText(pythonPath));
        var m=p.RootElement.GetProperty("Selected").GetProperty("TestMetrics");var estimates=c.RootElement.GetProperty("Estimates");
        double E(int kind)=>estimates.EnumerateArray().Single(x=>x.GetProperty("Estimator").GetInt32()==kind).GetProperty("Estimate").GetDouble();var a=estimates.EnumerateArray().Single(x=>x.GetProperty("Estimator").GetInt32()==3);
        return new(new(m.GetProperty("RocAuc").GetDouble(),m.GetProperty("PrAuc").GetDouble(),m.GetProperty("LogLoss").GetDouble(),m.GetProperty("BrierScore").GetDouble(),m.GetProperty("Precision").GetDouble(),m.GetProperty("Recall").GetDouble(),m.GetProperty("F1").GetDouble(),m.GetProperty("Specificity").GetDouble()),new(E(3),a.GetProperty("ConfidenceLower").GetDouble(),a.GetProperty("ConfidenceUpper").GetDouble(),E(1),E(2),E(0),y.RootElement.GetProperty("estimate").GetDouble()),true);
    }
}
