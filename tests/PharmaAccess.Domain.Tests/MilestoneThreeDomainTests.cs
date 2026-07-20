using PharmaAccess.Domain.Features;
using PharmaAccess.Domain.ValueObjects;
using Xunit;

namespace PharmaAccess.Domain.Tests;

public sealed class FeatureCalculationTests
{
    [Fact] public void Concentration_matches_hand_calculation() { var result = FeatureCalculations.Concentration([50m, 30m, 20m]); Assert.Equal(0.38m, result.ConcentrationIndex); Assert.Equal(0.5m, result.TopStateShare); Assert.Equal(1m, result.TopFiveStateShare); }
    [Fact] public void Zero_concentration_is_explicitly_undefined() { var result = FeatureCalculations.Concentration([0m, 0m]); Assert.Null(result.ConcentrationIndex); Assert.NotNull(result.UndefinedReason); }
    [Fact] public void Concentration_rejects_negative_values() => Assert.Throws<ArgumentOutOfRangeException>(() => FeatureCalculations.Concentration([1m, -1m]));
    [Fact] public void Growth_uses_ordinary_percent_change() => Assert.Equal(0.5m, FeatureCalculations.OrdinaryGrowth(15m, 10m).Value);
    [Fact] public void Growth_is_null_for_zero_denominator() => Assert.Null(FeatureCalculations.OrdinaryGrowth(5m, 0m).Value);
    [Fact] public void Growth_is_null_for_missing_lag() => Assert.Null(FeatureCalculations.OrdinaryGrowth(5m, null).Value);
    [Fact] public void Persistent_inequality_requires_consecutive_quarters() => Assert.True(FeatureCalculations.PersistentInequality([-10m, -12m, 2m], -5m, 2));
    [Fact] public void Frozen_weights_are_deterministic_and_windowed() { var values = new[] { (1, 10m, new CalendarQuarter(2024, 1)), (1, 5m, new CalendarQuarter(2024, 2)), (2, 20m, new CalendarQuarter(2024, 2)), (2, 999m, new CalendarQuarter(2025, 1)) }; var result = FeatureCalculations.FrozenMarketWeights(values, new(2024, 1), new(2024, 4)); Assert.Equal(15m, result[1]); Assert.Equal(20m, result[2]); }
    [Fact] public void Frozen_weights_fail_on_zero_total() => Assert.Throws<InvalidOperationException>(() => FeatureCalculations.FrozenMarketWeights([(1, 0m, new CalendarQuarter(2024, 1))], new(2024, 1), new(2024, 4)));
}

public sealed class StateEntryPolicyTests
{
    [Fact] public void Any_positive_marks_first_entry() { var result = new AnyPositiveUtilizationPolicy().Evaluate(new(1, 0m, false), false); Assert.True(result.IsPresent); Assert.True(result.IsFirstEntry); }
    [Fact] public void Suppression_remains_unknown_not_present() { var result = new AnyPositiveUtilizationPolicy().Evaluate(new(10, 10m, true), false); Assert.False(result.IsPresent); Assert.NotEmpty(result.Warnings); }
    [Fact] public void Threshold_policy_is_configurable() => Assert.False(new MinimumPrescriptionThresholdPolicy(10).Evaluate(new(9, 1m, false), false).IsPresent);
    [Fact] public void Combined_policy_requires_both_measures() => Assert.False(new PositivePrescriptionAndReimbursementPolicy().Evaluate(new(1, 0m, false), false).IsPresent);
    [Fact] public void Consecutive_policy_requires_confirmation() => Assert.True(new ConsecutiveQuarterConfirmationPolicy(2).Evaluate(new(1, 1m, false), false, 2).IsPresent);
}

public sealed class FeatureSetLifecycleTests
{
    private static readonly DateTime Now = new(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc);
    [Fact] public void Valid_lifecycle_finalizes_and_archives() { var value = Draft(); value.MarkBuilding(); value.MarkValidating(); value.MarkValidated(); value.FinalizeVersion(Now.AddMinutes(1)); value.Archive(); Assert.Equal(FeatureSetStatus.Archived, value.Status); }
    [Fact] public void Rejected_set_cannot_finalize() { var value = Draft(); value.MarkRejected(); Assert.Throws<InvalidOperationException>(() => value.FinalizeVersion(Now)); }
    [Fact] public void Validation_is_required_before_finalization() => Assert.Throws<InvalidOperationException>(() => Draft().FinalizeVersion(Now));
    private static FeatureSetVersion Draft() => new("feature-v1", 1, new string('A', 64), Now);
}
