using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.ML.Tests;

public sealed class DriftGovernanceTests
{
    [Fact]
    public void Identical_samples_have_no_drift()
    {
        var values = Enumerable.Range(0, 100).Select(x => x / 100d).ToArray();
        var report = Detector().Detect(Request(values, values));
        Assert.Equal(DriftSeverity.None, report.Severity);
        Assert.All(report.Measures, x => Assert.Equal(DriftSeverity.None, x.Severity));
    }

    [Fact]
    public void Moderate_feature_shift_is_warning_level()
    {
        var detector = new DriftDetector(new DriftThresholds(PsiInformational: .01, PsiWarning: .02, PsiBlocking: 100, KsInformational: .01, KsWarning: .10, KsBlocking: 100));
        var reference = Enumerable.Range(0, 100).Select(x => x / 100d).ToArray(); var current = reference.Select(x => Math.Min(1, x + .12)).ToArray();
        Assert.Equal(DriftSeverity.Warning, detector.Detect(Request(reference, current)).Severity);
    }

    [Fact]
    public void Large_feature_shift_is_blocking()
    {
        var report = Detector().Detect(Request(Enumerable.Repeat(.1, 100).ToArray(), Enumerable.Repeat(.9, 100).ToArray()));
        Assert.Equal(DriftSeverity.Blocking, report.Severity);
    }

    [Fact]
    public void Prediction_distribution_drift_is_reported_separately()
    {
        var stable = Enumerable.Range(0, 100).Select(x => x / 100d).ToArray();
        var request = Request(stable, stable) with { ReferencePredictions = Enumerable.Repeat(.1, 100).ToArray(), CurrentPredictions = Enumerable.Repeat(.8, 100).ToArray() };
        var report = Detector().Detect(request);
        Assert.Contains(report.Measures, x => x.Scope == "Prediction" && x.Severity == DriftSeverity.Blocking);
    }

    [Fact]
    public void Categorical_divergence_is_detected()
    {
        var stable = Enumerable.Range(0, 100).Select(x => x / 100d).ToArray(); var request = Request(stable, stable) with
        { CategoricalFeatures = [new("Region", Enumerable.Repeat("A", 90).Concat(Enumerable.Repeat("B", 10)).ToArray(), Enumerable.Repeat("A", 10).Concat(Enumerable.Repeat("B", 90)).ToArray())] };
        Assert.Contains(Detector().Detect(request).Measures, x => x.Statistic == "JensenShannon" && x.Severity == DriftSeverity.Blocking);
    }

    [Fact]
    public void Performance_degradation_reports_auc_brier_and_calibration()
    {
        var stable = Enumerable.Range(0, 40).Select(x => x / 40d).ToArray(); var reference = Labeled(good: true); var current = Labeled(good: false);
        var report = Detector().Detect(Request(stable, stable) with { ReferenceLabeled = reference, CurrentLabeled = current });
        Assert.Contains(report.Measures, x => x.Statistic == "PR_AUC" && x.Severity >= DriftSeverity.Warning);
        Assert.Contains(report.Measures, x => x.Statistic == "ROC_AUC"); Assert.Contains(report.Measures, x => x.Statistic == "Brier"); Assert.Contains(report.Measures, x => x.Statistic == "CalibrationError");
    }

    [Fact]
    public void Important_subgroup_degradation_is_warned()
    {
        var stable = Enumerable.Range(0, 40).Select(x => x / 40d).ToArray(); var reference = Labeled(true); var current = Labeled(true).Select((x, i) => x.Subgroup == "North" ? x with { Probability = i % 2 == 0 ? .1 : .9 } : x).ToArray();
        var report = Detector().Detect(Request(stable, stable) with { ReferenceLabeled = reference, CurrentLabeled = current });
        Assert.Contains(report.SubgroupWarnings, x => x.Contains("North"));
    }

    [Fact]
    public void Comparison_blocks_incompatible_feature_schemas()
    {
        var result = Comparer().Compare(Model("champion"), Model("challenger") with { FeatureSchemaHash = "other" });
        Assert.False(result.PromotionEligible); Assert.Contains(result.BlockingReasons, x => x.Contains("schemas"));
    }

    [Fact]
    public void Comparison_blocks_invalid_artifact_hashes()
    {
        var result = Comparer().Compare(Model("champion"), Model("challenger") with { ArtifactHashValid = false });
        Assert.False(result.PromotionEligible); Assert.Contains(result.BlockingReasons, x => x.Contains("hashes"));
    }

