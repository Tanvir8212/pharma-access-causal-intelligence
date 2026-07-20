namespace PharmaAccess.Application.Features;

public sealed record EligibleStateCandidate(int StateId, string StateCode, bool IsEligible, string? ExclusionReason, decimal DataCompleteness);
public sealed record EligibleStateDecision(int StateId, bool Included, string Reason);

public sealed class EligibleStateResolver(decimal minimumCompleteness)
{
    public IReadOnlyList<EligibleStateDecision> Resolve(IEnumerable<EligibleStateCandidate> states)
    {
        if (minimumCompleteness is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(minimumCompleteness));
        return states.Select(x => !x.IsEligible
            ? new EligibleStateDecision(x.StateId, false, x.ExclusionReason ?? "State is scientifically ineligible under configured policy.")
            : x.DataCompleteness < minimumCompleteness
                ? new EligibleStateDecision(x.StateId, false, $"Completeness {x.DataCompleteness} is below {minimumCompleteness}.")
                : new EligibleStateDecision(x.StateId, true, "Eligible and sufficiently complete.")).ToArray();
    }
}

public interface IStateAdjacencyProvider { IReadOnlyCollection<int> GetNeighbors(int stateId); }

public sealed class ConfiguredStateAdjacencyProvider(IReadOnlyDictionary<int, IReadOnlyCollection<int>> adjacency) : IStateAdjacencyProvider
{
    public IReadOnlyCollection<int> GetNeighbors(int stateId) => adjacency.TryGetValue(stateId, out var neighbors) ? neighbors.Order().ToArray() : [];
}

public sealed record HistoricalStateVector(int StateId, IReadOnlyList<decimal> Values);
public sealed record StateSimilarityResult(IReadOnlyList<int> StateIds, int ComparableStateCount, string Diagnostic);

public sealed class HistoricalStateSimilarityService
{
    public StateSimilarityResult FindSimilar(int targetStateId, IReadOnlyCollection<HistoricalStateVector> vectors, int topK, int minimumComparableStates)
    {
        if (topK <= 0 || minimumComparableStates <= 0) throw new ArgumentOutOfRangeException();
        var target = vectors.SingleOrDefault(x => x.StateId == targetStateId);
        if (target is null) return new([], vectors.Count - 1, "Target history is unavailable.");
        var peers = vectors.Where(x => x.StateId != targetStateId && x.Values.Count == target.Values.Count).OrderBy(x => x.StateId).ToArray();
        if (peers.Length < minimumComparableStates) return new([], peers.Length, "Insufficient comparable historical states.");
        var all = peers.Append(target).ToArray();
        var means = Enumerable.Range(0, target.Values.Count).Select(i => all.Average(x => x.Values[i])).ToArray();
        var scales = Enumerable.Range(0, target.Values.Count).Select(i => (decimal)Math.Sqrt((double)all.Average(x => (x.Values[i] - means[i]) * (x.Values[i] - means[i])))).ToArray();
        var ranked = peers.Select(peer => new { peer.StateId, Distance = Enumerable.Range(0, target.Values.Count).Sum(i => scales[i] == 0 ? 0 : (double)((peer.Values[i] - target.Values[i]) / scales[i]) * (double)((peer.Values[i] - target.Values[i]) / scales[i])) }).OrderBy(x => x.Distance).ThenBy(x => x.StateId).Take(topK).Select(x => x.StateId).ToArray();
        return new(ranked, peers.Length, "Standardized Euclidean distance using historical cutoff vectors; labels excluded.");
    }
}
