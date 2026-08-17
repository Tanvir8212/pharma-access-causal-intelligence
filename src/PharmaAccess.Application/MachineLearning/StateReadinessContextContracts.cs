namespace PharmaAccess.Application.MachineLearning;

public sealed record StateReadinessHistoricalLaunch(
    int StateId, string StateCode, int GenericLaunchId, bool HasObservableHistory,
    bool HasParticipated, int? FirstEntryDelayQuarters);

public sealed record StateReadinessCandidate(
    int StateId, string StateCode, decimal? NearbyAdoptionShare, int EligibleNeighborCount);

public sealed record StateReadinessStateContext(
    int StateId, string StateCode, string StateName, int AsOfQuarter,
    decimal? HistoricalParticipationRate, int HistoricalLaunchCount,
    int HistoricalParticipatingLaunchCount, decimal? TypicalObservedEntryDelayQuarters,
    int EntryDelayObservationCount, decimal? NearbyAdoptionShare, int EligibleNeighborCount);

public sealed record StateReadinessContextRequest(int GenericLaunchId, int AsOfQuarter);
public sealed record StateReadinessContextResponse(
    int GenericLaunchId, int AsOfQuarter, IReadOnlyList<StateReadinessStateContext> States,
    IReadOnlyList<string> Warnings);

public interface IStateReadinessContextService
{
    Task<StateReadinessContextResponse> GetAsync(StateReadinessContextRequest request, CancellationToken cancellationToken = default);
}

public static class StateReadinessContextCalculator
{
    public static IReadOnlyList<StateReadinessStateContext> Calculate(
        int asOfQuarter, IReadOnlyCollection<StateReadinessHistoricalLaunch> history,
        IReadOnlyCollection<StateReadinessCandidate> candidates)
    {
        if (asOfQuarter < 10001 || asOfQuarter % 10 is < 1 or > 4)
            throw new ArgumentException("As-of quarter is invalid.");
        if (history.Any(x => x.HasParticipated && !x.HasObservableHistory))
            throw new InvalidDataException("Participation requires observable history.");

        var profiles = history.Where(x => x.HasObservableHistory).GroupBy(x => new { x.StateId, x.StateCode })
            .ToDictionary(x => x.Key.StateId, x =>
            {
                var launches = x.GroupBy(y => y.GenericLaunchId).Select(y => new
                {
                    Participated = y.Any(z => z.HasParticipated),
                    Delay = y.Where(z => z.FirstEntryDelayQuarters.HasValue)
                        .Select(z => z.FirstEntryDelayQuarters!.Value).Cast<int?>().Min()
                }).ToArray();
                var participating = launches.Count(y => y.Participated);
                var delays = launches.Where(y => y.Delay.HasValue).Select(y => y.Delay!.Value).Order().ToArray();
                return new Profile(x.Key.StateCode, launches.Length, participating, Median(delays), delays.Length);
            });

        return candidates.OrderBy(x => x.StateCode, StringComparer.Ordinal).Select(candidate =>
        {
            profiles.TryGetValue(candidate.StateId, out var profile);
            var launchCount = profile?.LaunchCount ?? 0;
            decimal? participation = launchCount == 0 ? null : (decimal)profile!.ParticipatingCount / launchCount;
            var nearby = candidate.EligibleNeighborCount <= 0 || !candidate.NearbyAdoptionShare.HasValue
                ? null : candidate.NearbyAdoptionShare;
            return new StateReadinessStateContext(candidate.StateId, candidate.StateCode,
                VerifiedStateReference.Get(candidate.StateId).StateName, asOfQuarter, participation,
                launchCount, profile?.ParticipatingCount ?? 0, profile?.MedianDelay,
                profile?.DelayCount ?? 0, nearby, candidate.EligibleNeighborCount);
        }).ToArray();
    }

    private static decimal? Median(int[] values) => values.Length == 0 ? null : values.Length % 2 == 1
        ? values[values.Length / 2]
        : ((decimal)values[values.Length / 2 - 1] + values[values.Length / 2]) / 2;

    private sealed record Profile(string StateCode, int LaunchCount, int ParticipatingCount, decimal? MedianDelay, int DelayCount);
}
