using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.ML.Tests;

public sealed class MilestoneFiveEvaluationTests
{
    [Fact]
    public void Synthetic_reports_are_complete_prominent_and_reproducible()
    {
        var configured = Environment.GetEnvironmentVariable("PHARMAACCESS_M5_REPORT_PATH"); var root = configured ?? Path.Combine(Path.GetTempPath(), $"pharmaaccess-m5-{Guid.NewGuid():N}");
        try { var writer = new EvaluationReportWriter(); var first = writer.WriteSyntheticReports(root, new { seed = 17, dataset = "synthetic" }); var second = writer.WriteSyntheticReports(root, new { seed = 17, dataset = "synthetic" }); Assert.Equal(8, first.Count); Assert.Equal(first.Select(x => x.Sha256), second.Select(x => x.Sha256)); Assert.All(Directory.GetFiles(root, "*.md"), path => Assert.Contains(EvaluationReportWriter.SyntheticNotice, File.ReadAllText(path))); Assert.All(Directory.GetFiles(root, "*.json"), path => { using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path)); Assert.Equal(EvaluationReportWriter.SyntheticNotice, json.RootElement.GetProperty("notice").GetString()); }); }
        finally { if (configured is null && Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact]
    public void Platt_calibration_is_validation_only_deterministic_and_test_is_not_fit()
    {
        var rows = CalibrationRows(); var service = new PlattCalibrationService(); var first = service.Fit(rows, MlPartition.Validation, 10); var second = service.Fit(rows, MlPartition.Validation, 10);
        Assert.Equal(CalibrationStatus.Validated, first.Status); Assert.Equal(first.Parameters, second.Parameters); Assert.Throws<InvalidOperationException>(() => service.Fit(rows, MlPartition.Test, 10));
        var test = service.EvaluateTest(first, rows, MlPartition.Test); Assert.NotNull(test.TestEvaluation); Assert.Equal(first.Parameters, test.Parameters);
    }

    [Fact]
    public void Calibration_rejects_single_class_and_reports_bins_and_errors()
    {
        var service = new PlattCalibrationService(); var single = Enumerable.Range(0, 30).Select(i => new BinaryPrediction(true, 0, .8f, true)).ToArray(); Assert.Equal(CalibrationStatus.InsufficientData, service.Fit(single, MlPartition.Validation).Status);
        var result = service.Fit(CalibrationRows(), MlPartition.Validation, 10, 5, CalibrationBinning.EqualWidth); Assert.NotEmpty(result.ValidationEvaluation!.Bins); Assert.InRange(result.ValidationEvaluation.ExpectedCalibrationError, 0, 1); Assert.InRange(result.ValidationEvaluation.MaximumCalibrationError, 0, 1);
    }

    [Fact]
    public void Calibration_metrics_expose_improvement_and_degradation_cases()
    {
        var underconfident = Enumerable.Range(0, 40).Select(i => new BinaryPrediction(i % 2 == 0, 0, i % 2 == 0 ? .55f : .45f, i % 2 == 0)).ToArray();
        var fitted = new PlattCalibrationService().Fit(underconfident, MlPartition.Validation, 10); var rawBrier = underconfident.Average(x => Math.Pow(x.Probability - (x.Label ? 1 : 0), 2)); Assert.True(fitted.ValidationEvaluation!.BrierScore < rawBrier);
        var wellSeparated = CalibrationRows(); var degraded = PlattCalibrationService.Evaluate(wellSeparated, new CalibrationParameters(0, 0), 5, CalibrationBinning.EqualWidth); var separatedRawBrier = wellSeparated.Average(x => Math.Pow(x.Probability - (x.Label ? 1 : 0), 2)); Assert.True(degraded.BrierScore > separatedRawBrier);
    }

    [Fact]
    public void Uncertainty_combines_support_indicators_without_claiming_confidence_interval()
    {
        var service = new UncertaintyIndicatorService(); Assert.Equal(UncertaintyStatus.Moderate, service.Assess(.51, .5, 100, 0, 0, 0, 0, null, true).Status); Assert.Equal(UncertaintyStatus.High, service.Assess(.51, .5, 2, 10, 1, 1, 5, .5, true).Status); Assert.Equal(UncertaintyStatus.Unsupported, service.Assess(.8, .5, 20, 0, 0, 0, 0, null, false).Status);
    }

    [Fact]
    public void Threshold_policies_use_validation_and_reject_impossible_constraints()
    {
        var rows = CalibrationRows(); var service = new ThresholdPolicyService(); Assert.Equal(.5, service.Select(rows, new(ThresholdPolicyKind.FixedThreshold), MlPartition.Validation, .5).Threshold); Assert.InRange(service.Select(rows, new(ThresholdPolicyKind.MaximumF1), MlPartition.Validation, .5).Threshold, 0, 1); Assert.InRange(service.Select(rows, new(ThresholdPolicyKind.YoudenIndex), MlPartition.Validation, .5).Threshold, 0, 1); Assert.Throws<InvalidOperationException>(() => service.Select(rows, new(ThresholdPolicyKind.MaximumF1), MlPartition.Test, .5)); Assert.Throws<InvalidOperationException>(() => service.Select(rows, new(ThresholdPolicyKind.MinimumPrecision, MinimumPrecision: 1.1), MlPartition.Validation, .5));
    }

    [Fact]
    public void Permutation_importance_is_deterministic_validation_only_and_ordered()
    {
        var rows = Enumerable.Range(0, 20).Select(i => EvalRow(i, i % 2 == 0, i % 2 == 0 ? .9 : .1)).ToArray(); var service = new PermutationFeatureImportanceService(); double Metric(IReadOnlyList<IReadOnlyDictionary<string, float>> x) => x.Average(y => y["signal"]); var first = service.Evaluate(1, rows, MlPartition.Validation, ["signal", "noise"], Metric, 3, 17); var second = service.Evaluate(1, rows, MlPartition.Validation, ["signal", "noise"], Metric, 3, 17); Assert.Equal(first.Select(x => x.ImportanceDelta), second.Select(x => x.ImportanceDelta)); Assert.Equal([1, 2], first.Select(x => x.Rank)); Assert.Throws<InvalidOperationException>(() => service.Evaluate(1, rows, MlPartition.Test, ["signal"], Metric, 2, 1)); Assert.Contains(first.SelectMany(x => x.Warnings), x => x.Contains("not causal"));
    }

    [Fact]
    public void Subgroups_suppress_small_or_one_class_groups_and_error_analysis_is_structured()
    {
        var rows = Enumerable.Range(0, 12).Select(i => EvalRow(i, i % 3 == 0, i % 3 == 0 ? .9 : .1) with { Region = i < 10 ? "Large" : "Tiny" }).ToArray(); var subgroups = new SubgroupEvaluationService().Evaluate(rows, 5, .5, .33); Assert.Contains(subgroups, x => x.Dimension == "Region" && x.Value == "Tiny" && x.Status == EvaluationStatus.InsufficientData && x.Metrics is null);
        var errors = new ErrorAnalysisService().Analyze([EvalRow(1, false, .95), EvalRow(2, true, .05)], .5, new UncertaintyIndicatorService()); Assert.Contains(errors, x => x.Category == ErrorCategory.HighConfidenceFalsePositive); Assert.Contains(errors, x => x.Category == ErrorCategory.HighConfidenceFalseNegative);
    }

    private static BinaryPrediction[] CalibrationRows() => Enumerable.Range(0, 40).Select(i => { var label = i % 2 == 0; var p = label ? .65f + (i % 5) * .03f : .35f - (i % 5) * .03f; return new BinaryPrediction(label, 0, p, p >= .5); }).ToArray();
    private static ScoredEvaluationRow EvalRow(int id, bool label, double probability) => new(id, 1, 1, id % 4 + 1, "Region", 2020 + id % 3, 20201 + id, id % 5, label, probability, null, id % 6, 0, 0, 0, new Dictionary<string, float> { ["signal"] = label ? 1 : 0, ["noise"] = id }, []);
}
