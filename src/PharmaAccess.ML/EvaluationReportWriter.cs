using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PharmaAccess.ML;

public sealed record EvaluationReportFile(string Name, string Path, string Sha256);

public sealed class EvaluationReportWriter
{
    public const string SyntheticNotice = "Synthetic development data — not research results.";
    private static readonly string[] Names = ["calibration", "feature-importance", "subgroup-performance", "threshold-comparison", "error-analysis", "champion-challenger", "model-card", "reproducibility-manifest"];

    public IReadOnlyList<EvaluationReportFile> WriteSyntheticReports(string artifactRoot, object reproducibilityPayload)
    {
        if (string.IsNullOrWhiteSpace(artifactRoot)) throw new ArgumentException("Artifact root is required.", nameof(artifactRoot));
        var root = Path.GetFullPath(artifactRoot); Directory.CreateDirectory(root); var output = new List<EvaluationReportFile>();
        foreach (var name in Names)
        {
            var payload = JsonSerializer.Serialize(new { notice = SyntheticNotice, task = "NextQuarterStateEntry", report = name, generatedAtUtc = "deterministic-synthetic-workflow", reproducibility = reproducibilityPayload }, new JsonSerializerOptions { WriteIndented = true });
            var jsonPath = Path.Combine(root, $"{name}.json"); File.WriteAllText(jsonPath, payload, new UTF8Encoding(false));
            var markdownPath = Path.Combine(root, $"{name}.md"); File.WriteAllText(markdownPath, $"# {name}\n\n> **{SyntheticNotice}**\n\nTask: `NextQuarterStateEntry`\n\nThis development report is reproducible metadata and is not scientific evidence.\n", new UTF8Encoding(false));
            output.Add(new(name, jsonPath, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(jsonPath)))));
        }
        return output;
    }
}
