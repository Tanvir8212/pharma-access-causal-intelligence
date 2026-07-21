using PharmaAccess.Domain.Research;
using Xunit;

namespace PharmaAccess.Domain.Tests;

public sealed class MilestoneEightResearchDomainTests
{
    [Fact]public void Protocol_requires_explicit_review_and_approval_and_is_immutable(){var p=Protocol();Assert.Throws<InvalidOperationException>(()=>p.Approve("actor",Now,"reason"));p.SubmitForReview();p.Approve("actor",Now.AddMinutes(1),"approved for synthetic verification");Assert.Equal(ResearchProtocolStatus.Approved,p.Status);Assert.NotNull(p.ApprovedAtUtc);Assert.Throws<InvalidOperationException>(p.SubmitForReview);}
    [Fact]public void Rejected_protocol_cannot_be_approved(){var p=Protocol();p.Reject("reviewer","rules unresolved");Assert.Throws<InvalidOperationException>(()=>p.Approve("reviewer",Now,"no"));}
    [Fact]public void Freeze_transitions_are_sequential_and_approval_requires_protocol(){var f=Freeze();Assert.Throws<InvalidOperationException>(()=>f.Advance(ResearchFreezeStatus.DatasetBuilt));Advance(f);var draft=Protocol();Assert.Throws<InvalidOperationException>(()=>f.Approve(draft,"actor",Now,"reason"));draft.SubmitForReview();draft.Approve("reviewer",Now,"reason");f.Approve(draft,"actor",Now,"synthetic approval");Assert.Equal(ResearchFreezeStatus.ReadyForAnalysis,f.Status);Assert.Throws<InvalidOperationException>(()=>f.RecordFindings(0,0));}
    [Fact]public void Blocking_finding_prevents_approval(){var p=Protocol();p.SubmitForReview();p.Approve("reviewer",Now,"reason");var f=Freeze();Advance(f);f.RecordFindings(1,0);Assert.Throws<InvalidOperationException>(()=>f.Approve(p,"actor",Now,"reason"));}
    [Fact]public void Synthetic_freeze_cannot_be_marked_real(){var p=Protocol();p.SubmitForReview();p.Approve("reviewer",Now,"reason");var f=Freeze();Advance(f);Assert.Throws<InvalidOperationException>(()=>f.Approve(p,"actor",Now,"reason",true));}
    [Fact]public void Changed_lineage_requires_new_hash(){var a=Freeze();var b=new ResearchDataFreeze("freeze","2",1,1,1,Sha('A'),Sha('B'),Sha('D'),Sha('D'),Sha('E'),Sha('F'),Sha('A'),Sha('B'),"commit",true,Now);Assert.NotEqual(a.ReproducibilityHash(),b.ReproducibilityHash());}
    private static readonly DateTime Now=new(2026,7,21,0,0,0,DateTimeKind.Utc);private static string Sha(char c)=>new(c,64);private static ResearchDataFreeze Freeze()=>new("freeze","1",1,1,1,Sha('A'),Sha('B'),Sha('C'),Sha('D'),Sha('E'),Sha('F'),Sha('A'),Sha('B'),"commit",true,Now);private static void Advance(ResearchDataFreeze f){for(var s=ResearchFreezeStatus.SourceFilesRegistered;s<=ResearchFreezeStatus.ValidationBundleGenerated;s++)f.Advance(s);}private static ResearchProtocol Protocol()=>new("protocol","1","Title","Question","Prediction","Causal question","PR_AUC","Brier","ATT","RiskDifference","AIPW","IPTW","Population","Window","[]","[]","state-v1","entry-v1","weight-v1",1,1,1,1,1,1,"CompleteCase","Null censored","Prespecified","Sensitivity","Subgroups","Stopping",Now);
}
