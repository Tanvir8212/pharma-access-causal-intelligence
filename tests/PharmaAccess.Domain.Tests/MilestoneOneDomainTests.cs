using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Services;
using PharmaAccess.Domain.ValueObjects;
using Xunit;

namespace PharmaAccess.Domain.Tests;

public sealed class CalendarQuarterTests
{
    [Fact] public void Q1_has_expected_dates() { var q = new CalendarQuarter(2026, 1); Assert.Equal(new DateOnly(2026, 1, 1), q.StartDate); Assert.Equal(new DateOnly(2026, 3, 31), q.EndDate); }
    [Fact] public void Q4_has_expected_dates() { var q = new CalendarQuarter(2026, 4); Assert.Equal(new DateOnly(2026, 10, 1), q.StartDate); Assert.Equal(new DateOnly(2026, 12, 31), q.EndDate); }
    [Fact] public void Leap_year_Q1_still_ends_March_31() => Assert.Equal(new DateOnly(2024, 3, 31), new CalendarQuarter(2024, 1).EndDate);
    [Fact] public void Creates_from_date() => Assert.Equal(new CalendarQuarter(2026, 3), CalendarQuarter.FromDate(new DateOnly(2026, 8, 15)));
    [Fact] public void Adds_one_quarter() => Assert.Equal(new CalendarQuarter(2026, 2), new CalendarQuarter(2026, 1).AddQuarters(1));
    [Fact] public void Adds_across_year() => Assert.Equal(new CalendarQuarter(2027, 1), new CalendarQuarter(2026, 4).AddQuarters(1));
    [Fact] public void Supports_negative_movement() => Assert.Equal(new CalendarQuarter(2025, 4), new CalendarQuarter(2026, 1).AddQuarters(-1));
    [Fact] public void Calculates_signed_distance() => Assert.Equal(5, new CalendarQuarter(2025, 4).DistanceTo(new CalendarQuarter(2027, 1)));
    [Fact] public void Compares_chronologically() => Assert.True(new CalendarQuarter(2025, 4) < new CalendarQuarter(2026, 1));
    [Theory] [InlineData(0)] [InlineData(5)] public void Rejects_invalid_quarter(int quarter) => Assert.Throws<ArgumentOutOfRangeException>(() => new CalendarQuarter(2026, quarter));
    [Fact] public void Parses_controlled_format() { var q = CalendarQuarter.Parse("2026-Q1"); Assert.Equal("2026-Q1", q.ToString()); }
    [Theory] [InlineData("2026Q1")] [InlineData("2026-Q0")] [InlineData("anything")] public void Rejects_invalid_parse(string value) => Assert.Throws<FormatException>(() => CalendarQuarter.Parse(value));
}

public sealed class PrimitiveValueObjectTests
{
    [Fact] public void State_code_normalizes_and_trims() => Assert.Equal("CA", new StateCode(" ca ").ToString());
    [Fact] public void State_code_equality_uses_normalized_value() => Assert.Equal(new StateCode("ny"), new StateCode("NY"));
    [Theory] [InlineData("")] [InlineData(" ")] [InlineData("C")] [InlineData("CAL")] public void State_code_rejects_invalid_values(string value) => Assert.ThrowsAny<ArgumentException>(() => new StateCode(value));
    [Theory] [InlineData(0d)] [InlineData(100d)] [InlineData(42.125d)] public void Percentage_accepts_valid_values(double value) => Assert.Equal(value, new Percentage(value).Value);
    [Theory] [InlineData(-0.01d)] [InlineData(100.01d)] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] public void Percentage_rejects_invalid_values(double value) => Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage(value));
    [Fact] public void Dataset_code_trims_and_compares() => Assert.Equal(new DatasetVersionCode("2026Q1-v1"), new DatasetVersionCode(" 2026Q1-v1 "));
    [Theory] [InlineData("")] [InlineData("bad code")] [InlineData("/unsafe")] public void Dataset_code_rejects_invalid_values(string value) => Assert.ThrowsAny<ArgumentException>(() => new DatasetVersionCode(value));
    [Theory] [InlineData(0d)] [InlineData(25d)] [InlineData(-25d)] public void Access_gap_accepts_scientific_range(double value) => Assert.Equal(value, new AccessGapValue(value).Value);
    [Theory] [InlineData(-100.1d)] [InlineData(100.1d)] public void Access_gap_rejects_out_of_range(double value) => Assert.Throws<ArgumentOutOfRangeException>(() => new AccessGapValue(value));
}

