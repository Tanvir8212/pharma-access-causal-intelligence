using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed class DriftDetector(DriftThresholds thresholds) : IDriftDetector
{
    public DriftReport Detect(DriftReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ChampionVersion) || string.IsNullOrWhiteSpace(request.EvaluationWindow)) throw new ArgumentException("Model version and evaluation window are required.");
        var measures = new List<DriftMeasure>();
        foreach (var feature in request.NumericFeatures)
        {
            RequireSamples(feature.Reference, feature.Current, feature.Name);
            var psi = Psi(feature.Reference, feature.Current); var ks = Ks(feature.Reference, feature.Current);
            measures.Add(new("Feature", feature.Name, "PSI", 0, psi, psi, Severity(psi, thresholds.PsiInformational, thresholds.PsiWarning, thresholds.PsiBlocking), "sum((current%-reference%)*ln(current%/reference%))"));
            measures.Add(new("Feature", feature.Name, "KS", 0, ks, ks, Severity(ks, thresholds.KsInformational, thresholds.KsWarning, thresholds.KsBlocking), "max_x |F_current(x)-F_reference(x)|"));
        }
        foreach (var feature in request.CategoricalFeatures)
        {
            if (feature.Reference.Length == 0 || feature.Current.Length == 0) throw new ArgumentException($"Feature '{feature.Name}' requires reference and current samples.");
            var js = JensenShannon(feature.Reference, feature.Current);
            measures.Add(new("Feature", feature.Name, "JensenShannon", 0, js, js, Severity(js, thresholds.CategoricalInformational, thresholds.CategoricalWarning, thresholds.CategoricalBlocking), "0.5*KL(reference||midpoint)+0.5*KL(current||midpoint)"));
        }
        RequireSamples(request.ReferencePredictions, request.CurrentPredictions, "Prediction");
        var predictionPsi = Psi(request.ReferencePredictions, request.CurrentPredictions); var predictionKs = Ks(request.ReferencePredictions, request.CurrentPredictions);
        measures.Add(new("Prediction", "Probability", "PSI", 0, predictionPsi, predictionPsi, Severity(predictionPsi, thresholds.PsiInformational, thresholds.PsiWarning, thresholds.PsiBlocking), "sum((current%-reference%)*ln(current%/reference%))"));
        measures.Add(new("Prediction", "Probability", "KS", 0, predictionKs, predictionKs, Severity(predictionKs, thresholds.KsInformational, thresholds.KsWarning, thresholds.KsBlocking), "max_x |F_current(x)-F_reference(x)|"));
        var subgroupWarnings = new List<string>();
        if (request.ReferenceLabeled is { Length: > 1 } reference && request.CurrentLabeled is { Length: > 1 } current)
        {
            AddPerformance("Overall", "All", reference, current, measures);
            foreach (var subgroup in reference.Select(x => x.Subgroup).Intersect(current.Select(x => x.Subgroup), StringComparer.Ordinal).Order())
            {
                var r = reference.Where(x => x.Subgroup == subgroup).ToArray(); var c = current.Where(x => x.Subgroup == subgroup).ToArray();
                if (!HasBothClasses(r) || !HasBothClasses(c)) continue;
                var before = Metrics(r); var after = Metrics(c); var drop = before.PrAuc - after.PrAuc;
                var severity = Severity(drop, thresholds.MetricInformational, thresholds.MetricWarning, thresholds.MetricBlocking);
                measures.Add(new("Subgroup", subgroup, "PR_AUC", before.PrAuc, after.PrAuc, after.PrAuc - before.PrAuc, severity, "PR_AUC_current-PR_AUC_reference"));
                if (severity >= DriftSeverity.Warning) subgroupWarnings.Add($"Important subgroup '{subgroup}' PR AUC worsened by {drop:0.####}.");
            }
        }
        var overall = measures.Count == 0 ? DriftSeverity.None : measures.Max(x => x.Severity);
        return new(Guid.NewGuid(), request.ChampionVersion, request.EvaluationWindow, DateTime.UtcNow, overall, measures.ToArray(), subgroupWarnings.ToArray(), request.ReferenceLabeled is not null && request.CurrentLabeled is not null, "Monitoring is advisory only; retraining, promotion, and deployment require separate human approval.");
    }

    private void AddPerformance(string scope, string name, LabeledPrediction[] reference, LabeledPrediction[] current, List<DriftMeasure> output)
    {
        if (!HasBothClasses(reference) || !HasBothClasses(current)) return;
        var r = Metrics(reference); var c = Metrics(current);
        Add("ROC_AUC", r.RocAuc, c.RocAuc, r.RocAuc - c.RocAuc, false);
        Add("PR_AUC", r.PrAuc, c.PrAuc, r.PrAuc - c.PrAuc, false);
        Add("Brier", r.Brier, c.Brier, c.Brier - r.Brier, true);
        Add("CalibrationError", r.Calibration, c.Calibration, c.Calibration - r.Calibration, true);
        void Add(string metric, double before, double after, double degradation, bool calibration)
        {
            var severity = calibration ? Severity(degradation, thresholds.CalibrationInformational, thresholds.CalibrationWarning, thresholds.CalibrationBlocking) : Severity(degradation, thresholds.MetricInformational, thresholds.MetricWarning, thresholds.MetricBlocking);
            output.Add(new(scope, name, metric, before, after, after - before, severity, metric is "Brier" or "CalibrationError" ? "current-reference (lower is better)" : "current-reference (higher is better)"));
        }
    }

    public static double Psi(IReadOnlyList<double> reference, IReadOnlyList<double> current, int bins = 10)
    {
        RequireSamples(reference, current, "PSI"); var min = Math.Min(reference.Min(), current.Min()); var max = Math.Max(reference.Max(), current.Max()); if (max == min) return 0;
        var rb = new int[bins]; var cb = new int[bins];
        foreach (var x in reference) rb[Math.Min(bins - 1, (int)((x - min) / (max - min) * bins))]++;
        foreach (var x in current) cb[Math.Min(bins - 1, (int)((x - min) / (max - min) * bins))]++;
        return Enumerable.Range(0, bins).Sum(i => { var p = Math.Max(rb[i] / (double)reference.Count, 1e-6); var q = Math.Max(cb[i] / (double)current.Count, 1e-6); return (q - p) * Math.Log(q / p); });
    }
    public static double Ks(IReadOnlyList<double> reference, IReadOnlyList<double> current)
    {
        RequireSamples(reference, current, "KS"); return reference.Concat(current).Distinct().Order().Max(x => Math.Abs(reference.Count(v => v <= x) / (double)reference.Count - current.Count(v => v <= x) / (double)current.Count));
    }
    public static double JensenShannon(IReadOnlyList<string> reference, IReadOnlyList<string> current)
    {
        var keys = reference.Concat(current).Distinct(StringComparer.Ordinal).ToArray();
        return keys.Sum(k => { var p = reference.Count(x => x == k) / (double)reference.Count; var q = current.Count(x => x == k) / (double)current.Count; var m = (p + q) / 2; return .5 * (p == 0 ? 0 : p * Math.Log(p / m)) + .5 * (q == 0 ? 0 : q * Math.Log(q / m)); });
    }
    private static (double RocAuc, double PrAuc, double Brier, double Calibration) Metrics(LabeledPrediction[] rows)
    {
        var ordered = rows.OrderByDescending(x => x.Probability).ToArray(); var positives = rows.Count(x => x.Label); var negatives = rows.Length - positives;
        double tp = 0, fp = 0, previousRecall = 0, previousPrecision = 1, pr = 0, roc = 0, previousFpr = 0;
        foreach (var row in ordered) { if (row.Label) tp++; else fp++; var recall = tp / positives; var precision = tp / (tp + fp); var fpr = fp / negatives; pr += (recall - previousRecall) * (precision + previousPrecision) / 2; roc += (fpr - previousFpr) * (recall + previousRecall) / 2; previousRecall = recall; previousPrecision = precision; previousFpr = fpr; }
        var brier = rows.Average(x => Math.Pow(x.Probability - (x.Label ? 1 : 0), 2)); var calibration = Math.Abs(rows.Average(x => x.Probability) - rows.Average(x => x.Label ? 1d : 0d));
        return (roc, pr, brier, calibration);
    }
    private static bool HasBothClasses(IEnumerable<LabeledPrediction> rows) => rows.Select(x => x.Label).Distinct().Count() == 2;
    private static DriftSeverity Severity(double value, double info, double warning, double blocking) => value >= blocking ? DriftSeverity.Blocking : value >= warning ? DriftSeverity.Warning : value >= info ? DriftSeverity.Informational : DriftSeverity.None;
    private static void RequireSamples(IReadOnlyCollection<double> reference, IReadOnlyCollection<double> current, string name) { if (reference.Count == 0 || current.Count == 0 || reference.Any(x => !double.IsFinite(x)) || current.Any(x => !double.IsFinite(x))) throw new ArgumentException($"'{name}' requires finite reference and current samples."); }
}

