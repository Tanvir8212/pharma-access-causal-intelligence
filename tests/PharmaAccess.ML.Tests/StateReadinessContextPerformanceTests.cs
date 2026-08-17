using System.Diagnostics;
using PharmaAccess.ML;
using Xunit;
using Xunit.Abstractions;

namespace PharmaAccess.ML.Tests;

[Trait("Category", "LocalResearchIntegration")]
public sealed class StateReadinessContextPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Real_panel_is_interactive_cached_and_state_history_is_launch_invariant()
    {
        var service = new StateReadinessContextService(true, true);
        var timer = Stopwatch.StartNew();
        var first = await service.GetAsync(new(1, 20244));
        var cold = timer.Elapsed;
        timer.Restart();
        var warm = await service.GetAsync(new(1, 20244));
        var warmTime = timer.Elapsed;
        timer.Restart();
        var otherLaunch = await service.GetAsync(new(12, 20244));
        var sharedHistoryTime = timer.Elapsed;

        var texas = first.States.Single(x => x.StateCode == "TX");
        var texasWarm = warm.States.Single(x => x.StateCode == "TX");
        var texasOther = otherLaunch.States.Single(x => x.StateCode == "TX");
        Assert.Equal(texas.HistoricalParticipationRate, texasWarm.HistoricalParticipationRate);
        Assert.Equal(texas.TypicalObservedEntryDelayQuarters, texasWarm.TypicalObservedEntryDelayQuarters);
        Assert.Equal(texas.HistoricalParticipationRate, texasOther.HistoricalParticipationRate);
        Assert.Equal(texas.TypicalObservedEntryDelayQuarters, texasOther.TypicalObservedEntryDelayQuarters);
        Assert.NotEqual(texas.NearbyAdoptionShare, texasOther.NearbyAdoptionShare);
        Assert.True(cold < TimeSpan.FromSeconds(10), $"Cold readiness load was {cold}.");
        Assert.True(warmTime < TimeSpan.FromSeconds(1), $"Warm readiness load was {warmTime}.");
        output.WriteLine($"COLD_MS={cold.TotalMilliseconds:0.0}; WARM_MS={warmTime.TotalMilliseconds:0.0}; SHARED_HISTORY_MS={sharedHistoryTime.TotalMilliseconds:0.0}");
        output.WriteLine($"TX={texas.HistoricalParticipatingLaunchCount}/{texas.HistoricalLaunchCount}; MEDIAN={texas.TypicalObservedEntryDelayQuarters}; NEIGHBORS_L1={texas.NearbyAdoptionShare}; NEIGHBORS_L12={texasOther.NearbyAdoptionShare}");
    }
}
