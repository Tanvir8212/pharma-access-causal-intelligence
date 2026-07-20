using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Services;

public readonly record struct WeightedStateObservation(bool IsActive, double FrozenBaselineMarketWeight);

public static class DistributionMetrics
{
    public static Percentage NumericDistribution(int activeEligibleStateCount, int eligibleStateCount)
    {
        if (activeEligibleStateCount < 0) throw new ArgumentOutOfRangeException(nameof(activeEligibleStateCount));
        if (eligibleStateCount < 0) throw new ArgumentOutOfRangeException(nameof(eligibleStateCount));
        if (activeEligibleStateCount > eligibleStateCount) throw new ArgumentException("Active count cannot exceed eligible count.");
        if (eligibleStateCount == 0) throw new InvalidOperationException("Numeric Distribution is undefined when no states are eligible.");

        return new Percentage(activeEligibleStateCount * 100d / eligibleStateCount);
    }

    public static Percentage WeightedDistribution(IEnumerable<WeightedStateObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        double total = 0d;
        double active = 0d;
        foreach (var observation in observations)
        {
            if (!double.IsFinite(observation.FrozenBaselineMarketWeight) || observation.FrozenBaselineMarketWeight < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(observations), "Weights must be finite and nonnegative.");
            }

            total += observation.FrozenBaselineMarketWeight;
            if (observation.IsActive) active += observation.FrozenBaselineMarketWeight;
        }

        if (total == 0d) throw new InvalidOperationException("Weighted Distribution is undefined when total frozen baseline weight is zero.");
        return new Percentage(active * 100d / total);
    }

    public static AccessGapValue AccessGap(Percentage weightedDistribution, Percentage numericDistribution) =>
        new(weightedDistribution.Value - numericDistribution.Value);
}