    [Fact]
    public void Comparison_blocks_cohort_leakage_calibration_subgroup_and_missing_metrics()
    {
        var challenger = Model("challenger") with { EvaluationCohortHash = "different", LeakageChecksPassed = false,
            Metrics = new Dictionary<string, double> { ["ROC_AUC"] = .83, ["PR_AUC"] = .12, ["CalibrationError"] = .2 },
            ImportantSubgroupPrAuc = new Dictionary<string, double> { ["North"] = .01 } };
        var result = Comparer().Compare(Model("champion"), challenger);
        Assert.False(result.PromotionEligible); Assert.Contains(result.BlockingReasons, x => x.Contains("cohorts")); Assert.Contains(result.BlockingReasons, x => x.Contains("Leakage")); Assert.Contains(result.BlockingReasons, x => x.Contains("Calibration")); Assert.Contains(result.BlockingReasons, x => x.Contains("subgroup")); Assert.Contains(result.BlockingReasons, x => x.Contains("missing"));
    }

    [Fact]
    public async Task Rejected_promotion_is_audited_without_changing_champion()
    {
        var manager = new HumanGovernedModelManager("champion"); var comparison = Comparer().Compare(Model("champion"), Model("challenger")); await manager.RegisterComparisonAsync(comparison);
        var audit = await manager.RejectAsync(Action(comparison.Id, "evidence insufficient"));
        Assert.Equal(PromotionDecision.Rejected, audit.Decision); Assert.Equal("champion", manager.State.ChampionVersion); Assert.Equal("reviewer-1", audit.ApproverIdentifier);
    }

    [Fact]
    public async Task Approved_promotion_requires_human_metadata_and_complete_audit()
    {
        var manager = new HumanGovernedModelManager("champion"); var comparison = Comparer().Compare(Model("champion"), Model("challenger")); await manager.RegisterComparisonAsync(comparison);
        await Assert.ThrowsAsync<ArgumentException>(() => manager.ApproveAsync(Action(comparison.Id, "") with { ApproverIdentifier = "" }));
        var audit = await manager.ApproveAsync(Action(comparison.Id, "validated on frozen cohort"));
        Assert.Equal("challenger", manager.State.ChampionVersion); Assert.Equal("champion", audit.ChampionBefore); Assert.Equal("challenger", audit.ChampionAfter); Assert.NotEqual(default, audit.RecordedAtUtc);
    }

    [Fact]
    public async Task Rollback_restores_previously_approved_champion()
    {
        var manager = new HumanGovernedModelManager("champion"); var comparison = Comparer().Compare(Model("champion"), Model("challenger")); await manager.RegisterComparisonAsync(comparison); await manager.ApproveAsync(Action(comparison.Id, "approve"));
        var audit = await manager.RollbackAsync("reviewer-2", DateTime.UtcNow, "post-promotion incident");
        Assert.Equal(PromotionDecision.RolledBack, audit.Decision); Assert.Equal("champion", manager.State.ChampionVersion);
    }

    [Fact]
    public async Task Comparison_registration_never_automatically_promotes()
    {
        var manager = new HumanGovernedModelManager("champion"); var comparison = Comparer().Compare(Model("champion"), Model("challenger")); await manager.RegisterComparisonAsync(comparison);
        Assert.Equal("champion", manager.State.ChampionVersion); Assert.Equal("Awaiting human approval", manager.State.PromotionStatus); Assert.Empty(manager.State.AuditRecords);
    }

    private static DriftDetector Detector() => new(new DriftThresholds());
    private static ChampionChallengerComparer Comparer() => new(new DriftThresholds());
    private static DriftReportRequest Request(double[] reference, double[] current) => new("fasttree-published", "2026-Q3", [new("Volume", reference, current)], [], reference, current, null, null);
    private static LabeledPrediction[] Labeled(bool good) => Enumerable.Range(0, 40).Select(i => { var label = i % 2 == 0; return new LabeledPrediction(label, good ? label ? .9 : .1 : label ? .4 : .6, i < 20 ? "North" : "South"); }).ToArray();
    private static GovernedModelSnapshot Model(string version) => new(version, new string('a', 64), true, "schema-v1", "cohort-v1", "dataset-v1", "repro-v1", true,
        new Dictionary<string, double> { ["ROC_AUC"] = .82, ["PR_AUC"] = .11, ["Brier"] = .08, ["CalibrationError"] = .02 }, new Dictionary<string, double> { ["North"] = .10 });
    private static PromotionActionRequest Action(Guid id, string reason) => new(id, "reviewer-1", DateTime.UtcNow, reason);
}
