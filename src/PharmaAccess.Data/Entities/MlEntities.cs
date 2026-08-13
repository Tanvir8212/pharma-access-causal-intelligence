using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.Data.Entities;

public sealed class MlExperiment
{
    private MlExperiment() { }
    internal MlExperiment(string name, int datasetVersionId, int featureSetVersionId, string configurationJson, DateTime atUtc)
    {
        ExperimentName = name; TaskName = "NextQuarterStateEntry"; DatasetVersionId = datasetVersionId; FeatureSetVersionId = featureSetVersionId;
        FeatureSelectionVersion = "real-next-entry-features-v1"; SplitManifestVersion = "1E6909E1CA6F92D8CBA7944CDBC03E765B3042BE8744F16F6ACECDE18A9DE748";
        ResearchQuestion = "Recovered verified real-model serving candidate registration."; PrimaryMetric = "PR-AUC"; RandomSeed = 17;
        ConfigurationJson = configurationJson; StartedAtUtc = atUtc; CompletedAtUtc = atUtc; Status = MlRunStatus.Succeeded; Notes = "ValidationSelected candidate; not approved or active.";
    }
    public long ExperimentId { get; private set; } public string ExperimentName { get; private set; } = null!; public string TaskName { get; private set; } = null!;
    public int DatasetVersionId { get; private set; } public int FeatureSetVersionId { get; private set; } public string FeatureSelectionVersion { get; private set; } = null!; public string SplitManifestVersion { get; private set; } = null!;
    public string ResearchQuestion { get; private set; } = null!; public string PrimaryMetric { get; private set; } = null!; public int RandomSeed { get; private set; } public string ConfigurationJson { get; private set; } = null!; public string? CodeCommitHash { get; private set; }
    public DateTime StartedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public MlRunStatus Status { get; private set; } public string? FailureMessage { get; private set; } public string? Notes { get; private set; }
}

public sealed class ModelTrainingRun
{
    private ModelTrainingRun() { }
    internal ModelTrainingRun(long experimentId, string hyperparametersJson, DateTime atUtc)
    {
        ExperimentId = experimentId; TrainerName = "FastTree"; Algorithm = "FastTree"; HyperparametersJson = hyperparametersJson;
        StartedAtUtc = atUtc; CompletedAtUtc = atUtc; Status = MlRunStatus.Succeeded;
    }
    public long ModelTrainingRunId { get; private set; } public long ExperimentId { get; private set; } public string TrainerName { get; private set; } = null!; public string Algorithm { get; private set; } = null!; public string HyperparametersJson { get; private set; } = null!;
    public int TrainingRowCount { get; private set; } public int ValidationRowCount { get; private set; } public int TestRowCount { get; private set; } public int PositiveTrainingCount { get; private set; } public int NegativeTrainingCount { get; private set; } public int FeatureCount { get; private set; }
    public DateTime StartedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public long? TrainingDurationMs { get; private set; } public MlRunStatus Status { get; private set; } public string? FailureMessage { get; private set; }
}

public sealed class ModelMetric
{
    private ModelMetric() { }
    public long ModelMetricId { get; private set; } public long ModelTrainingRunId { get; private set; } public MlPartition Partition { get; private set; } public string MetricName { get; private set; } = null!; public double MetricValue { get; private set; } public double? Threshold { get; private set; } public string? SubgroupName { get; private set; } public string? SubgroupValue { get; private set; } public DateTime CreatedAtUtc { get; private set; }
}

public sealed class ModelArtifact
{
    private ModelArtifact() { }
    internal ModelArtifact(long runId, string version, string path, string sha256, long size, string schemaHash, string featureHash, string notes, DateTime atUtc)
    {
        ModelTrainingRunId = runId; ModelVersionCode = version; ArtifactPath = path; Sha256 = sha256; FileSize = size;
        InputSchemaHash = schemaHash; FeatureSchemaHash = featureHash; ApprovalNotes = notes; CreatedAtUtc = atUtc;
        IsApproved = false; ApprovalStatus = ModelApprovalStatus.ValidationSelected;
    }
    public long ModelArtifactId { get; private set; } public long ModelTrainingRunId { get; private set; } public string ModelVersionCode { get; private set; } = null!; public string ArtifactPath { get; private set; } = null!; public string Sha256 { get; private set; } = null!; public long FileSize { get; private set; } public string InputSchemaHash { get; private set; } = null!; public string FeatureSchemaHash { get; private set; } = null!; public DateTime CreatedAtUtc { get; private set; } public bool IsApproved { get; private set; } public ModelApprovalStatus ApprovalStatus { get; private set; } public string? ApprovalNotes { get; private set; }
    internal void Approve() { if(IsApproved||ApprovalStatus!=ModelApprovalStatus.ValidationSelected) throw new InvalidOperationException("Artifact is not eligible for approval."); IsApproved=true; ApprovalStatus=ModelApprovalStatus.Approved; }
}

public sealed class PredictionRecord
{
    private PredictionRecord() { }
    public long PredictionRecordId { get; private set; } public long ModelTrainingRunId { get; private set; } public MlPartition Partition { get; private set; } public int DrugId { get; private set; } public int StateId { get; private set; } public int ObservationQuarterId { get; private set; } public bool Label { get; private set; } public float Score { get; private set; } public float Probability { get; private set; } public bool PredictedLabel { get; private set; } public double Threshold { get; private set; } public DateTime CreatedAtUtc { get; private set; }
}
