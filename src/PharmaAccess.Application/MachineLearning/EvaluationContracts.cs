namespace PharmaAccess.Application.MachineLearning;

public enum CalibrationMethod { Uncalibrated, Platt }
public enum CalibrationStatus { NotAttempted, InsufficientData, Fitted, Validated, Failed, Rejected }
public enum CalibrationBinning { EqualWidth, EqualFrequency }
public enum UncertaintyStatus { Low, Moderate, High, Unsupported }
public enum ThresholdPolicyKind { FixedThreshold, MaximumF1, MinimumRecall, MinimumPrecision, YoudenIndex, CostSensitive }
public enum EvaluationStatus { Complete, InsufficientData, Failed }
public enum ErrorCategory { FalsePositive, FalseNegative, HighConfidenceFalsePositive, HighConfidenceFalseNegative, UnsupportedPrediction, SchemaWarning, HighMissingness, OutOfDistribution }
public enum ApprovalDecision { Approve, Reject }

public sealed record ScoredEvaluationRow(long FeatureRowId, long ModelTrainingRunId, int DrugId, int StateId,
    string Region, int LaunchCohort, int ObservationQuarterId, int QuarterSinceApproval,
    bool Label, double RawProbability, double? CalibratedProbability, int MissingFeatureCount,
    int OutOfRangeFeatureCount, int UnseenCategoryCount, double TrainingDistributionDistance,
    IReadOnlyDictionary<string, float> Features, IReadOnlyList<string> Warnings);

public sealed record CalibrationParameters(double Slope, double Intercept);
public sealed record CalibrationBin(int BinIndex, double MinimumProbability, double MaximumProbability,
    int RowCount, double MeanPredictedProbability, double ObservedRate);
public sealed record CalibrationEvaluation(double BrierScore, double LogLoss, double ExpectedCalibrationError,
    double MaximumCalibrationError, double CalibrationSlope, double CalibrationIntercept,
    IReadOnlyList<CalibrationBin> Bins, IReadOnlyList<string> Warnings);
public sealed record CalibrationResult(CalibrationMethod Method, CalibrationStatus Status,
    CalibrationParameters? Parameters, CalibrationEvaluation? ValidationEvaluation,
    CalibrationEvaluation? TestEvaluation, IReadOnlyList<string> Warnings);

public sealed record UncertaintyAssessment(UncertaintyStatus Status, double DistanceFromThreshold,
    int CalibrationBinSampleSize, IReadOnlyList<string> Reasons);
public sealed record FeatureImportanceResult(long ModelTrainingRunId, string FeatureName, string Metric,
    double BaselineValue, double PermutedValue, double ImportanceDelta, double StandardDeviation,
    int RepetitionCount, int Rank, DateTime CreatedAtUtc, IReadOnlyList<string> Warnings);
public sealed record SubgroupEvaluation(string Dimension, string Value, EvaluationStatus Status, int RowCount,
    int PositiveCount, int NegativeCount, ClassificationMetrics? Metrics, double? CalibrationError,
    IReadOnlyList<string> Warnings);
public sealed record ModelErrorRecord(long ModelTrainingRunId, int DrugId, int StateId, int ObservationQuarterId,
    bool ActualLabel, double Probability, bool PredictedLabel, double Threshold, ErrorCategory Category,
    int MissingFeatureCount, UncertaintyStatus UncertaintyStatus, string Region, int LaunchCohort,
    int QuarterSinceApproval, IReadOnlyDictionary<string, float> KeyFeatureContext, IReadOnlyList<string> Warnings);
public sealed record ThresholdPolicy(ThresholdPolicyKind Kind, double FixedThreshold = .5,
    double? MinimumRecall = null, double? MinimumPrecision = null, double FalsePositiveCost = 1,
    double FalseNegativeCost = 1);
public sealed record ThresholdEvaluation(ThresholdPolicyKind Policy, double Threshold,
    ClassificationMetrics ValidationMetrics, string Objective, IReadOnlyList<string> Warnings);

public sealed record ApproveModelVersionCommand(long ModelArtifactId, ApprovalDecision Decision,
    string ApprovedBy, string ApprovalReason, string TargetEnvironment, bool PromoteToChampion,
    string ModelCardVersion);
public sealed record ModelRegistryRecord(long ModelArtifactId, string Task, string Environment,
    ModelApprovalStatus Status, bool IsSyntheticDevelopmentOnly, bool ArtifactIntegrityValid,
    string FeatureSchemaHash, string ExpectedFeatureSchemaHash, string? ApprovedBy,
    DateTime? ApprovedAtUtc, string? ApprovalReason, string? ModelCardVersion);

public interface IModelRegistryRepository
{
    Task<ModelRegistryRecord?> GetAsync(long artifactId, CancellationToken cancellationToken);
    Task<ModelRegistryRecord?> GetChampionAsync(string task, string environment, CancellationToken cancellationToken);
    Task SaveApprovalAsync(ModelRegistryRecord updated, ModelRegistryRecord? previousChampion, CancellationToken cancellationToken);
}

public sealed class ModelApprovalService(IModelRegistryRepository repository)
{
    public async Task<ModelRegistryRecord> ExecuteAsync(ApproveModelVersionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ApprovedBy) || string.IsNullOrWhiteSpace(command.ApprovalReason) || string.IsNullOrWhiteSpace(command.TargetEnvironment) || string.IsNullOrWhiteSpace(command.ModelCardVersion)) throw new ArgumentException("Approval audit metadata is required.");
        var model = await repository.GetAsync(command.ModelArtifactId, cancellationToken) ?? throw new KeyNotFoundException("Model artifact is not registered.");
        if (!model.ArtifactIntegrityValid || model.Status == ModelApprovalStatus.Corrupted) throw new InvalidOperationException("Corrupted artifacts cannot be approved.");
        if (!model.FeatureSchemaHash.Equals(model.ExpectedFeatureSchemaHash, StringComparison.Ordinal)) throw new InvalidOperationException("Feature schema is incompatible.");
        if (command.PromoteToChampion && command.Decision != ApprovalDecision.Approve) throw new InvalidOperationException("Only an approval decision may promote a champion.");
        if (command.PromoteToChampion && model.IsSyntheticDevelopmentOnly && !command.TargetEnvironment.Equals("Development", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Synthetic models are development-only.");
        var status = command.Decision == ApprovalDecision.Reject ? ModelApprovalStatus.Rejected : command.PromoteToChampion ? ModelApprovalStatus.Champion : ModelApprovalStatus.Approved;
        var previous = command.PromoteToChampion ? await repository.GetChampionAsync(model.Task, command.TargetEnvironment, cancellationToken) : null;
        var updated = model with { Environment = command.TargetEnvironment, Status = status, ApprovedBy = command.ApprovedBy.Trim(), ApprovedAtUtc = DateTime.UtcNow, ApprovalReason = command.ApprovalReason.Trim(), ModelCardVersion = command.ModelCardVersion.Trim() };
        await repository.SaveApprovalAsync(updated, previous is null || previous.ModelArtifactId == updated.ModelArtifactId ? null : previous with { Status = ModelApprovalStatus.Archived }, cancellationToken);
        return updated;
    }
}
