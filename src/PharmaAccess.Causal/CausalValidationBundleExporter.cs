using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PharmaAccess.Application.Causal;
using PharmaAccess.Domain.Causal;

namespace PharmaAccess.Causal;

public sealed class CausalValidationBundleExporter
{
    public const string ContractVersion = "1.1";
    public ExportCausalValidationBundleResult ExportSynthetic(ExportCausalValidationBundleCommand request)
    {
        ValidateRequest(request);
        var root = SafeRunRoot(request.OutputDirectory, request.ValidationRunCode);
        if (Directory.Exists(root))
        {
            if (request.OverwritePolicy == ValidationOverwritePolicy.Reject) throw new IOException("Validation run already exists.");
            Directory.Delete(root, true);
        }
        Directory.CreateDirectory(root);
        var command = new RunCausalStudyCommand(request.CausalStudyId, 1, 1, "neighbor-v1", "next-entry-v1", "dag-v1", "adjust-v1", EstimandType.AverageTreatmentEffectOnTreated, Enum.GetValues<CausalEstimatorKind>(), 20210, 20214, MissingDataPolicy.CompleteCase, new(), new(), new(20, 17, 10), new(.2, 10, 5, 5), false, request.CorrelationId, "milestone-7-synthetic");
        var source = SyntheticCausalData.Create();
        var rows = BuildRows(source, command, out var exclusions);
        var variables = rows[0].Confounders.Keys.Order(StringComparer.Ordinal).ToArray();
        var propensity = new LogisticNuisanceModel().Fit(rows, variables, command.Propensity);
        var weights = PropensityWeighting.Calculate(rows, propensity.Scores, command.Estimand, command.Weighting);
        var outcome = CausalEstimators.OutcomeRegression(rows, variables, command.Estimand);
        var estimates = new[] { CausalEstimators.Unadjusted(rows, command.Estimand), CausalEstimators.Weighted(rows, weights, command.Estimand), outcome.Result, CausalEstimators.Aipw(rows, propensity.Scores, outcome.M1, outcome.M0, command.Estimand) };
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        files["analysis_rows.csv"] = Csv(rows, variables);
        files["schema.json"] = Json(new { contractVersion = ContractVersion, analyticalKey = new[] { "feature_row_id" }, nullRepresentation = "", unexpectedColumns = "reject", columns = BaseColumns.Concat(variables).ToArray() });
        files["study_manifest.json"] = Json(new { contractVersion = ContractVersion, validationRunCode = request.ValidationRunCode, causalStudyId = request.CausalStudyId, causalStudyCode = "synthetic-peer-exposure", datasetVersion = "synthetic-v1", featureSetVersion = "synthetic-feature-v1", dagVersion = "dag-v1", adjustmentSetVersion = "adjust-v1", treatmentDefinitionVersion = "neighbor-v1", outcomeDefinitionVersion = "next-entry-v1", estimand = "ATT", effectScale = "RiskDifference", observationWindow = new { start = 20210, end = 20214 }, rowCount = rows.Count, treatedCount = rows.Count(x => x.BinaryTreatment), controlCount = rows.Count(x => !x.BinaryTreatment), excludedRowCounts = exclusions, randomSeeds = new { nuisance = 17, folds = 17 }, numericPrecisionPolicy = "IEEE-754 round-trip (R), formula abs=1e-9 rel=1e-8", sourceCodeCommit = GitCommit(), generatedTimestampUtc = "DECLARED_NONDETERMINISTIC", synthetic = true });
        var dagNodes = variables.Select(x => new { name = x, role = "Confounder" }).Concat([new { name = "treatment", role = "Treatment" }, new { name = "outcome", role = "Outcome" }, new { name = "UnmeasuredCommercialFactors", role = "Unobserved" }]).ToArray();
        var dagEdges = variables.SelectMany(x => new[] { new { from = x, to = "treatment" }, new { from = x, to = "outcome" } }).Concat([new { from = "treatment", to = "outcome" }]).ToArray();
        files["dag.json"] = Json(new { version = "dag-v1", humanAuthored = true, nodes = dagNodes, edges = dagEdges, warning = "Assumed graph; not proven correct." });
        files["adjustment_set.json"] = Json(new { version = "adjust-v1", variables, hash = Sha(Json(new { variables })) });
        files["treatment_definition.json"] = Json(new { version = "neighbor-v1", column = "treatment", domain = new[] { 0, 1 }, threshold = .5 });
        files["outcome_definition.json"] = Json(new { version = "next-entry-v1", column = "outcome", domain = new[] { 0, 1 }, effectScale = "RiskDifference" });
        files["fold_manifest.json"] = Json(Folds(rows, 5));
        files["csharp_estimates.json"] = Json(new CausalEstimateExchangeDocument(
            ContractVersion,
            "ATT",
            "RiskDifference",
            estimates.Select(ToExchangeEstimate).ToArray()));
        files["csharp_diagnostics.json"] = Json(new { balance = CausalDiagnostics.Balance(rows, weights.Weights, variables, .1), positivity = CausalDiagnostics.Positivity(rows, propensity.Scores, weights, command.Diagnostics), warnings = propensity.Warnings.Concat(weights.Warnings) });
        if (request.IncludeNuisancePredictions) files["nuisance_predictions.csv"] = NuisanceCsv(rows, propensity.Scores, outcome.M1, outcome.M0, weights.Weights);
        foreach (var file in files) File.WriteAllBytes(Path.Combine(root, file.Key), file.Value);
        var hashes = files.ToDictionary(x => x.Key, x => Sha(x.Value), StringComparer.Ordinal);
        var reproducibility = Sha(Encoding.UTF8.GetBytes(string.Join('\n', hashes.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"))));
        var hashDocument = Json(new { contractVersion = ContractVersion, files = hashes, reproducibilityHash = reproducibility });
        File.WriteAllBytes(Path.Combine(root, "file_hashes.json"), hashDocument);
        hashes["file_hashes.json"] = Sha(hashDocument);
        return new(request.ValidationRunCode, hashes.Keys.Select(x => Path.Combine(root, x)).ToArray(), rows.Count, rows.Count(x => x.BinaryTreatment), rows.Count(x => !x.BinaryTreatment), hashes, [CausalReportWriter.SyntheticNotice], reproducibility);
    }

    private static readonly string[] BaseColumns = ["feature_row_id", "generic_launch_id", "drug_id", "state_id", "region", "launch_cohort", "observation_quarter_id", "outcome_quarter_id", "quarter_since_approval", "continuous_exposure", "treatment", "outcome", "row_hash"];
    private static List<CausalAnalysisRowContract> BuildRows(IReadOnlyList<CausalInputRow> source, RunCausalStudyCommand command, out Dictionary<string, int> excluded)
    {
        excluded = []; var names = source.SelectMany(x => x.Confounders.Keys).Distinct().Order().ToArray(); var rows = new List<CausalAnalysisRowContract>();
        foreach (var r in source.OrderBy(x => x.GenericLaunchId).ThenBy(x => x.StateId).ThenBy(x => x.ObservationQuarterId))
        {
            if (r.IsCensored || r.Outcome is null) { excluded["CensoredOrMissingOutcome"] = excluded.GetValueOrDefault("CensoredOrMissingOutcome") + 1; continue; }
            var c = names.ToDictionary(x => x, x => r.Confounders[x]!.Value); var canonical = string.Join('|', command.CausalStudyId, r.DrugId, r.StateId, r.ObservationQuarterId, r.ContinuousExposure.ToString("R", CultureInfo.InvariantCulture), r.BinaryTreatment, r.Outcome, string.Join(';', c.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value:R}"))); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
            rows.Add(new(r.FeatureRowId, r.GenericLaunchId, r.DrugId, r.StateId, r.Region, r.LaunchCohort, r.ObservationQuarterId, r.OutcomeQuarterId, r.QuarterSinceApproval, r.ContinuousExposure, r.BinaryTreatment!.Value, r.Outcome.Value, c, r.HistoricalVolumeBand, r.NationalDiffusionBand, hash));
        }
        return rows;
    }
    private static object Folds(IReadOnlyList<CausalAnalysisRowContract> rows, int count) { var groups = rows.Select(x => x.GenericLaunchId).Distinct().Order().Select((id, i) => new { genericLaunchId = id, fold = i % count }).ToArray(); return new { version = "fold-v1", groupKey = "generic_launch_id", foldCount = count, seed = 17, groups, hash = Sha(Json(groups)) }; }
    private static CausalEstimateExchange ToExchangeEstimate(CausalEstimatorResult value) => new(
        value.Estimator switch
        {
            CausalEstimatorKind.UnadjustedDifferenceInMeans => "UnadjustedDifferenceInMeans",
            CausalEstimatorKind.PropensityScoreWeighting => "PropensityScoreWeighting",
            CausalEstimatorKind.OutcomeRegression => "OutcomeRegression",
            CausalEstimatorKind.AugmentedInverseProbabilityWeighting => "AugmentedInverseProbabilityWeighting",
            _ => throw new InvalidOperationException("Unsupported estimator contract identifier.")
        },
        value.Estimand switch
        {
            EstimandType.AverageTreatmentEffect => "ATE",
            EstimandType.AverageTreatmentEffectOnTreated => "ATT",
            _ => throw new InvalidOperationException("Unsupported estimand contract identifier.")
        },
        value.EffectScale switch
        {
            EffectScale.RiskDifference => "RiskDifference",
            EffectScale.RiskRatio => "RiskRatio",
            EffectScale.OddsRatio => "OddsRatio",
            _ => throw new InvalidOperationException("Unsupported effect-scale contract identifier.")
        },
        value.Estimate, value.StandardError, value.ConfidenceLower, value.ConfidenceUpper,
        value.TreatedCount, value.ControlCount, value.EffectiveSampleSize);
    private static byte[] Csv(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<string> vars) { var b = new StringBuilder(); b.AppendLine(string.Join(',', BaseColumns.Concat(vars))); foreach (var r in rows) b.AppendLine(string.Join(',', new[] { I(r.FeatureRowId), I(r.GenericLaunchId), I(r.DrugId), I(r.StateId), Escape(r.Region), I(r.LaunchCohort), I(r.ObservationQuarterId), I(r.OutcomeQuarterId), I(r.QuarterSinceApproval), D(r.TreatmentValue), r.BinaryTreatment ? "1" : "0", r.OutcomeValue ? "1" : "0", r.RowHash }.Concat(vars.Select(v => D(r.Confounders[v]))))); return Encoding.UTF8.GetBytes(b.ToString()); }
    private static byte[] NuisanceCsv(IReadOnlyList<CausalAnalysisRowContract> rows, IReadOnlyList<double> p, IReadOnlyList<double> m1, IReadOnlyList<double> m0, IReadOnlyList<double> w) { var b = new StringBuilder("feature_row_id,treatment,outcome,propensity_score,outcome_prediction_treated,outcome_prediction_control,final_weight,generic_launch_id\n"); for (var i = 0; i < rows.Count; i++) b.AppendLine(string.Join(',', I(rows[i].FeatureRowId), rows[i].BinaryTreatment ? "1" : "0", rows[i].OutcomeValue ? "1" : "0", D(p[i]), D(m1[i]), D(m0[i]), D(w[i]), I(rows[i].GenericLaunchId))); return Encoding.UTF8.GetBytes(b.ToString()); }
    private static byte[] Json(object value) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    private static string Sha(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant(); private static string D(double x) => x.ToString("R", CultureInfo.InvariantCulture); private static string I(long x) => x.ToString(CultureInfo.InvariantCulture); private static string Escape(string x) => x.IndexOfAny([',', '"', '\r', '\n']) < 0 ? x : $"\"{x.Replace("\"", "\"\"")}\"";
    private static void ValidateRequest(ExportCausalValidationBundleCommand x) { if (x.CausalStudyId <= 0 || string.IsNullOrWhiteSpace(x.CorrelationId) || string.IsNullOrWhiteSpace(x.ValidationRunCode) || x.ValidationRunCode.Any(c => !char.IsLetterOrDigit(c) && c is not '-' and not '_')) throw new ArgumentException("Validation export request is invalid."); }
    private static string SafeRunRoot(string output, string code) { var root = Path.GetFullPath(output); var run = Path.GetFullPath(Path.Combine(root, code)); if (!run.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output path traversal is prohibited."); return run; }
    private static string GitCommit() => "WORKTREE-NOT-COMMITTED";
}

internal sealed record CausalEstimateExchangeDocument(
    string ContractVersion,
    string Estimand,
    string EffectScale,
    IReadOnlyList<CausalEstimateExchange> Estimates);

internal sealed record CausalEstimateExchange(
    string Estimator,
    string Estimand,
    string EffectScale,
    double Estimate,
    double? StandardError,
    double? ConfidenceLower,
    double? ConfidenceUpper,
    int TreatedCount,
    int ControlCount,
    double? EffectiveSampleSize);
