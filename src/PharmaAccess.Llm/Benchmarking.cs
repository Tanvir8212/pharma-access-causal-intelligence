namespace PharmaAccess.Llm;

public sealed record BenchmarkObservation(
    bool ExpectedAnswerable,
    bool AnswerSupported,
    bool NumericalValuesExact,
    bool CitationsValid,
    bool HasUnsupportedClaim,
    bool HasCausalOverstatement,
    bool UsedFallback);

public sealed record BenchmarkMetrics(
    int Total,
    double AnswerSupportRate,
    double ExactNumericalAccuracy,
    double CitationValidity,
    double UnsupportedClaimRate,
    double CausalOverstatementRate,
    double FallbackRate);

public static class BenchmarkEvaluator
{
    public static BenchmarkMetrics Evaluate(IReadOnlyCollection<BenchmarkObservation> observations)
    {
        if (observations.Count == 0) return new(0, 0, 0, 0, 0, 0, 0);
        double Rate(Func<BenchmarkObservation, bool> predicate) => observations.Count(predicate) / (double)observations.Count;
        return new(observations.Count, Rate(x => x.AnswerSupported), Rate(x => x.NumericalValuesExact),
            Rate(x => x.CitationsValid), Rate(x => x.HasUnsupportedClaim), Rate(x => x.HasCausalOverstatement), Rate(x => x.UsedFallback));
    }
}
