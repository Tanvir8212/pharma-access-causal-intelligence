namespace PharmaAccess.Application.MachineLearning;

public enum MlPartition { Training, Validation, Test }
public enum ModelApprovalStatus { Candidate, ValidationSelected, PendingApproval, Approved, Champion, Challenger, Rejected, Archived, Corrupted }
public enum MlRunStatus { Pending, Running, Succeeded, Failed, Cancelled }
public enum TrainerKind { LogisticRegression, FastTree, FastForest, LightGbm }

public sealed record NextQuarterTrainingRow(
    long FeatureRowId, int GenericLaunchId, int DrugId, int StateId, int ObservationQuarterId,
    int LaunchCohortOrdinal, bool FeatureSetValidated, bool HasBlockingQualityIssue, bool HasBlockingLeakageFinding,
    bool IsEligibleState, bool WasAlreadyPresent, bool? LabelNextQuarterEntry,
    float QuarterSinceApproval, float NumberOfObservedQuarters,
    float ObservedPrescriptionCount, float ObservedReimbursementAmount,
    float Lag1PrescriptionCount, float Lag2PrescriptionCount,
    float Lag1ReimbursementAmount, float Lag2ReimbursementAmount,
    float PrescriptionGrowthRate, float ReimbursementGrowthRate,
    float InitialActiveStateCount, float InitialPrescriptionVolume,
    float PreviousQuarterNumericDistribution, float PreviousQuarterWeightedDistribution, float PreviousQuarterAccessGap,
    float StateHistoricalGenericVolume, float StateHistoricalLaunchCount, float StateHistoricalEntryRate,
    float StateHistoricalMedianEntryDelay, float StateHistoricalMarketWeight, float StateVolumePercentile,
    float StateDataCompleteness, float RegionActiveStateShare, float RegionHistoricalEntryRate,
    float NeighborStateAdoptionShare, float SimilarStateAdoptionShare,
    float RegionPrescriptionGrowth, float NationalPrescriptionGrowth,
    float MissingFeatureCount, bool IsObservedZero, bool IsMissing, bool IsSuppressed);

public sealed record TemporalGroupedSplitConfiguration(
    int TrainingEndCohort, int ValidationStartCohort, int ValidationEndCohort,
    int TestStartCohort, int TestEndCohort, int MinimumPositiveCount = 1, int MinimumNegativeCount = 1,
    string GroupingKey = "GenericLaunchId");

public sealed record FeatureSelectionSpecification(string Version, IReadOnlyList<string> OrderedFeatures, string SchemaHash);
public sealed record SplitMembership(long FeatureRowId, int GenericLaunchId, MlPartition Partition);
public sealed record TrainingDataset(
    IReadOnlyList<NextQuarterTrainingRow> Training, IReadOnlyList<NextQuarterTrainingRow> Validation,
    IReadOnlyList<NextQuarterTrainingRow> Test, FeatureSelectionSpecification FeatureSelection,
    IReadOnlyList<SplitMembership> Manifest, IReadOnlyDictionary<string, int> ExcludedRows,
    int CensoredRows, string DatasetHash, string SplitManifestHash);

public sealed record BinaryPrediction(bool Label, float Score, float Probability, bool PredictedLabel);
public sealed record ClassificationMetrics(double RocAuc, double PrAuc, double LogLoss, double LogLossReduction,
    double Accuracy, double Precision, double Recall, double F1, double Specificity, long TruePositive,
    long TrueNegative, long FalsePositive, long FalseNegative, double Prevalence, double PredictedPositiveRate,
    double BrierScore, double Threshold);

public sealed record TrainerEvaluation(TrainerKind Trainer, ClassificationMetrics ValidationMetrics,
    ClassificationMetrics? TestMetrics, long TrainingDurationMs, long ScoringDurationMs, string ConfigurationJson,
    string? ArtifactVersion, ModelApprovalStatus ApprovalStatus, IReadOnlyList<string> Warnings);
public sealed record BaselineEvaluation(string Name, ClassificationMetrics ValidationMetrics, ClassificationMetrics TestMetrics);

public sealed record TrainNextQuarterStateEntryRequest(string ExperimentName, int DatasetVersionId,
    int FeatureSetVersionId, string FeatureSelectionVersion, TemporalGroupedSplitConfiguration Split,
    IReadOnlyList<TrainerKind> Trainers, int RandomSeed, double Threshold, bool PersistPredictions,
    bool ValidateOnly, string CorrelationId, int MaximumRows = 100_000, TimeSpan? Timeout = null);

public sealed record TrainNextQuarterStateEntryResult(string Task, string ExperimentId,
    TrainingDataset Dataset, IReadOnlyList<BaselineEvaluation> Baselines, IReadOnlyList<TrainerEvaluation> Candidates, TrainerEvaluation? Selected,
    string? ArtifactPath, string? ModelCardPath, string? ReproducibilityManifestPath,
    IReadOnlyList<string> Warnings);

public sealed record NextStateEntryPredictionRequest(string? ModelVersion, long FeatureRowId, int FeatureSetVersionId);
public sealed record NextStateEntryPredictionResponse(string Task, float RawProbability, float? CalibratedProbability,
    CalibrationStatus CalibrationStatus, float Score, bool PredictedLabel, double Threshold,
    UncertaintyStatus UncertaintyStatus, IReadOnlyList<string> UncertaintyReasons,
    IReadOnlyList<string> ImportantFeatureSummary, string ModelVersion, ModelApprovalStatus ModelApprovalStatus,
    int DatasetVersionId, int FeatureSetVersionId, int ObservationQuarterId,
    IReadOnlyList<string> Warnings, DateTime CreatedAtUtc)
{
    // Backward-compatible alias for Milestone 4 consumers. RawProbability is the
    // explicit Milestone 5 name; calibration never overwrites the raw score.
    public float Probability => RawProbability;
}

public interface INextQuarterStateEntryTrainingService
{
    Task<TrainNextQuarterStateEntryResult> TrainAsync(TrainNextQuarterStateEntryRequest request, IReadOnlyCollection<NextQuarterTrainingRow> rows, CancellationToken cancellationToken = default);
}

public interface INextStateEntryPredictionService
{
    Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request, CancellationToken cancellationToken = default);
}

public sealed record SelectedModelArtifact(string Version, string ModelPath, string ManifestPath, string Sha256,
    long FileSize, string SchemaHash, ModelApprovalStatus Status, int DatasetVersionId, int FeatureSetVersionId,
    double Threshold);

public interface IModelArtifactRegistry
{
    Task<SelectedModelArtifact?> ResolveAsync(string? modelVersion, CancellationToken cancellationToken);
}

public interface ITrainingFeatureRowResolver
{
    Task<NextQuarterTrainingRow?> ResolveAsync(long featureRowId, int featureSetVersionId, CancellationToken cancellationToken);
}
