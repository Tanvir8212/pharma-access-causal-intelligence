using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed class PlattCalibrationService
{
    public CalibrationResult Fit(IReadOnlyList<BinaryPrediction> validation, MlPartition partition, int minimumRows = 20, int bins = 10, CalibrationBinning binning = CalibrationBinning.EqualFrequency)
    {
        if (partition != MlPartition.Validation) throw new InvalidOperationException("Calibration may be fitted only on validation data.");
        if (validation.Count < minimumRows || validation.Select(x => x.Label).Distinct().Count() < 2) return new(CalibrationMethod.Platt, CalibrationStatus.InsufficientData, null, null, null, ["Validation data are too small or contain only one class."]);
        double slope = 1, intercept = 0;
        for (var iteration = 0; iteration < 100; iteration++)
        {
            double g0 = 0, g1 = 0, h00 = 1e-9, h01 = 0, h11 = 1e-9;
            foreach (var row in validation) { var x = Logit(row.Probability); var p = Sigmoid(slope * x + intercept); var error = p - (row.Label ? 1 : 0); var weight = p * (1 - p); g0 += error * x; g1 += error; h00 += weight * x * x; h01 += weight * x; h11 += weight; }
            var determinant = h00 * h11 - h01 * h01; if (Math.Abs(determinant) < 1e-12) break;
            var deltaSlope = (h11 * g0 - h01 * g1) / determinant; var deltaIntercept = (-h01 * g0 + h00 * g1) / determinant; slope -= deltaSlope; intercept -= deltaIntercept; if (Math.Abs(deltaSlope) + Math.Abs(deltaIntercept) < 1e-9) break;
        }
        var parameters = new CalibrationParameters(slope, intercept); var evaluation = Evaluate(validation, parameters, bins, binning);
        return new(CalibrationMethod.Platt, CalibrationStatus.Validated, parameters, evaluation, null, evaluation.Warnings);
    }

    public CalibrationResult EvaluateTest(CalibrationResult fitted, IReadOnlyList<BinaryPrediction> test, MlPartition partition, int bins = 10, CalibrationBinning binning = CalibrationBinning.EqualFrequency)
    {
        if (partition != MlPartition.Test || fitted.Status != CalibrationStatus.Validated || fitted.Parameters is null) throw new InvalidOperationException("A frozen validated calibrator is required for test evaluation.");
        return fitted with { TestEvaluation = Evaluate(test, fitted.Parameters, bins, binning) };
    }
    public static double Calibrate(double raw, CalibrationParameters p) => Sigmoid(p.Slope * Logit(raw) + p.Intercept);
    public static CalibrationEvaluation Evaluate(IReadOnlyList<BinaryPrediction> rows, CalibrationParameters p, int binCount, CalibrationBinning binning)
    {
        if (binCount < 2) throw new ArgumentOutOfRangeException(nameof(binCount)); var values = rows.Select(x => (x.Label, Probability: Calibrate(x.Probability, p))).ToArray();
        var brier = values.Average(x => Math.Pow(x.Probability - (x.Label ? 1 : 0), 2)); var logLoss = -values.Average(x => x.Label ? Math.Log(Math.Clamp(x.Probability, 1e-15, 1 - 1e-15)) : Math.Log(Math.Clamp(1 - x.Probability, 1e-15, 1 - 1e-15)));
        var groups = binning == CalibrationBinning.EqualWidth ? values.GroupBy(x => Math.Min(binCount - 1, (int)(x.Probability * binCount))).OrderBy(x => x.Key).Select(x => x.ToArray()).ToArray() : values.OrderBy(x => x.Probability).Select((x, i) => (x, i)).GroupBy(x => Math.Min(binCount - 1, x.i * binCount / values.Length)).Select(x => x.Select(y => y.x).ToArray()).ToArray();
        var bins = groups.Select((group, index) => new CalibrationBin(index, group.Min(x => x.Probability), group.Max(x => x.Probability), group.Length, group.Average(x => x.Probability), group.Average(x => x.Label ? 1d : 0d))).ToArray();
        var ece = bins.Sum(x => x.RowCount / (double)values.Length * Math.Abs(x.MeanPredictedProbability - x.ObservedRate)); var mce = bins.Max(x => Math.Abs(x.MeanPredictedProbability - x.ObservedRate)); var warnings = bins.Any(x => x.RowCount < 5) ? new[] { "One or more calibration bins have fewer than five rows." } : [];
        return new(brier, logLoss, ece, mce, p.Slope, p.Intercept, bins, warnings);
    }
    private static double Logit(double p) { p = Math.Clamp(p, 1e-6, 1 - 1e-6); return Math.Log(p / (1 - p)); }
    private static double Sigmoid(double x) => 1 / (1 + Math.Exp(-Math.Clamp(x, -40, 40)));
}

