using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class HistoricalOutcomeDerivationTests
{
    private static readonly HistoricalReplayStateResult[] Ranked =
    [
        new(6, "CA", "California", .09f, true, 1),
        new(36, "NY", "New York", .07f, false, 2),
        new(26, "MI", "Michigan", .03f, false, 3)
    ];

    [Fact] public void No_actual_entry_is_neutral_and_threshold_decision_is_incorrect_when_any_state_crossed_threshold()
    {
        var result=HistoricalOutcomeDerivation.Create(Ranked,[],20251);Assert.False(result.AnyActualEntryObserved);Assert.Empty(result.ActualObservedStates);Assert.Null(result.BestActualObservedRank);Assert.False(result.TopPredictedStateActuallyEntered);Assert.False(result.ThresholdDecisionCorrect);Assert.Contains("No eligible state",result.HistoricalOutcomeSummary);
    }

    [Fact] public void One_actual_entry_reports_safe_identity_rank_and_incorrect_top_prediction()
    {
        var result=HistoricalOutcomeDerivation.Create(Ranked,[36],20251);Assert.Equal(1,result.ActualObservedEntryCount);Assert.Equal(new HistoricalObservedState(2,36,"NY","New York"),Assert.Single(result.ActualObservedStates));Assert.Equal(2,result.BestActualObservedRank);Assert.False(result.TopPredictedStateActuallyEntered);Assert.True(result.ThresholdDecisionCorrect);
    }

    [Fact] public void Multiple_actual_entries_are_ordered_by_canonical_rank_and_top_prediction_is_correct()
    {
        var result=HistoricalOutcomeDerivation.Create(Ranked,[26,6],20251);Assert.Equal([1,3],result.ActualObservedStates.Select(x=>x.Rank));Assert.True(result.TopPredictedStateActuallyEntered);Assert.Equal(1,result.BestActualObservedRank);Assert.True(result.ThresholdDecisionCorrect);
    }

    [Fact] public void Threshold_decision_covers_correct_and_incorrect_without_a_threshold_crossing()
    {
        var below=Ranked.Select(x=>x with{AboveSelectedThreshold=false}).ToArray();Assert.True(HistoricalOutcomeDerivation.Create(below,[],20251).ThresholdDecisionCorrect);Assert.False(HistoricalOutcomeDerivation.Create(below,[36],20251).ThresholdDecisionCorrect);
    }
}
