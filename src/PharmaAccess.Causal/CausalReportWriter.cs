using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PharmaAccess.Application.Causal;

namespace PharmaAccess.Causal;

public sealed record CausalReportFile(string Name,string Path,string Sha256);
public sealed class CausalReportWriter
{
    public const string SyntheticNotice="Synthetic development data — not research results.";
    private static readonly string[] Names=["causal-study-protocol","cohort-construction","treatment-assignment","covariate-balance","positivity-overlap","estimator-comparison","sensitivity-analysis","placebo-tests","heterogeneous-effects","counterfactual-scenario","causal-reproducibility-manifest","causal-limitations"];
    public IReadOnlyList<CausalReportFile> Write(string root,RunCausalStudyCommand command,RunCausalStudyResult result)
    {
        root=Path.GetFullPath(root);Directory.CreateDirectory(root);var files=new List<CausalReportFile>();foreach(var name in Names){var json=JsonSerializer.Serialize(new{notice=SyntheticNotice,report=name,study="PeerStateExposureToNextQuarterEntry",command,result},new JsonSerializerOptions{WriteIndented=true});var path=Path.Combine(root,$"{name}.json");File.WriteAllText(path,json,new UTF8Encoding(false));File.WriteAllText(Path.Combine(root,$"{name}.md"),$"# {name}\n\n> **{SyntheticNotice}**\n\nObservational estimates depend on acknowledged identification assumptions, partial interference, overlap, measured confounding, and correct timing. They are not proof of causation or guaranteed policy effects.\n",new UTF8Encoding(false));files.Add(new(name,path,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))));}return files;
    }
}