public sealed class EntityInvariantTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc);

    [Fact] public void Drug_requires_ingredient_and_trims_text() { var drug = new Drug(" acetaminophen ", UtcNow); Assert.Equal("acetaminophen", drug.NormalizedIngredient); Assert.ThrowsAny<ArgumentException>(() => new Drug(" ", UtcNow)); }
    [Fact] public void Drug_rejects_update_before_creation() { var drug = new Drug("a", UtcNow); Assert.Throws<ArgumentOutOfRangeException>(() => drug.MarkUpdated(UtcNow.AddSeconds(-1))); }
    [Fact] public void State_requires_name_and_normalizes_code() { var state = new State(new StateCode(" tx "), " Texas ", true, UtcNow); Assert.Equal("TX", state.StateCode.Value); Assert.Equal("Texas", state.StateName); Assert.ThrowsAny<ArgumentException>(() => new State(new StateCode("TX"), " ", true, UtcNow)); }
    [Fact] public void Dataset_follows_valid_lifecycle() { var version = Draft(); version.MarkValidating(); version.MarkValidated(10); version.FinalizeVersion(UtcNow.AddHours(1)); version.Archive(); Assert.Equal(DatasetVersionStatus.Archived, version.Status); Assert.NotNull(version.FinalizedAtUtc); }
    [Fact] public void Rejected_dataset_cannot_finalize() { var version = Draft(); version.MarkRejected(); Assert.Throws<InvalidOperationException>(() => version.FinalizeVersion(UtcNow)); }
    [Fact] public void Dataset_must_validate_before_finalization() => Assert.Throws<InvalidOperationException>(() => Draft().FinalizeVersion(UtcNow));
    [Fact] public void Source_file_validates_size_and_rows() { var hash = new string('A', 64); Assert.Throws<ArgumentOutOfRangeException>(() => new SourceFile(1, SourceType.Other, "file.csv", hash, -1, "1", UtcNow)); Assert.Throws<ArgumentException>(() => new SourceFile(1, SourceType.Other, "file.csv", hash, 1, "1", UtcNow, rowCount: 2, rejectedRowCount: 3)); }
    [Fact] public void Source_file_normalizes_sha256() { var file = new SourceFile(1, SourceType.FDAFirstGeneric, "file.csv", new string('a', 64), 1, "1", UtcNow); Assert.Equal(new string('A', 64), file.Sha256); }
    [Fact] public void Utilization_rejects_negative_counts() => Assert.Throws<ArgumentOutOfRangeException>(() => new StateDrugUtilization(1, null, 1, 1, -1, 0m, 1, false, DataQualityStatus.Unchecked, 1, UtcNow));
    [Fact] public void Utilization_preserves_decimal_reimbursement() { var item = new StateDrugUtilization(1, null, 1, 1, 5, -12.3456m, 1, false, DataQualityStatus.Warning, 1, UtcNow); Assert.Equal(-12.3456m, item.ReimbursementAmount); }
    [Fact] public void Job_transitions_explicitly_to_success() { var job = new JobRun("Import", "corr", UtcNow); job.MarkRunning(); job.MarkSucceeded(UtcNow.AddMinutes(1)); Assert.Equal(JobRunStatus.Succeeded, job.Status); Assert.NotNull(job.CompletedAtUtc); }
    [Fact] public void Job_rejects_completion_before_start() { var job = new JobRun("Import", "corr", UtcNow); Assert.Throws<ArgumentOutOfRangeException>(() => job.MarkSucceeded(UtcNow.AddMinutes(-1))); }
    [Fact] public void Job_cannot_complete_twice() { var job = new JobRun("Import", "corr", UtcNow); job.Cancel(UtcNow); Assert.Throws<InvalidOperationException>(() => job.MarkSucceeded(UtcNow)); }

    private static DatasetVersion Draft() => new(new DatasetVersionCode("2026Q3-v1"), "1", 1, UtcNow);
}

public sealed class DistributionMetricTests
{
    [Fact] public void Numeric_distribution_calculates_percentage() => Assert.Equal(25d, DistributionMetrics.NumericDistribution(1, 4).Value);
    [Theory] [InlineData(-1, 4)] [InlineData(5, 4)] public void Numeric_distribution_rejects_invalid_counts(int active, int eligible) => Assert.ThrowsAny<ArgumentException>(() => DistributionMetrics.NumericDistribution(active, eligible));
    [Fact] public void Numeric_distribution_rejects_zero_denominator() => Assert.Throws<InvalidOperationException>(() => DistributionMetrics.NumericDistribution(0, 0));
    [Fact] public void Weighted_distribution_uses_frozen_weights() { var values = new[] { new WeightedStateObservation(true, 3), new WeightedStateObservation(false, 1) }; Assert.Equal(75d, DistributionMetrics.WeightedDistribution(values).Value); }
    [Fact] public void Weighted_distribution_rejects_negative_weights() => Assert.Throws<ArgumentOutOfRangeException>(() => DistributionMetrics.WeightedDistribution([new(true, -1)]));
    [Fact] public void Weighted_distribution_rejects_zero_total() => Assert.Throws<InvalidOperationException>(() => DistributionMetrics.WeightedDistribution([new(false, 0)]));
    [Theory] [InlineData(50d, 50d, 0d)] [InlineData(75d, 50d, 25d)] [InlineData(25d, 50d, -25d)] public void Access_gap_is_weighted_minus_numeric(double weighted, double numeric, double expected) => Assert.Equal(expected, DistributionMetrics.AccessGap(new Percentage(weighted), new Percentage(numeric)).Value);
}
