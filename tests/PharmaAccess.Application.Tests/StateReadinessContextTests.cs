using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class StateReadinessContextTests
{
    [Fact]
    public void Participation_counts_observable_launches_and_preserves_explicit_zero()
    {
        var result = Calculate([
            H(1, true, true, 2), H(2, true, false, null),
            H(3, false, false, null)
        ]).Single();
        Assert.Equal(2, result.HistoricalLaunchCount);
        Assert.Equal(1, result.HistoricalParticipatingLaunchCount);
        Assert.Equal(.5m, result.HistoricalParticipationRate);
    }

    [Fact]
    public void Participation_is_unavailable_when_no_observable_denominator()
    {
        var result = Calculate([H(1, false, false, null)]).Single();
        Assert.Equal(0, result.HistoricalLaunchCount);
        Assert.Null(result.HistoricalParticipationRate);
    }

    [Fact]
    public void Suppressed_only_history_cannot_be_counted_as_participation()
    {
        Assert.Throws<InvalidDataException>(() => Calculate([H(1, false, true, null)]));
    }

    [Theory]
    [InlineData(new[] { 1, 3, 8 }, 3)]
    [InlineData(new[] { 1, 3, 8, 10 }, 5.5)]
    public void Typical_delay_is_the_median_of_observed_first_entries(int[] delays, double expected)
    {
        var result = Calculate(delays.Select((x, i) => H(i + 1, true, true, x)).ToArray()).Single();
        Assert.Equal(delays.Length, result.EntryDelayObservationCount);
        Assert.Equal((decimal)expected, result.TypicalObservedEntryDelayQuarters);
    }

    [Fact]
    public void Typical_delay_is_unavailable_without_entries()
    {
        var result = Calculate([H(1, true, false, null)]).Single();
        Assert.Null(result.TypicalObservedEntryDelayQuarters);
        Assert.Equal(0, result.EntryDelayObservationCount);
    }

    [Theory]
    [InlineData("0.4", 5, "0.4")]
    [InlineData(null, 5, null)]
    [InlineData("0.4", 0, null)]
    public void Nearby_adoption_obeys_peer_and_null_semantics(string? share, int peers, string? expected)
    {
        var candidate = new StateReadinessCandidate(48, "TX", share is null ? null : decimal.Parse(share), peers);
        var result = StateReadinessContextCalculator.Calculate(20244, [H(1, true, true, 2)], [candidate]).Single();
        Assert.Equal(expected is null ? null : decimal.Parse(expected), result.NearbyAdoptionShare);
        Assert.Equal(peers, result.EligibleNeighborCount);
    }

    [Fact]
    public void Historical_profile_is_launch_invariant_while_neighbor_context_can_differ()
    {
        var history = new[] { H(1, true, true, 2), H(2, true, false, null) };
        var a = StateReadinessContextCalculator.Calculate(20244, history, [new(48, "TX", .2m, 5)]).Single();
        var b = StateReadinessContextCalculator.Calculate(20244, history, [new(48, "TX", .8m, 5)]).Single();
        Assert.Equal(a.HistoricalParticipationRate, b.HistoricalParticipationRate);
        Assert.Equal(a.TypicalObservedEntryDelayQuarters, b.TypicalObservedEntryDelayQuarters);
        Assert.NotEqual(a.NearbyAdoptionShare, b.NearbyAdoptionShare);
    }

    private static IReadOnlyList<StateReadinessStateContext> Calculate(IReadOnlyCollection<StateReadinessHistoricalLaunch> history) =>
        StateReadinessContextCalculator.Calculate(20244, history, [new(48, "TX", .4m, 5)]);

    private static StateReadinessHistoricalLaunch H(int launch, bool observable, bool participated, int? delay) =>
        new(48, "TX", launch, observable, participated, delay);
}
