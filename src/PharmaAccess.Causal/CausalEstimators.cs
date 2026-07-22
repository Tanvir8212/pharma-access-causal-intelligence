using System.Security.Cryptography;
using System.Text;
using PharmaAccess.Application.Causal;
using PharmaAccess.Domain.Causal;

namespace PharmaAccess.Causal;

public sealed class LogisticNuisanceModel
{
    public PropensityResult Fit(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<string> variables, PropensityConfiguration configuration)
    {
        if (rows.Count == 0 || rows.All(x => x.BinaryTreatment) || rows.All(x => !x.BinaryTreatment)) throw new InvalidOperationException("Propensity fitting requires treated and control rows.");
        if (variables.Any(x => x.Contains("Outcome", StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Outcome fields are prohibited from the propensity model.");
        var means = variables.Select(v => rows.Average(r => r.Confounders[v])).ToArray(); var scales = variables.Select((v, j) => Math.Sqrt(rows.Average(r => Math.Pow(r.Confounders[v] - means[j], 2)))).Select(x => x == 0 ? 1 : x).ToArray();
        var beta = new double[variables.Count + 1]; var prevalence = rows.Count(x => x.BinaryTreatment) / (double)rows.Count; beta[0] = Math.Log(prevalence / (1 - prevalence));
        for (var iteration = 0; iteration < configuration.MaximumIterations; iteration++)
        {
            var gradient = new double[beta.Length]; foreach (var row in rows) { var x = Vector(row, variables, means, scales); var p = Sigmoid(Dot(beta, x)); var error = p - (row.BinaryTreatment ? 1 : 0); for (var j = 0; j < beta.Length; j++) gradient[j] += error * x[j]; }
            var maximumUpdate = 0d; for (var j = 0; j < beta.Length; j++) { var update=configuration.LearningRate*gradient[j]/rows.Count;beta[j]-=update;maximumUpdate=Math.Max(maximumUpdate,Math.Abs(update)); } if(maximumUpdate<1e-8)break;
        }
        var raw = rows.Select(r => Sigmoid(Dot(beta, Vector(r, variables, means, scales)))).ToArray(); var clipped = raw.Select(x => Math.Clamp(x, configuration.ClipLower, configuration.ClipUpper)).ToArray();
        var warnings = new List<string>(); if (raw.Any(x => x < .01 || x > .99)) warnings.Add("Quasi-separation or extreme untreated propensity probabilities detected."); if (raw.Where((_, i) => clipped[i] != raw[i]).Any()) warnings.Add($"Propensities were explicitly clipped to [{configuration.ClipLower}, {configuration.ClipUpper}].");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', variables) + $"|{configuration}")));
        return new(clipped, beta, hash, warnings);
    }
    internal static double Sigmoid(double value) => 1 / (1 + Math.Exp(-Math.Clamp(value, -35, 35)));
    private static double[] Vector(CausalAnalysisRowContract row, IReadOnlyList<string> variables, double[] means, double[] scales) => [1, .. variables.Select((v, i) => (row.Confounders[v] - means[i]) / scales[i])];
    private static double Dot(IReadOnlyList<double> x, IReadOnlyList<double> y) => x.Zip(y).Sum(z => z.First * z.Second);
}

public static class PropensityWeighting
{
    public static WeightResult Calculate(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<double> scores, EstimandType estimand, WeightingConfiguration configuration)
    {
        if (rows.Count != scores.Count || scores.Any(x => x is <= 0 or >= 1 || double.IsNaN(x))) throw new ArgumentException("Valid row-aligned propensity probabilities are required.");
        var weights = rows.Select((row, i) => estimand == EstimandType.AverageTreatmentEffectOnTreated ? row.BinaryTreatment ? 1d : scores[i] / (1 - scores[i]) : row.BinaryTreatment ? 1 / scores[i] : 1 / (1 - scores[i])).ToArray();
        if (configuration.Stabilized) { var prevalence = rows.Count(x => x.BinaryTreatment) / (double)rows.Count; weights = weights.Select((w, i) => w * (rows[i].BinaryTreatment ? prevalence : 1 - prevalence)).ToArray(); }
        var ess = Math.Pow(weights.Sum(), 2) / weights.Sum(x => x * x); var sorted = weights.Order().ToArray(); var p95 = sorted[(int)Math.Floor((sorted.Length - 1) * .95)]; var extreme = weights.Count(x => x > configuration.ExtremeWeightThreshold); var warnings = extreme > 0 ? new[] { $"{extreme} weights exceed {configuration.ExtremeWeightThreshold}; positivity may be weak." } : [];
        return new(weights, ess, weights.Max(), p95, extreme, warnings);
    }
}

public static class CausalDiagnostics
{
    public static IReadOnlyList<CovariateBalance> Balance(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<double> weights, IReadOnlyList<string> variables, double threshold)
    {
        return variables.Select(v =>
        {
            var t = rows.Select((r, i) => (r, i)).Where(x => x.r.BinaryTreatment).ToArray(); var c = rows.Select((r, i) => (r, i)).Where(x => !x.r.BinaryTreatment).ToArray();
            var tm = t.Average(x => x.r.Confounders[v]); var cm = c.Average(x => x.r.Confounders[v]); var tv = t.Average(x => Math.Pow(x.r.Confounders[v] - tm, 2)); var cv = c.Average(x => Math.Pow(x.r.Confounders[v] - cm, 2)); var pooled = Math.Sqrt((tv + cv) / 2); var smd = pooled == 0 ? 0 : (tm - cm) / pooled;
            var wtm = Weighted(t, weights, v); var wcm = Weighted(c, weights, v); var wtVar = WeightedVariance(t, weights, v, wtm); var wcVar = WeightedVariance(c, weights, v, wcm); var wp = Math.Sqrt((wtVar + wcVar) / 2); var wsmd = wp == 0 ? 0 : (wtm - wcm) / wp; double? ratio = cv == 0 ? null : tv / cv;
            return new CovariateBalance(v, tm, cm, smd, wtm, wcm, wsmd, ratio, 0, Math.Abs(wsmd) <= threshold ? DiagnosticStatus.Acceptable : DiagnosticStatus.Warning);
        }).ToArray();
    }
    public static PositivityDiagnostic Positivity(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<double> scores, WeightResult weights, DiagnosticThresholds thresholds)
    {
        var treated = scores.Where((_, i) => rows[i].BinaryTreatment).ToArray(); var control = scores.Where((_, i) => !rows[i].BinaryTreatment).ToArray(); if (treated.Length == 0 || control.Length == 0) return new(0, 0, 0, 0, 0, 0, 0, 0, rows.Count, 0, 0, 0, DiagnosticStatus.InsufficientData, ["Both treatment groups are required."]);
        var lower = Math.Max(treated.Min(), control.Min()); var upper = Math.Min(treated.Max(), control.Max()); var outside = scores.Count(x => x < lower || x > upper); var failed = lower >= upper || weights.EffectiveSampleSize < thresholds.MinimumEffectiveSampleSize || treated.Length < thresholds.MinimumTreated || control.Length < thresholds.MinimumControl; var warnings = new List<string>(weights.Warnings); if (outside > 0) warnings.Add($"{outside} rows are outside empirical common support.");
        return new(scores.Min(), scores.Max(), treated.Min(), treated.Max(), control.Min(), control.Max(), lower, upper, outside, weights.EffectiveSampleSize, weights.ExtremeWeightCount, treated.Length / (double)rows.Count, failed ? DiagnosticStatus.Failed : outside > 0 ? DiagnosticStatus.Warning : DiagnosticStatus.Acceptable, warnings);
    }
    private static double Weighted((CausalAnalysisRowContract r, int i)[] rows, IReadOnlyList<double> w, string v) => rows.Sum(x => x.r.Confounders[v] * w[x.i]) / rows.Sum(x => w[x.i]);
    private static double WeightedVariance((CausalAnalysisRowContract r, int i)[] rows, IReadOnlyList<double> w, string v, double mean) => rows.Sum(x => w[x.i] * Math.Pow(x.r.Confounders[v] - mean, 2)) / rows.Sum(x => w[x.i]);
}

public static class CausalEstimators
{
    public static CausalEstimatorResult Unadjusted(IReadOnlyList<CausalAnalysisRowContract> rows, EstimandType estimand)
    {
        RequireGroups(rows); var treated = rows.Where(x => x.BinaryTreatment).Average(x => x.OutcomeValue ? 1d : 0); var control = rows.Where(x => !x.BinaryTreatment).Average(x => x.OutcomeValue ? 1d : 0);
        return Result(CausalEstimatorKind.UnadjustedDifferenceInMeans, estimand, treated - control, rows, null, CausalEstimateStatus.DescriptiveOnly, "Descriptive risk difference; not an adjusted causal estimate.");
    }
    public static CausalEstimatorResult Weighted(IReadOnlyList<CausalAnalysisRowContract> rows, WeightResult weights, EstimandType estimand)
    {
        RequireGroups(rows); var indexed = rows.Select((r, i) => (r, i)).ToArray(); var treated = Mean(indexed.Where(x => x.r.BinaryTreatment), weights.Weights); var control = Mean(indexed.Where(x => !x.r.BinaryTreatment), weights.Weights); return Result(CausalEstimatorKind.PropensityScoreWeighting, estimand, treated - control, rows, weights.EffectiveSampleSize, CausalEstimateStatus.Estimated, "Observational propensity-weighted risk difference under stated assumptions.");
    }
    public static (CausalEstimatorResult Result, double[] M1, double[] M0) OutcomeRegression(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<string> variables, EstimandType estimand)
    {
        RequireGroups(rows); var beta = FitOutcome(rows, variables); var m1 = rows.Select(r => Predict(beta, r, variables, true)).ToArray(); var m0 = rows.Select(r => Predict(beta, r, variables, false)).ToArray(); var indices = estimand == EstimandType.AverageTreatmentEffectOnTreated ? rows.Select((r, i) => (r, i)).Where(x => x.r.BinaryTreatment).Select(x => x.i) : Enumerable.Range(0, rows.Count); var estimate = indices.Average(i => m1[i] - m0[i]); return (Result(CausalEstimatorKind.OutcomeRegression, estimand, estimate, rows, null, CausalEstimateStatus.Estimated, "Model-based potential-outcome risk difference under stated assumptions."), m1, m0);
    }
    public static CausalEstimatorResult Aipw(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<double> p, IReadOnlyList<double> m1, IReadOnlyList<double> m0, EstimandType estimand)
    {
        RequireGroups(rows); if (p.Count != rows.Count || m1.Count != rows.Count || m0.Count != rows.Count) throw new ArgumentException("Nuisance predictions must align with rows.");
        double estimate; if (estimand == EstimandType.AverageTreatmentEffect)
            estimate = rows.Select((r, i) => m1[i] - m0[i] + (r.BinaryTreatment ? 1 : 0) * ((r.OutcomeValue ? 1 : 0) - m1[i]) / p[i] - (r.BinaryTreatment ? 0 : 1) * ((r.OutcomeValue ? 1 : 0) - m0[i]) / (1 - p[i])).Average();
        else { var n1 = rows.Count(x => x.BinaryTreatment); estimate = rows.Select((r, i) => r.BinaryTreatment ? ((r.OutcomeValue ? 1 : 0) - m0[i]) / n1 : -p[i] / (1 - p[i]) * ((r.OutcomeValue ? 1 : 0) - m0[i]) / n1).Sum(); }
        return Result(CausalEstimatorKind.AugmentedInverseProbabilityWeighting, estimand, estimate, rows, null, CausalEstimateStatus.Estimated, "Doubly robust observational risk difference; robustness does not address unmeasured confounding.");
    }
    internal static double[] FitOutcome(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<string> vars)
    {
        var beta = new double[vars.Count + 2];var prevalence=rows.Count(x=>x.OutcomeValue)/(double)rows.Count;beta[0]=Math.Log(prevalence/(1-prevalence));for (var iteration = 0; iteration < 400; iteration++) { var g = new double[beta.Length]; foreach (var r in rows) { var x = new[] { 1d, r.BinaryTreatment ? 1d : 0d }.Concat(vars.Select(v => SignedLog(r.Confounders[v]))).ToArray(); var error = LogisticNuisanceModel.Sigmoid(beta.Zip(x).Sum(z => z.First * z.Second)) - (r.OutcomeValue ? 1 : 0); for (var j = 0; j < beta.Length; j++) g[j] += error * x[j]; }var maximumUpdate=0d;for (var j = 0; j < beta.Length; j++){var update=.02*g[j]/rows.Count;beta[j]-=update;maximumUpdate=Math.Max(maximumUpdate,Math.Abs(update));}if(maximumUpdate<1e-8)break;} return beta;
    }
    internal static double Predict(IReadOnlyList<double> beta, CausalAnalysisRowContract r, IReadOnlyList<string> vars, bool treatment) { var x = new[] { 1d, treatment ? 1d : 0d }.Concat(vars.Select(v => SignedLog(r.Confounders[v]))).ToArray(); return LogisticNuisanceModel.Sigmoid(beta.Zip(x).Sum(z => z.First * z.Second)); }
    private static double SignedLog(double value) => Math.Sign(value) * Math.Log(1 + Math.Abs(value));
    private static double Mean(IEnumerable<(CausalAnalysisRowContract r, int i)> rows, IReadOnlyList<double> weights) { var x = rows.ToArray(); return x.Sum(v => (v.r.OutcomeValue ? 1 : 0) * weights[v.i]) / x.Sum(v => weights[v.i]); }
    private static void RequireGroups(IReadOnlyCollection<CausalAnalysisRowContract> rows) { if (!rows.Any(x => x.BinaryTreatment) || !rows.Any(x => !x.BinaryTreatment)) throw new InvalidOperationException("Treated and control rows are required."); }
    private static CausalEstimatorResult Result(CausalEstimatorKind estimator, EstimandType estimand, double estimate, IReadOnlyCollection<CausalAnalysisRowContract> rows, double? ess, CausalEstimateStatus status, string interpretation) => new(estimator, estimand, EffectScale.RiskDifference, estimate, null, null, null, rows.Count(x => x.BinaryTreatment), rows.Count(x => !x.BinaryTreatment), ess, status, interpretation, ["Observational estimate under measured-confounder assumptions; not proof of causation.", "Partial interference and unmeasured commercial factors may remain."]);
}

public sealed class GroupedBootstrap
{
    public BootstrapResult Run(IReadOnlyList<CausalAnalysisRowContract> rows, BootstrapConfiguration configuration, Func<IReadOnlyList<CausalAnalysisRowContract>, double> estimator, CancellationToken cancellationToken = default)
    {
        if (configuration.Repetitions <= 0 || configuration.MinimumSuccessfulRepetitions > configuration.Repetitions) throw new ArgumentOutOfRangeException(nameof(configuration)); var groups = rows.GroupBy(x => x.GenericLaunchId).OrderBy(x => x.Key).Select(x => x.ToArray()).ToArray(); if (groups.Length < 2) throw new InvalidOperationException("Grouped bootstrap requires multiple launch clusters."); var values = new List<double>(); var failed = 0;
        for (var repetition = 0; repetition < configuration.Repetitions; repetition++) { cancellationToken.ThrowIfCancellationRequested(); var random = new Random(configuration.Seed + repetition); var sample = Enumerable.Range(0, groups.Length).SelectMany(_ => groups[random.Next(groups.Length)]).ToArray(); try { values.Add(estimator(sample)); } catch (InvalidOperationException) { failed++; } }
        if (values.Count < configuration.MinimumSuccessfulRepetitions) throw new InvalidOperationException("Insufficient successful grouped bootstrap repetitions."); values.Sort(); var alpha = (1 - configuration.ConfidenceLevel) / 2; var lower = values[(int)Math.Floor(alpha * (values.Count - 1))]; var upper = values[(int)Math.Ceiling((1 - alpha) * (values.Count - 1))]; var mean = values.Average(); var se = Math.Sqrt(values.Average(x => Math.Pow(x - mean, 2))); return new(se, lower, upper, values.Count, failed, "Deterministic percentile bootstrap grouped by GenericLaunchId");
    }
}
