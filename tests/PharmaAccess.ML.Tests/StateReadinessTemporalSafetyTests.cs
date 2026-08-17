using Xunit;

namespace PharmaAccess.ML.Tests;

public sealed class StateReadinessTemporalSafetyTests
{
    [Fact]
    public void Historical_profile_query_is_strictly_pre_as_of_and_outcome_blind()
    {
        var sql = StateReadinessContextService.BuildHistorySql(20244);
        Assert.Contains("p.ObservationQuarter<20244", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LabelNextQuarterEntry", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("20245", sql, StringComparison.Ordinal);
        Assert.Contains("p.IsFirstEntryQuarter=1", sql, StringComparison.Ordinal);
        Assert.Contains("p.IsSuppressed=0", sql, StringComparison.Ordinal);
        Assert.Contains("p.IsObservedZero=1 OR p.IsPresent=1", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Earlier_as_of_query_cannot_see_later_first_entry()
    {
        var earlier = StateReadinessContextService.BuildHistorySql(20242);
        var later = StateReadinessContextService.BuildHistorySql(20243);
        Assert.Contains("p.ObservationQuarter<20242", earlier, StringComparison.Ordinal);
        Assert.Contains("p.ObservationQuarter<20243", later, StringComparison.Ordinal);
        Assert.NotEqual(earlier, later);
    }

    [Fact]
    public void Nearby_adoption_uses_only_the_selected_launch_row_at_t()
    {
        var sql = StateReadinessContextService.BuildCandidateSql(291, 20244);
        Assert.Contains("p.AndaLaunchId=291", sql, StringComparison.Ordinal);
        Assert.Contains("p.ObservationQuarter=20244", sql, StringComparison.Ordinal);
        Assert.Contains("p.LaggedNeighborExposure", sql, StringComparison.Ordinal);
        Assert.Contains("p.EligiblePeerCount", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LabelNextQuarterEntry", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("20245", sql, StringComparison.Ordinal);
    }
}
