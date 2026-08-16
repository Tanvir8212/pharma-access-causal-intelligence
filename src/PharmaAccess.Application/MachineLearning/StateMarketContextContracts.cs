namespace PharmaAccess.Application.MachineLearning;

public enum MarketDataSupport { High, Moderate, Limited }

public sealed record StateMarketQuarterVolume(
    int Quarter, long? Volume, bool HasMissingOrSuppressedValues, int SourceFileCount = 1);

public sealed record StateMarketContextSource(
    int StateId, string StateCode, long HistoricalBaselineVolume, decimal HistoricalBaselineMarketWeight,
    decimal StateDataCompleteness, IReadOnlyList<StateMarketQuarterVolume> QuarterVolumes);

public sealed record StateMarketContext(
    int StateId, string StateCode, string StateName, int AsOfQuarter,
    long? RecentMarketVolume, decimal? MarketSizeScore,
    long HistoricalBaselineVolume, decimal HistoricalBaselineMarketWeight,
    decimal? StateMarketGrowthRate, decimal? GrowthMomentumScore,
    long? RecentWindowVolume, long? PriorWindowVolume, int GrowthObservationQuarterCount,
    decimal StateDataCompleteness, MarketDataSupport DataSupport, string MarketProfile);

public sealed record StateMarketContextRequest(int GenericLaunchId, int AsOfQuarter);
public sealed record StateMarketContextResponse(
    int GenericLaunchId, int AsOfQuarter, int ComparisonPopulationCount,
    IReadOnlyList<StateMarketContext> States, IReadOnlyList<string> Warnings);

public interface IStateMarketContextService
{
    Task<StateMarketContextResponse> GetAsync(StateMarketContextRequest request, CancellationToken cancellationToken = default);
}

public static class StateMarketContextCalculator
{
    public const decimal HighCompletenessThreshold = .90m;
    public const decimal ModerateCompletenessThreshold = .75m;

    public static IReadOnlyList<StateMarketContext> Calculate(
        int asOfQuarter, IReadOnlyCollection<StateMarketContextSource> fullJurisdictionPopulation)
    {
        ValidateQuarter(asOfQuarter);
        var expectedEight = PreviousQuarters(asOfQuarter, 8);
        var expectedRecent = expectedEight.Skip(4).ToArray();
        if (fullJurisdictionPopulation.SelectMany(x => x.QuarterVolumes).Any(x => x.SourceFileCount != 1))
            throw new InvalidDataException("Authoritative market context requires exactly one governed source file per state-quarter.");
        var working = fullJurisdictionPopulation.OrderBy(x => x.StateCode, StringComparer.Ordinal).Select(source =>
        {
            var byQuarter = source.QuarterVolumes.Where(x => x.Quarter <= asOfQuarter)
                .GroupBy(x => x.Quarter).ToDictionary(x => x.Key, x => x.Single());
            var recentObservations = expectedRecent.Where(byQuarter.ContainsKey).Select(x => byQuarter[x]).ToArray();
            var eightObservations = expectedEight.Where(byQuarter.ContainsKey).Select(x => byQuarter[x]).ToArray();
            long? recent = recentObservations.Length == 4 && recentObservations.All(x => x.Volume.HasValue)
                ? recentObservations.Sum(x => x.Volume!.Value) : null;
            long? prior = null; decimal? growth = null;
            if (eightObservations.Length == 8 && eightObservations.All(x => x.Volume.HasValue))
            {
                prior = eightObservations.Take(4).Sum(x => x.Volume!.Value);
                if (prior > 0) growth = (decimal)(recent!.Value - prior.Value) / prior.Value;
            }
            return new Working(source, recent, growth, prior, eightObservations.Length,
                recentObservations.Any(x => x.HasMissingOrSuppressedValues),
                eightObservations.Any(x => x.HasMissingOrSuppressedValues));
        }).ToArray();

        var rankedSize = PercentileRanks(working.Where(x => x.Recent.HasValue)
            .Select(x => (x.Source.StateId, (decimal)x.Recent!.Value)));
        var rankedGrowth = PercentileRanks(working.Where(x => x.Growth.HasValue)
            .Select(x => (x.Source.StateId, x.Growth!.Value)));

        return working.Select(x =>
        {
            decimal? sizeScore = rankedSize.TryGetValue(x.Source.StateId, out var size) ? size : null;
            decimal? growthScore = rankedGrowth.TryGetValue(x.Source.StateId, out var growth) ? growth : null;
            var completeSize = x.Recent.HasValue;
            var completeGrowth = x.ObservationCount == 8 && x.Growth.HasValue;
            var support = DataSupport(x.Source.StateDataCompleteness, completeSize, completeGrowth,
                x.RecentHasMissingOrSuppressed || x.GrowthHasMissingOrSuppressed);
            return new StateMarketContext(x.Source.StateId, x.Source.StateCode,
                VerifiedStateReference.Get(x.Source.StateId).StateName, asOfQuarter,
                x.Recent, sizeScore, x.Source.HistoricalBaselineVolume,
                x.Source.HistoricalBaselineMarketWeight, x.Growth, growthScore, x.Recent, x.Prior,
                x.ObservationCount, x.Source.StateDataCompleteness, support, Profile(sizeScore, growthScore));
        }).ToArray();
    }

    public static Dictionary<int, decimal> PercentileRanks(IEnumerable<(int Id, decimal Value)> values)
    {
        var ordered = values.OrderBy(x => x.Value).ThenBy(x => x.Id).ToArray();
        var result = new Dictionary<int, decimal>();
        if (ordered.Length < 2) return result;
        for (var start = 0; start < ordered.Length;)
        {
            var end = start;
            while (end + 1 < ordered.Length && ordered[end + 1].Value == ordered[start].Value) end++;
            var percentile = decimal.Round(((decimal)start + end) / 2 / (ordered.Length - 1) * 100, 2, MidpointRounding.AwayFromZero);
            for (var i = start; i <= end; i++) result[ordered[i].Id] = percentile;
            start = end + 1;
        }
        return result;
    }

    public static MarketDataSupport DataSupport(decimal completeness, bool completeSize, bool completeGrowth, bool hasMissingOrSuppressed) =>
        completeness < ModerateCompletenessThreshold || !completeSize || !completeGrowth ? MarketDataSupport.Limited :
        completeness >= HighCompletenessThreshold && !hasMissingOrSuppressed ? MarketDataSupport.High : MarketDataSupport.Moderate;

    public static string Profile(decimal? size, decimal? growth) => size is null ? "Market context unavailable" :
        growth is null ? "Growth history unavailable" : size >= 50 && growth >= 50 ? "Large & Growing" :
        size >= 50 ? "Large & Established" : growth >= 50 ? "Emerging" : "Smaller / Stable";

    public static int[] PreviousQuarters(int asOfQuarter, int count)
    {
        ValidateQuarter(asOfQuarter); var result = new int[count]; var current = asOfQuarter;
        for (var i = count - 1; i >= 0; i--) { result[i] = current; current = current % 10 == 1 ? (current / 10 - 1) * 10 + 4 : current - 1; }
        return result;
    }

    private static void ValidateQuarter(int quarter)
    {
        if (quarter < 10001 || quarter % 10 is < 1 or > 4) throw new ArgumentException("As-of quarter is invalid.");
    }

    private sealed record Working(StateMarketContextSource Source, long? Recent, decimal? Growth, long? Prior,
        int ObservationCount, bool RecentHasMissingOrSuppressed, bool GrowthHasMissingOrSuppressed);
}
