using PharmaAccess.Application.Research;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class ResearchDatasetFinalizationTests
{
    [Fact] public void Finalization_contract_carries_human_decision_hashes_and_correlation()
    {
        var at=new DateTime(2026,7,23,0,0,0,DateTimeKind.Utc);var hash=new string('A',64);
        var command=new FinalizeResearchDatasetCommand("real-2021-2025-v1","Tanvir Mahmud Khan","final-closeout-001",at,"commit",hash,hash,hash,hash,hash,hash,hash,hash,"material discrepancy is not a finalization blocker",[new("metrics","artifacts/final/metrics.json",hash,1)]);
        Assert.Equal("final-closeout-001",command.CorrelationId);Assert.Contains("not a finalization blocker",command.HumanDecision);Assert.Single(command.Artifacts);
    }

    [Fact] public void Guarded_script_validates_before_should_process_and_dispatches_application_command()
    {
        var root=FindRoot();var text=File.ReadAllText(Path.Combine(root,"scripts","Finalize-ResearchDataset.ps1"));
        Assert.Contains("PHARMAACCESS_ALLOW_RESEARCH_DB_WRITE",text);Assert.Contains("ResearchDatabaseOwnership",text);Assert.Contains("$PSCmdlet.ShouldProcess",text);Assert.Contains("finalize-research-dataset",text);
        Assert.True(text.IndexOf("Preview gates passed",StringComparison.Ordinal)<text.IndexOf("$PSCmdlet.ShouldProcess",StringComparison.Ordinal));
    }

    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"PharmaAccess.sln")))d=d.Parent;return d?.FullName??throw new InvalidOperationException("Repository root not found.");}
}
