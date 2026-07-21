using PharmaAccess.Domain.Causal;

namespace PharmaAccess.Application.Causal;

public enum CausalEstimatorKind { UnadjustedDifferenceInMeans, PropensityScoreWeighting, OutcomeRegression, AugmentedInverseProbabilityWeighting }
public enum SensitivityAnalysisKind { PropensityClipping, AlternativeTreatmentThreshold, AlternativeAdjustmentSet, AlternativeStateEntryPolicy, AlternativeEligibleStatePolicy, FutureTreatmentPlacebo, UnmeasuredConfoundingUnsupported }
public enum PlaceboTestKind { FutureTreatment, PreTreatmentOutcome, RandomizedTreatmentPermutation }

public sealed record PropensityConfiguration(int MaximumIterations = 500, double LearningRate = .05, double ClipLower = .02, double ClipUpper = .98, int Seed = 17);
public sealed record WeightingConfiguration(double ExtremeWeightThreshold = 10, bool Stabilized = false);
public sealed record BootstrapConfiguration(int Repetitions = 200, int Seed = 17, int MinimumSuccessfulRepetitions = 100, double ConfidenceLevel = .95);
public sealed record DiagnosticThresholds(double MaximumAbsoluteSmd = .1, double MinimumEffectiveSampleSize = 20, int MinimumTreated = 10, int MinimumControl = 10);
public sealed record RunCausalStudyCommand(long CausalStudyId, int DatasetVersionId, int FeatureSetVersionId,
    string TreatmentDefinitionVersion, string OutcomeDefinitionVersion, string DagVersion, string AdjustmentSetVersion,
    EstimandType Estimand, IReadOnlyList<CausalEstimatorKind> Estimators, int ObservationStartQuarter,
    int ObservationEndQuarter, MissingDataPolicy MissingDataPolicy, PropensityConfiguration Propensity,
    WeightingConfiguration Weighting, BootstrapConfiguration Bootstrap, DiagnosticThresholds Diagnostics,
    bool DryRun, string CorrelationId, string RequestedBy);

public sealed record CausalInputRow(long FeatureRowId, int GenericLaunchId, int DrugId, int StateId, string Region,
    int LaunchCohort, int ObservationQuarterId, int OutcomeQuarterId, int BaselineQuarterId, int TreatmentQuarterId,
    int QuarterSinceApproval, bool FeatureSetValidated, bool HasBlockingIssue, bool WasAlreadyEntered,
    bool? Outcome, bool IsCensored, double ContinuousExposure, bool? BinaryTreatment, int EligiblePeerCount,
    bool UsesFutureAdoption, string HistoricalVolumeBand, string NationalDiffusionBand,
    IReadOnlyDictionary<string, double?> Confounders, IReadOnlyList<string> Warnings);

public sealed record CausalAnalysisRowContract(long FeatureRowId, int GenericLaunchId, int DrugId, int StateId,
    string Region, int LaunchCohort, int ObservationQuarterId, int OutcomeQuarterId, int QuarterSinceApproval,
    double TreatmentValue, bool BinaryTreatment, bool OutcomeValue, IReadOnlyDictionary<string, double> Confounders,
    string HistoricalVolumeBand, string NationalDiffusionBand, string RowHash);
public sealed record PropensityResult(IReadOnlyList<double> Scores, IReadOnlyList<double> Coefficients, string ConfigurationHash, IReadOnlyList<string> Warnings);
public sealed record WeightResult(IReadOnlyList<double> Weights, double EffectiveSampleSize, double MaximumWeight, double P95Weight, int ExtremeWeightCount, IReadOnlyList<string> Warnings);
public sealed record CovariateBalance(string Variable, double TreatedMean, double ControlMean, double StandardizedMeanDifference, double WeightedTreatedMean, double WeightedControlMean, double WeightedStandardizedMeanDifference, double? VarianceRatio, double MissingnessDifference, DiagnosticStatus Status);
public sealed record PositivityDiagnostic(double MinimumPropensity, double MaximumPropensity, double TreatedMinimum, double TreatedMaximum, double ControlMinimum, double ControlMaximum, double CommonSupportLower, double CommonSupportUpper, int OutsideCommonSupport, double EffectiveSampleSize, int ExtremeWeightCount, double TreatmentPrevalence, DiagnosticStatus Status, IReadOnlyList<string> Warnings);
public sealed record CausalEstimatorResult(CausalEstimatorKind Estimator, EstimandType Estimand, EffectScale EffectScale,
    double Estimate, double? StandardError, double? ConfidenceLower, double? ConfidenceUpper, int TreatedCount,
    int ControlCount, double? EffectiveSampleSize, CausalEstimateStatus Status, string Interpretation,
    IReadOnlyList<string> Limitations);
public sealed record BootstrapResult(double StandardError, double Lower, double Upper, int SuccessfulRepetitions, int FailedRepetitions, string Method);
public sealed record SensitivityResult(SensitivityAnalysisKind Analysis, DiagnosticStatus Status, double? Estimate, string Description);
public sealed record PlaceboResult(PlaceboTestKind Test, double Estimate, DiagnosticStatus Status, string Interpretation);
public sealed record HeterogeneousEffect(string Dimension, string Value, int TreatedCount, int ControlCount, double? Estimate, DiagnosticStatus Status, string Warning);
public sealed record CounterfactualScenarioRequest(string ScenarioVersion, string ScenarioCode, double InterventionTreatment, double ObservedSupportMinimum, double ObservedSupportMaximum);
public sealed record CounterfactualScenarioResult(string ScenarioVersion, long CausalStudyId, string EstimatorVersion, double BaselinePredictedOutcome, double InterventionPredictedOutcome, double EstimatedDifference, SimulationSupportStatus SupportStatus, double ExtrapolationDistance, string UncertaintyMethod, IReadOnlyList<string> Limitations);
public sealed record RunCausalStudyResult(long CausalStudyId, int EligibleRowCount, int TreatedCount, int ControlCount,
    IReadOnlyDictionary<string, int> ExcludedRows, double TreatmentPrevalence, EstimandType Estimand,
    IReadOnlyList<CausalEstimatorResult> Estimates, IReadOnlyList<CovariateBalance> Balance,
    PositivityDiagnostic? Positivity, IReadOnlyList<SensitivityResult> Sensitivity,
    IReadOnlyList<PlaceboResult> Placebos, IReadOnlyList<HeterogeneousEffect> Heterogeneity,
    IReadOnlyList<string> BlockingFindings, IReadOnlyList<string> Warnings, string ReproducibilityHash,
    IReadOnlyList<string> ReportPaths, TimeSpan Duration, bool DryRun);

public interface ICausalStudyRunner
{
    Task<RunCausalStudyResult> RunAsync(RunCausalStudyCommand command, IReadOnlyCollection<CausalInputRow> source, CancellationToken cancellationToken = default);
}
