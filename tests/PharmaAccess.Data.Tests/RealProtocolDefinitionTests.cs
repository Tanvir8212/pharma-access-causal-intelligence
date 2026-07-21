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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PharmaAccess.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
