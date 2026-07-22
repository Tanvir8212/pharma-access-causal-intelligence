using PharmaAccess.Data.Research;
using Xunit;

namespace PharmaAccess.Data.Tests;

public sealed class RealProtocolDefinitionTests
{
    [Fact]
    public void Resolved_real_protocol_matches_the_application_schema_and_reviewed_boundaries()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "config", "research-protocols", "approval-to-access-real-1.0.json");
        var raw = File.ReadAllText(path);
        var protocol = ResearchProtocolCommandService.ReadDocument(path);

        Assert.Equal("approval-to-access-real", protocol.ProtocolCode);
        Assert.Equal("1.0", protocol.ProtocolVersion);
        Assert.DoesNotContain("HUMAN_DECISION_REQUIRED", raw, StringComparison.Ordinal);
        Assert.Empty(protocol.RemainingReviewQuestions);
        Assert.Equal("2021-01-01", protocol.StudyPeriod.GetProperty("fdaApprovalCohortStart").GetString());
        Assert.Equal("2024-12-31", protocol.StudyPeriod.GetProperty("fdaApprovalCohortEnd").GetString());
        Assert.Contains("no GenericLaunchId overlap", protocol.Predictive.GetProperty("split").GetString());
        Assert.Contains("minimum eligible-peer count 2", protocol.Causal.GetProperty("treatment").GetString());
        Assert.Contains("lag 1 quarter", protocol.Causal.GetProperty("treatment").GetString());
        Assert.Contains("minimum counts 1 and 3", protocol.SensitivityAnalyses.GetRawText());
        Assert.Contains("Ambiguous/Unresolved", protocol.DataPolicies.GetProperty("ambiguousNdc").GetString());
        Assert.Contains("documented protocol amendment or new protocol version", protocol.DataPolicies.GetProperty("ndcMappingChange").GetString());
    }

    [Fact]
    public void Product_identity_amendment_is_an_approved_fda_only_path_and_preserves_version_one()
    {
        var root = FindRepositoryRoot();
        var one = File.ReadAllText(Path.Combine(root,"config","research-protocols","approval-to-access-real-1.0.json"));
        var path = Path.Combine(root,"config","research-protocols","approval-to-access-real-1.1.json");
        var draft = File.ReadAllText(path); var protocol = ResearchProtocolCommandService.ReadDocument(path);
        Assert.Equal("1.0", ResearchProtocolCommandService.ReadDocument(Path.Combine(root,"config","research-protocols","approval-to-access-real-1.0.json")).ProtocolVersion);
        Assert.Equal("1.1",protocol.ProtocolVersion); Assert.Contains("\"status\": \"Approved\"",draft); Assert.DoesNotContain("HUMAN_DECISION_REQUIRED",draft);
        Assert.Equal("1.0", protocol.AmendsProtocolVersion); Assert.True(protocol.MappingRules.HasValue); Assert.True(protocol.Reproducibility.HasValue);
        Assert.Contains("ndcDirectoryIsApprovalEvidence\": false",draft); Assert.Contains("fuzzyAutoApproval\": false",draft);
        Assert.Contains("ambiguousTenDigitNdc\": \"Unresolved\"",draft); Assert.Contains("invalidNdc\": \"Unresolved\"",draft);
        Assert.Contains("multipleCandidates\": \"Unresolved\"",draft); Assert.Contains("automaticEvidenceRequired\": true",draft);
        Assert.Contains("liveApiDuringAnalysis\": false",draft); Assert.Contains("sourceHashesRequired\": true",draft);
        Assert.Contains("Required primary: FDA Orange Book", draft); Assert.Contains("Optional conditional secondary source", draft);
        Assert.Contains("Approved by Tanvir Mahmud Khan", draft); Assert.Contains("not a prerequisite", draft);
        Assert.Contains("\"datasetFinalized\":false", File.ReadAllText(Path.Combine(root,"artifacts","research-audit","m9-reference-source-blockers.json")), StringComparison.OrdinalIgnoreCase);
        var acquisition = File.ReadAllText(Path.Combine(root,"scripts","Invoke-ProductIdentitySourceAcquisition.ps1"));
        Assert.DoesNotContain("execute-real-import", acquisition, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("train", acquisition, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("estimate-causal", acquisition, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(one,draft);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PharmaAccess.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