public sealed class InMemoryDriftReportStore : IDriftReportStore
{
    private readonly Dictionary<Guid, DriftReport> _reports = [];
    private readonly object _gate = new();
    public Task SaveAsync(DriftReport report, CancellationToken cancellationToken = default) { lock (_gate) _reports[report.Id] = report; return Task.CompletedTask; }
    public Task<IReadOnlyList<DriftReport>> ListAsync(CancellationToken cancellationToken = default) { lock (_gate) return Task.FromResult<IReadOnlyList<DriftReport>>(_reports.Values.OrderByDescending(x => x.CreatedAtUtc).ToArray()); }
    public Task<DriftReport?> GetAsync(Guid id, CancellationToken cancellationToken = default) { lock (_gate) return Task.FromResult(_reports.GetValueOrDefault(id)); }
}

public sealed class ChampionChallengerComparer(DriftThresholds thresholds) : IChampionChallengerComparer
{
    private static readonly string[] RequiredMetrics = ["ROC_AUC", "PR_AUC", "Brier", "CalibrationError"];
    public ChampionChallengerComparison Compare(GovernedModelSnapshot champion, GovernedModelSnapshot challenger)
    {
        var blocks = new List<string>(); var warnings = new List<string>();
        if (champion.FeatureSchemaHash != challenger.FeatureSchemaHash) blocks.Add("Feature schemas differ.");
        if (!champion.ArtifactHashValid || !challenger.ArtifactHashValid || !IsSha(champion.ArtifactSha256) || !IsSha(challenger.ArtifactSha256)) blocks.Add("Artifact hashes are invalid.");
        if (champion.EvaluationCohortHash != challenger.EvaluationCohortHash) blocks.Add("Frozen evaluation cohorts differ.");
        if (champion.DatasetHash != challenger.DatasetHash || champion.DatasetFreezeIdentifier != challenger.DatasetFreezeIdentifier || champion.ReproducibilityHash != challenger.ReproducibilityHash) blocks.Add("Dataset, freeze, or reproducibility metadata differ.");
        if (!champion.LeakageChecksPassed || !challenger.LeakageChecksPassed) blocks.Add("Leakage checks failed.");
        if (!champion.Metrics.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(challenger.Metrics.Keys)) blocks.Add("Champion and challenger metric sets differ.");
        var missing = RequiredMetrics.Where(x => !champion.Metrics.ContainsKey(x) || !challenger.Metrics.ContainsKey(x)).ToArray(); if (missing.Length > 0) blocks.Add($"Required metrics are missing: {string.Join(", ", missing)}.");
        var differences = RequiredMetrics.Where(x => champion.Metrics.ContainsKey(x) && challenger.Metrics.ContainsKey(x)).Select(name => new MetricDifference(name, champion.Metrics[name], challenger.Metrics[name], challenger.Metrics[name] - champion.Metrics[name], name is "ROC_AUC" or "PR_AUC")).ToArray();
        if (differences.FirstOrDefault(x => x.Name == "CalibrationError") is { Change: var calibration } && calibration >= thresholds.CalibrationBlocking) blocks.Add("Calibration materially worsens.");
        foreach (var subgroup in champion.ImportantSubgroupPrAuc.Keys.Order(StringComparer.Ordinal)) { if (!challenger.ImportantSubgroupPrAuc.TryGetValue(subgroup, out var challengerValue)) { blocks.Add($"Important subgroup '{subgroup}' metric is missing."); continue; } var drop = champion.ImportantSubgroupPrAuc[subgroup] - challengerValue; if (drop >= thresholds.MetricBlocking) { var message = $"Important subgroup '{subgroup}' PR AUC worsens by {drop:0.####}."; warnings.Add(message); blocks.Add(message); } }
        return new(Guid.NewGuid(), champion, challenger, DateTime.UtcNow, differences, warnings.ToArray(), blocks.Distinct().ToArray(), blocks.Count == 0, "Comparison does not promote automatically; an eligible challenger requires explicit human approval.");
    }
    private static bool IsSha(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
}

public sealed class HumanGovernedModelManager : IHumanGovernedModelManager
{
    private readonly Dictionary<Guid, ChampionChallengerComparison> _comparisons = [];
    private readonly List<PromotionAuditRecord> _audit = [];
    private MetricDifference[] _differences = []; private string[] _subgroupWarnings = [];
    private string _champion; private string? _previous; private string? _challenger; private string _status = "No pending comparison";
    public HumanGovernedModelManager(string initialChampion = "fasttree-published") => _champion = initialChampion;
    public ModelGovernanceState State => new(_champion, _challenger, _status, _previous is not null, _differences, _subgroupWarnings, _audit.ToArray());
    public Task<ModelGovernanceState> GetStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
    public Task RegisterComparisonAsync(ChampionChallengerComparison comparison, CancellationToken cancellationToken = default) { _comparisons[comparison.Id] = comparison; _challenger = comparison.Challenger.Version; _differences = comparison.MetricDifferences; _subgroupWarnings = comparison.SubgroupWarnings; _status = comparison.PromotionEligible ? "Awaiting human approval" : "Blocked"; return Task.CompletedTask; }
    public Task<PromotionAuditRecord> ApproveAsync(PromotionActionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateAudit(request.ApproverIdentifier, request.ApprovalTimestampUtc, request.Reason); var comparison = Get(request.ComparisonId); if (comparison.SubmitterIdentifier.Equals(request.ApproverIdentifier,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("A challenger submitter cannot approve the same comparison."); if (!comparison.PromotionEligible) throw new InvalidOperationException("Blocked comparison cannot be promoted."); if (_champion != comparison.Champion.Version) throw new InvalidOperationException("Comparison champion is no longer current.");
        var before = _champion; _previous = before; _champion = comparison.Challenger.Version; _challenger = null; _status = "Human-approved promotion"; return Record(request.ComparisonId, PromotionDecision.Approved, before, _champion, comparison.Challenger.Version, request.ApproverIdentifier, request.ApprovalTimestampUtc, request.Reason);
    }
    public Task<PromotionAuditRecord> RejectAsync(PromotionActionRequest request, CancellationToken cancellationToken = default) { ValidateAudit(request.ApproverIdentifier, request.ApprovalTimestampUtc, request.Reason); var comparison = Get(request.ComparisonId); _challenger = null; _status = "Human-rejected promotion"; return Record(request.ComparisonId, PromotionDecision.Rejected, _champion, _champion, comparison.Challenger.Version, request.ApproverIdentifier, request.ApprovalTimestampUtc, request.Reason); }
    public Task<PromotionAuditRecord> RollbackAsync(string approverIdentifier, DateTime approvalTimestampUtc, string reason, CancellationToken cancellationToken = default)
    {
        ValidateAudit(approverIdentifier, approvalTimestampUtc, reason); if (_previous is null) throw new InvalidOperationException("No previously approved champion is available for rollback."); var before = _champion; var target = _previous; _champion = target; _previous = before; _status = "Human-approved rollback"; return Record(Guid.Empty, PromotionDecision.RolledBack, before, target, before, approverIdentifier, approvalTimestampUtc, reason);
    }
    private ChampionChallengerComparison Get(Guid id) => _comparisons.GetValueOrDefault(id) ?? throw new KeyNotFoundException("Comparison was not registered.");
    private Task<PromotionAuditRecord> Record(Guid comparisonId, PromotionDecision decision, string before, string after, string challenger, string approver, DateTime timestamp, string reason) { var record = new PromotionAuditRecord(Guid.NewGuid(), comparisonId, decision, before, after, challenger, approver.Trim(), timestamp.ToUniversalTime(), reason.Trim(), DateTime.UtcNow); _audit.Add(record); return Task.FromResult(record); }
    private static void ValidateAudit(string approver, DateTime timestamp, string reason) { if (string.IsNullOrWhiteSpace(approver) || string.IsNullOrWhiteSpace(reason) || timestamp == default || timestamp > DateTime.UtcNow.AddMinutes(5)) throw new ArgumentException("Approver identifier, valid approval timestamp, and reason are required."); }
}