public sealed class UncertaintyIndicatorService
{
    public UncertaintyAssessment Assess(double probability, double threshold, int calibrationBinSize, int missing, int outOfRange, int unseen, double distributionDistance, double? modelDisagreement, bool schemaSupported)
    {
        var reasons = new List<string>(); if (!schemaSupported) return new(UncertaintyStatus.Unsupported, Math.Abs(probability - threshold), calibrationBinSize, ["Input feature schema is unsupported."]);
        var score = 0; var distance = Math.Abs(probability - threshold); if (distance < .1) { score += 2; reasons.Add("Probability is near the decision threshold."); } if (calibrationBinSize < 20) { score++; reasons.Add("Calibration bin has limited support."); } if (missing >= 5) { score += 2; reasons.Add("High missing-feature count."); } if (outOfRange > 0) { score += 2; reasons.Add("One or more inputs are outside training ranges."); } if (unseen > 0) { score += 2; reasons.Add("Unseen categories are present."); } if (distributionDistance > 3) { score += 2; reasons.Add("Input is distant from the training distribution."); } if (modelDisagreement > .2) { score += 2; reasons.Add("Candidate models disagree materially."); }
        return new(score >= 5 ? UncertaintyStatus.High : score >= 2 ? UncertaintyStatus.Moderate : UncertaintyStatus.Low, distance, calibrationBinSize, reasons);
    }
}

public sealed class ThresholdPolicyService
{
    public ThresholdEvaluation Select(IReadOnlyList<BinaryPrediction> validation, ThresholdPolicy policy, MlPartition partition, double trainingPrevalence)
    {
        if (partition != MlPartition.Validation) throw new InvalidOperationException("Thresholds may be selected only on validation data.");
        var candidates = policy.Kind == ThresholdPolicyKind.FixedThreshold ? new[] { policy.FixedThreshold } : validation.Select(x => (double)x.Probability).Append(0).Append(1).Distinct().Order().ToArray();
        var evaluated = candidates.Select(t => (Threshold: t, Metrics: BinaryMetricCalculator.Calculate(validation, t, trainingPrevalence))).ToArray();
        var selected = policy.Kind switch { ThresholdPolicyKind.FixedThreshold => evaluated.Single(), ThresholdPolicyKind.MaximumF1 => evaluated.OrderByDescending(x => x.Metrics.F1).ThenBy(x => Math.Abs(x.Threshold - .5)).First(), ThresholdPolicyKind.MinimumRecall => evaluated.Where(x => x.Metrics.Recall >= (policy.MinimumRecall ?? throw new ArgumentException("Recall constraint is required."))).OrderByDescending(x => x.Metrics.Precision).FirstOrDefault(), ThresholdPolicyKind.MinimumPrecision => evaluated.Where(x => x.Metrics.Precision >= (policy.MinimumPrecision ?? throw new ArgumentException("Precision constraint is required."))).OrderByDescending(x => x.Metrics.Recall).FirstOrDefault(), ThresholdPolicyKind.YoudenIndex => evaluated.OrderByDescending(x => x.Metrics.Recall + x.Metrics.Specificity - 1).First(), ThresholdPolicyKind.CostSensitive => evaluated.OrderBy(x => x.Metrics.FalsePositive * policy.FalsePositiveCost + x.Metrics.FalseNegative * policy.FalseNegativeCost).First(), _ => throw new NotSupportedException() };
        if (selected.Metrics is null) throw new InvalidOperationException("Threshold constraint is impossible on validation data.");
        return new(policy.Kind, selected.Threshold, selected.Metrics, policy.Kind.ToString(), []);
    }
}

public sealed class PermutationFeatureImportanceService
{
    public IReadOnlyList<FeatureImportanceResult> Evaluate(long runId, IReadOnlyList<ScoredEvaluationRow> validation, MlPartition partition, IReadOnlyList<string> featureOrder, Func<IReadOnlyList<IReadOnlyDictionary<string, float>>, double> metric, int repetitions, int seed)
    {
        if (partition != MlPartition.Validation) throw new InvalidOperationException("Permutation importance uses validation data only."); if (repetitions <= 0) throw new ArgumentOutOfRangeException(nameof(repetitions)); var baseline = metric(validation.Select(x => x.Features).ToArray()); var results = new List<(string Feature, double Mean, double Sd)>();
        foreach (var feature in featureOrder) { var values = new List<double>(); for (var repeat = 0; repeat < repetitions; repeat++) { var random = new Random(seed + repeat); var shuffled = validation.Select(x => x.Features.GetValueOrDefault(feature, float.NaN)).OrderBy(_ => random.Next()).ToArray(); var rows = validation.Select((x, i) => (IReadOnlyDictionary<string, float>)new Dictionary<string, float>(x.Features) { [feature] = shuffled[i] }).ToArray(); values.Add(metric(rows)); } var mean = values.Average(); var sd = Math.Sqrt(values.Average(x => Math.Pow(x - mean, 2))); results.Add((feature, mean, sd)); }
        return results.OrderBy(x => x.Mean).Select((x, i) => new FeatureImportanceResult(runId, x.Feature, "PR_AUC", baseline, x.Mean, baseline - x.Mean, x.Sd, repetitions, i + 1, DateTime.UtcNow, ["Correlated features can make individual permutation importance unstable; importance is not causal."])).ToArray();
    }
}

