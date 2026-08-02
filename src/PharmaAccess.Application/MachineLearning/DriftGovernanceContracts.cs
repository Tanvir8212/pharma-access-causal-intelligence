namespace PharmaAccess.Application.MachineLearning;

public enum DriftSeverity { None, Informational, Warning, Blocking }
public enum PromotionDecision { Approved, Rejected, RolledBack }

public sealed record DriftThresholds(
    double PsiInformational = .05, double PsiWarning = .10, double PsiBlocking = .20,
    double KsInformational = .05, double KsWarning = .10, double KsBlocking = .20,
    double CategoricalInformational = .02, double CategoricalWarning = .05, double CategoricalBlocking = .10,
    double MetricInformational = .01, double MetricWarning = .03, double MetricBlocking = .05,
    double CalibrationInformational = .01, double CalibrationWarning = .03, double CalibrationBlocking = .05);
public sealed record LabeledPrediction(bool Label, double Probability, string Subgroup);
public sealed record NumericDriftInput(string Name, double[] Reference, double[] Current);
public sealed record CategoricalDriftInput(string Name, string[] Reference, string[] Current);
public sealed record DriftReportRequest(string ChampionVersion, string EvaluationWindow,
    NumericDriftInput[] NumericFeatures, CategoricalDriftInput[] CategoricalFeatures,
    double[] ReferencePredictions, double[] CurrentPredictions,
    LabeledPrediction[]? ReferenceLabeled, LabeledPrediction[]? CurrentLabeled);
public sealed record DriftMeasure(string Scope, string Name, string Statistic, double ReferenceValue,
    double CurrentValue, double Change, DriftSeverity Severity, string Formula);
public sealed record DriftReport(Guid Id, string ChampionVersion, string EvaluationWindow, DateTime CreatedAtUtc,
    DriftSeverity Severity, DriftMeasure[] Measures, string[] SubgroupWarnings,
    bool LabelsAvailable, string GovernanceNotice);

public interface IDriftDetector { DriftReport Detect(DriftReportRequest request); }
public interface IDriftReportStore
{
    Task SaveAsync(DriftReport report, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriftReport>> ListAsync(CancellationToken cancellationToken = default);
    Task<DriftReport?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record GovernedModelSnapshot(string Version, string ArtifactSha256, bool ArtifactHashValid,
    string FeatureSchemaHash, string EvaluationCohortHash, string DatasetHash, string ReproducibilityHash,
    bool LeakageChecksPassed, IReadOnlyDictionary<string, double> Metrics,
    IReadOnlyDictionary<string, double> ImportantSubgroupPrAuc, string ArtifactPath = "",
    string DatasetFreezeIdentifier = "");
public sealed record MetricDifference(string Name, double Champion, double Challenger, double Change, bool HigherIsBetter);
public sealed record ChampionChallengerComparison(Guid Id, GovernedModelSnapshot Champion,
    GovernedModelSnapshot Challenger, DateTime CreatedAtUtc, MetricDifference[] MetricDifferences,
    string[] SubgroupWarnings, string[] BlockingReasons, bool PromotionEligible, string GovernanceNotice,
    string SubmitterIdentifier = "");
public interface IChampionChallengerComparer { ChampionChallengerComparison Compare(GovernedModelSnapshot champion, GovernedModelSnapshot challenger); }

public sealed record PromotionActionRequest(Guid ComparisonId, string ApproverIdentifier, DateTime ApprovalTimestampUtc, string Reason);
public sealed record PromotionAuditRecord(Guid Id, Guid ComparisonId, PromotionDecision Decision,
    string ChampionBefore, string ChampionAfter, string ChallengerVersion, string ApproverIdentifier,
    DateTime ActionTimestampUtc, string Reason, DateTime RecordedAtUtc);
public sealed record ModelGovernanceState(string ChampionVersion, string? ChallengerVersion,
    string PromotionStatus, bool RollbackAvailable, MetricDifference[] MetricDifferences,
    string[] SubgroupWarnings, PromotionAuditRecord[] AuditRecords);
public interface IHumanGovernedModelManager
{
    ModelGovernanceState State { get; }
    Task<ModelGovernanceState> GetStateAsync(CancellationToken cancellationToken = default);
    Task RegisterComparisonAsync(ChampionChallengerComparison comparison, CancellationToken cancellationToken = default);
    Task<PromotionAuditRecord> ApproveAsync(PromotionActionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionAuditRecord> RejectAsync(PromotionActionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionAuditRecord> RollbackAsync(string approverIdentifier, DateTime approvalTimestampUtc, string reason, CancellationToken cancellationToken = default);
}

public interface IModelGovernanceRepository
{
    Task SaveComparisonAsync(ChampionChallengerComparison comparison, CancellationToken cancellationToken = default);
    Task<ChampionChallengerComparison?> GetComparisonAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ModelGovernanceState> GetStateAsync(CancellationToken cancellationToken = default);
    Task<PromotionAuditRecord> ExecuteDecisionAsync(PromotionDecision decision, PromotionActionRequest request, CancellationToken cancellationToken = default);
    Task<PromotionAuditRecord> ExecuteRollbackAsync(string approverIdentifier, DateTime timestampUtc, string reason, CancellationToken cancellationToken = default);
}

public sealed record ArtifactVerificationRequest(string ArtifactPath, string ExpectedSha256);
public sealed record ArtifactVerificationResult(bool IsValid, string Status, string? ComputedSha256 = null);
public interface IArtifactIntegrityVerifier { ArtifactVerificationResult Verify(ArtifactVerificationRequest request); }
