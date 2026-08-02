using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.Data.Entities;

public sealed class GovernanceDriftReport
{
    private GovernanceDriftReport() { }
    internal GovernanceDriftReport(DriftReport report) { DriftReportId = report.Id; ChampionVersion = report.ChampionVersion; EvaluationWindow = report.EvaluationWindow; CreatedAtUtc = report.CreatedAtUtc; Severity = report.Severity; LabelsAvailable = report.LabelsAvailable; SubgroupWarningsJson = System.Text.Json.JsonSerializer.Serialize(report.SubgroupWarnings); GovernanceNotice = report.GovernanceNotice; }
    public Guid DriftReportId { get; private set; } public string ChampionVersion { get; private set; } = null!; public string EvaluationWindow { get; private set; } = null!; public DateTime CreatedAtUtc { get; private set; } public DriftSeverity Severity { get; private set; } public bool LabelsAvailable { get; private set; } public string SubgroupWarningsJson { get; private set; } = "[]"; public string GovernanceNotice { get; private set; } = null!;
    public ICollection<GovernanceDriftFinding> Findings { get; private set; } = new List<GovernanceDriftFinding>();
}
public sealed class GovernanceDriftFinding
{
    private GovernanceDriftFinding() { }
    internal GovernanceDriftFinding(Guid reportId, DriftMeasure finding) { DriftFindingId = Guid.NewGuid(); DriftReportId = reportId; Scope = finding.Scope; Name = finding.Name; Statistic = finding.Statistic; ReferenceValue = finding.ReferenceValue; CurrentValue = finding.CurrentValue; Change = finding.Change; Severity = finding.Severity; Formula = finding.Formula; }
    public Guid DriftFindingId { get; private set; } public Guid DriftReportId { get; private set; } public string Scope { get; private set; } = null!; public string Name { get; private set; } = null!; public string Statistic { get; private set; } = null!; public double ReferenceValue { get; private set; } public double CurrentValue { get; private set; } public double Change { get; private set; } public DriftSeverity Severity { get; private set; } public string Formula { get; private set; } = null!;
}
public sealed class GovernanceComparison
{
    private GovernanceComparison() { }
    internal GovernanceComparison(ChampionChallengerComparison value) { ComparisonId=value.Id; ChampionVersion=value.Champion.Version; ChallengerVersion=value.Challenger.Version; ChampionArtifactPath=value.Champion.ArtifactPath; ChallengerArtifactPath=value.Challenger.ArtifactPath; ChampionArtifactSha256=value.Champion.ArtifactSha256; ChallengerArtifactSha256=value.Challenger.ArtifactSha256; FeatureSchemaHash=value.Champion.FeatureSchemaHash; EvaluationCohortHash=value.Champion.EvaluationCohortHash; DatasetHash=value.Champion.DatasetHash; DatasetFreezeIdentifier=value.Champion.DatasetFreezeIdentifier; ReproducibilityHash=value.Champion.ReproducibilityHash; ChampionJson=System.Text.Json.JsonSerializer.Serialize(value.Champion); ChallengerJson=System.Text.Json.JsonSerializer.Serialize(new { Snapshot=value.Challenger,value.SubmitterIdentifier }); MetricDifferencesJson=System.Text.Json.JsonSerializer.Serialize(value.MetricDifferences); SubgroupResultsJson=System.Text.Json.JsonSerializer.Serialize(value.SubgroupWarnings); BlockingReasonsJson=System.Text.Json.JsonSerializer.Serialize(value.BlockingReasons); PromotionEligible=value.PromotionEligible; CreatedAtUtc=value.CreatedAtUtc; Status="Pending"; }
    public Guid ComparisonId { get; private set; } public string ChampionVersion { get; private set; }=null!; public string ChallengerVersion { get; private set; }=null!; public string ChampionArtifactPath { get; private set; }=""; public string ChallengerArtifactPath { get; private set; }=""; public string ChampionArtifactSha256 { get; private set; }=null!; public string ChallengerArtifactSha256 { get; private set; }=null!; public string FeatureSchemaHash { get; private set; }=null!; public string EvaluationCohortHash { get; private set; }=null!; public string DatasetHash { get; private set; }=null!; public string DatasetFreezeIdentifier { get; private set; }=""; public string ReproducibilityHash { get; private set; }=null!; public string ChampionJson { get; private set; }=null!; public string ChallengerJson { get; private set; }=null!; public string MetricDifferencesJson { get; private set; }=null!; public string SubgroupResultsJson { get; private set; }=null!; public string BlockingReasonsJson { get; private set; }=null!; public bool PromotionEligible { get; private set; } public string Status { get; private set; }=null!; public DateTime CreatedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public byte[] RowVersion { get; private set; }=[];
    internal void Complete(string status, DateTime at) { if (CompletedAtUtc.HasValue) throw new InvalidOperationException("Comparison decision was already completed."); Status=status; CompletedAtUtc=at; }
}
public sealed class GovernanceChampionState
{
    private GovernanceChampionState() { }
    internal GovernanceChampionState(string version) { GovernanceChampionStateId=1; ChampionVersion=version; UpdatedAtUtc=DateTime.UtcNow; }
    public int GovernanceChampionStateId { get; private set; } public string ChampionVersion { get; private set; }=null!; public string? PreviousChampionVersion { get; private set; } public DateTime UpdatedAtUtc { get; private set; } public byte[] RowVersion { get; private set; }=[];
    internal void Promote(string version, DateTime at) { PreviousChampionVersion=ChampionVersion; ChampionVersion=version; UpdatedAtUtc=at; }
    internal void Rollback(DateTime at) { if (PreviousChampionVersion is null) throw new InvalidOperationException("No approved rollback target exists."); var target=PreviousChampionVersion; PreviousChampionVersion=ChampionVersion; ChampionVersion=target; UpdatedAtUtc=at; }
}
public sealed class GovernanceDecision
{
    private GovernanceDecision() { }
    internal GovernanceDecision(Guid comparisonId, PromotionDecision decision, string before, string after, string challenger, string approver, DateTime actionAt, string reason) { GovernanceDecisionId=Guid.NewGuid(); ComparisonId=comparisonId==Guid.Empty?null:comparisonId; Decision=decision; ChampionBefore=before; ChampionAfter=after; ChallengerVersion=challenger; ApproverIdentifier=approver; ActionTimestampUtc=actionAt; Reason=reason; RecordedAtUtc=DateTime.UtcNow; }
    public Guid GovernanceDecisionId { get; private set; } public Guid? ComparisonId { get; private set; } public PromotionDecision Decision { get; private set; } public string ChampionBefore { get; private set; }=null!; public string ChampionAfter { get; private set; }=null!; public string ChallengerVersion { get; private set; }=null!; public string ApproverIdentifier { get; private set; }=null!; public DateTime ActionTimestampUtc { get; private set; } public string Reason { get; private set; }=null!; public DateTime RecordedAtUtc { get; private set; } public byte[] RowVersion { get; private set; }=[];
}
public sealed class GovernanceChampionHistory
{
    private GovernanceChampionHistory() { }
    internal GovernanceChampionHistory(string version, string? previous, Guid decisionId, bool current, DateTime at) { ChampionHistoryId=Guid.NewGuid(); ModelVersion=version; PreviousChampionVersion=previous; GovernanceDecisionId=decisionId; IsCurrent=current; ApprovedAtUtc=at; }
    public Guid ChampionHistoryId { get; private set; } public string ModelVersion { get; private set; }=null!; public string? PreviousChampionVersion { get; private set; } public Guid GovernanceDecisionId { get; private set; } public bool IsCurrent { get; private set; } public DateTime ApprovedAtUtc { get; private set; } public DateTime? SupersededAtUtc { get; private set; }
    internal void Supersede(DateTime at) { IsCurrent=false; SupersededAtUtc=at; }
}
public sealed class GovernanceAuditRecord
{
    private GovernanceAuditRecord() { }
    internal GovernanceAuditRecord(Guid decisionId, string eventType, DateTime at, string payloadJson) { GovernanceAuditRecordId=Guid.NewGuid(); GovernanceDecisionId=decisionId; EventType=eventType; OccurredAtUtc=at; PayloadJson=payloadJson; IsCompleted=true; }
    public Guid GovernanceAuditRecordId { get; private set; } public Guid GovernanceDecisionId { get; private set; } public string EventType { get; private set; }=null!; public DateTime OccurredAtUtc { get; private set; } public string PayloadJson { get; private set; }=null!; public bool IsCompleted { get; private set; }
}