public sealed class SubgroupEvaluationService
{
    public IReadOnlyList<SubgroupEvaluation> Evaluate(IReadOnlyList<ScoredEvaluationRow> rows, int minimumSize, double threshold, double trainingPrevalence)
    {
        if (minimumSize < 2) throw new ArgumentOutOfRangeException(nameof(minimumSize)); var dimensions = new (string Name, Func<ScoredEvaluationRow, string> Value)[] { ("State", x => x.StateId.ToString()), ("Region", x => x.Region), ("LaunchCohort", x => x.LaunchCohort.ToString()), ("ObservationQuarter", x => x.ObservationQuarterId.ToString()), ("QuarterSinceApproval", x => x.QuarterSinceApproval.ToString()), ("MissingnessBand", x => x.MissingFeatureCount >= 5 ? "High" : x.MissingFeatureCount > 0 ? "Some" : "None"), ("StateHistoricalVolumeBand", x => x.Features.GetValueOrDefault("StateHistoricalGenericVolume") switch { < 100 => "Low", < 1000 => "Medium", _ => "High" }), ("ProbabilityBand", x => $"{Math.Floor((x.CalibratedProbability ?? x.RawProbability) * 5) / 5:0.0}") };
        return dimensions.SelectMany(d => rows.GroupBy(d.Value).OrderBy(x => x.Key).Select(group => { var values = group.ToArray(); var positives = values.Count(x => x.Label); var insufficient = values.Length < minimumSize || positives == 0 || positives == values.Length; var predictions = values.Select(x => { var p = (float)(x.CalibratedProbability ?? x.RawProbability); return new BinaryPrediction(x.Label, 0, p, p >= threshold); }).ToArray(); return new SubgroupEvaluation(d.Name, group.Key, insufficient ? EvaluationStatus.InsufficientData : EvaluationStatus.Complete, values.Length, positives, values.Length - positives, insufficient ? null : BinaryMetricCalculator.Calculate(predictions, threshold, trainingPrevalence), insufficient ? null : Math.Abs(predictions.Average(x => x.Probability) - predictions.Average(x => x.Label ? 1f : 0f)), insufficient ? ["Metrics suppressed because sample size or class support is insufficient."] : []); })).ToArray();
    }
}

public sealed class ErrorAnalysisService
{
    public IReadOnlyList<ModelErrorRecord> Analyze(IReadOnlyList<ScoredEvaluationRow> rows, double threshold, UncertaintyIndicatorService uncertainty)
    {
        var results = new List<ModelErrorRecord>(); foreach (var row in rows) { var probability = row.CalibratedProbability ?? row.RawProbability; var predicted = probability >= threshold; var assessment = uncertainty.Assess(probability, threshold, 20, row.MissingFeatureCount, row.OutOfRangeFeatureCount, row.UnseenCategoryCount, row.TrainingDistributionDistance, null, true); ErrorCategory? category = predicted && !row.Label ? probability >= .8 ? ErrorCategory.HighConfidenceFalsePositive : ErrorCategory.FalsePositive : !predicted && row.Label ? probability <= .2 ? ErrorCategory.HighConfidenceFalseNegative : ErrorCategory.FalseNegative : assessment.Status == UncertaintyStatus.Unsupported ? ErrorCategory.UnsupportedPrediction : row.MissingFeatureCount >= 5 ? ErrorCategory.HighMissingness : row.TrainingDistributionDistance > 3 ? ErrorCategory.OutOfDistribution : null; if (category.HasValue) results.Add(new(row.ModelTrainingRunId, row.DrugId, row.StateId, row.ObservationQuarterId, row.Label, probability, predicted, threshold, category.Value, row.MissingFeatureCount, assessment.Status, row.Region, row.LaunchCohort, row.QuarterSinceApproval, row.Features.Take(5).ToDictionary(), row.Warnings.Concat(assessment.Reasons).ToArray())); } return results;
    }
}
