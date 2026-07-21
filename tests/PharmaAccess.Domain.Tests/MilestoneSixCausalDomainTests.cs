using PharmaAccess.Domain.Causal;
using Xunit;

namespace PharmaAccess.Domain.Tests;

public sealed class MilestoneSixCausalDomainTests
{
    [Fact] public void Study_lifecycle_requires_assumptions_and_finalization_is_immutable(){var s=Study();s.MarkValidating();Assert.Throws<InvalidOperationException>(s.MarkReady);s.AcknowledgeAssumptions();s.MarkReady();s.MarkEstimating();s.MarkEstimated();s.FinalizeStudy(DateTime.UtcNow);Assert.Equal(CausalStudyStatus.Finalized,s.Status);Assert.Throws<InvalidOperationException>(s.Reject);}
    [Fact] public void Diagnostics_failed_and_rejected_studies_cannot_finalize(){var a=Study();a.MarkDiagnosticsFailed();Assert.Throws<InvalidOperationException>(()=>a.FinalizeStudy(DateTime.UtcNow));var b=Study();b.Reject();Assert.Throws<InvalidOperationException>(()=>b.FinalizeStudy(DateTime.UtcNow));}
    [Fact] public void Treatment_normalizes_continuous_and_requires_pre_outcome_timing(){var d=new TreatmentDefinition("neighbor-v1",ExposureType.HighNeighborAdoptionExposure,.5,2,0,"adj-v1","eligible-only");var a=d.Assign(.6,3,10,11);Assert.True(a.BinaryValue);Assert.Equal(.6,a.ContinuousValue);Assert.Throws<InvalidOperationException>(()=>d.Assign(.6,3,11,11));Assert.False(d.Assign(.8,1,10,11).IsSupported);}
    [Fact] public void Temporal_policy_rejects_future_confounders_treatment_and_incomplete_window(){Assert.Contains(TemporalOrderPolicy.Validate(11,10,10,11,false),x=>x.Code=="PostTreatmentConfounder");Assert.Contains(TemporalOrderPolicy.Validate(9,11,10,11,false),x=>x.Code=="TreatmentAfterOutcome");Assert.Contains(TemporalOrderPolicy.Validate(9,10,10,12,false),x=>x.Code=="IncompleteOutcomeWindow");Assert.Contains(TemporalOrderPolicy.Validate(9,10,10,11,true),x=>x.Code=="FutureTreatmentInformation");}
    [Fact] public void Dag_requires_treatment_outcome_and_rejects_descendant_adjustment(){var dag=Dag();dag.Validate(["Baseline"]);Assert.Contains("digraph",dag.ToDot());Assert.Equal(64,dag.Hash().Length);Assert.Throws<InvalidOperationException>(()=>dag.Validate(["Mediator"]));}
    [Theory][InlineData(.5,SimulationSupportStatus.Supported)][InlineData(.15,SimulationSupportStatus.WeaklySupported)][InlineData(.95,SimulationSupportStatus.Extrapolative)][InlineData(1.5,SimulationSupportStatus.Unsupported)] public void Intervention_support_is_explicit(double value,SimulationSupportStatus expected)=>Assert.Equal(expected,new CounterfactualIntervention(.2,value).Classify(.1,.9));
    private static CausalStudy Study()=>new("peer-v1","Peer exposure","Does earlier peer adoption affect next-quarter entry?",1,1,"dag-v1","adjust-v1","neighbor-v1","next-entry-v1",EstimandType.AverageTreatmentEffectOnTreated,"Eligible not-yet-entered rows",1,4,DateTime.UtcNow);
    private static CausalDag Dag()=>new("dag-v1",[new("Treatment",DagNodeRole.Treatment,"spillover"),new("Outcome",DagNodeRole.Outcome,"next entry"),new("Baseline",DagNodeRole.Confounder,"history"),new("Mediator",DagNodeRole.Mediator,"post treatment")],[new("Baseline","Treatment",""),new("Baseline","Outcome",""),new("Treatment","Mediator",""),new("Mediator","Outcome","")]);
}
