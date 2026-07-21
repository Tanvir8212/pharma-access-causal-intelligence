using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PharmaAccess.Domain.Causal;

public enum CausalStudyStatus { Draft, Validating, Ready, Estimating, Estimated, DiagnosticsFailed, Rejected, Finalized, Archived }
public enum AssumptionStatus { Unacknowledged, Acknowledged, Violated }
public enum EstimandType { AverageTreatmentEffect, AverageTreatmentEffectOnTreated }
public enum EffectScale { RiskDifference, RiskRatio, OddsRatio }
public enum DefinitionStatus { Draft, Validated, Rejected, Archived }
public enum DagNodeRole { Treatment, Outcome, Confounder, Mediator, Collider, InstrumentCandidate, Unobserved }
public enum ExposureType { HighNeighborAdoptionExposure, HighRegionAdoptionExposure, HighSimilarStateAdoptionExposure, EarlyLargeMarketExposure }
public enum MissingDataPolicy { CompleteCase, TrainingFittedImputationWithIndicators }
public enum DiagnosticStatus { Acceptable, Warning, Failed, InsufficientData }
public enum DiagnosticSeverity { Information, Warning, Blocking }
public enum CausalEstimateStatus { DescriptiveOnly, Estimated, DiagnosticsFailed, InsufficientData }
public enum SimulationSupportStatus { Supported, WeaklySupported, Extrapolative, Unsupported }
public enum ScenarioStatus { Draft, Validated, Evaluated, Rejected, Archived }

public sealed class CausalStudy
{
    private CausalStudy() { }
    public CausalStudy(string code, string name, string question, int datasetVersionId, int featureSetVersionId,
        string dagVersion, string adjustmentVersion, string treatmentVersion, string outcomeVersion,
        EstimandType estimand, string targetPopulation, int startQuarterId, int endQuarterId, DateTime createdAtUtc)
    {
        StudyCode = Required(code, 64); StudyName = Required(name, 256); ResearchQuestion = Required(question, 2000);
        if (datasetVersionId <= 0 || featureSetVersionId <= 0 || startQuarterId <= 0 || endQuarterId < startQuarterId) throw new ArgumentOutOfRangeException(nameof(datasetVersionId));
        DatasetVersionId = datasetVersionId; FeatureSetVersionId = featureSetVersionId; DagVersion = Required(dagVersion, 64); AdjustmentSetVersion = Required(adjustmentVersion, 64); TreatmentDefinitionVersion = Required(treatmentVersion, 64); OutcomeDefinitionVersion = Required(outcomeVersion, 64); Estimand = estimand; TargetPopulation = Required(targetPopulation, 1000); ObservationStartQuarterId = startQuarterId; ObservationEndQuarterId = endQuarterId; CreatedAtUtc = Utc(createdAtUtc); Status = CausalStudyStatus.Draft; AssumptionStatus = AssumptionStatus.Unacknowledged;
    }
    public long CausalStudyId { get; private set; } public string StudyCode { get; private set; } = null!; public string StudyName { get; private set; } = null!; public string ResearchQuestion { get; private set; } = null!; public int DatasetVersionId { get; private set; } public int FeatureSetVersionId { get; private set; } public string DagVersion { get; private set; } = null!; public string AdjustmentSetVersion { get; private set; } = null!; public string TreatmentDefinitionVersion { get; private set; } = null!; public string OutcomeDefinitionVersion { get; private set; } = null!; public EstimandType Estimand { get; private set; } public string TargetPopulation { get; private set; } = null!; public int ObservationStartQuarterId { get; private set; } public int ObservationEndQuarterId { get; private set; } public CausalStudyStatus Status { get; private set; } public AssumptionStatus AssumptionStatus { get; private set; } public DateTime CreatedAtUtc { get; private set; } public DateTime? FinalizedAtUtc { get; private set; } public string? CodeCommitHash { get; private set; } public string? Notes { get; private set; }
    public void MarkValidating() => Transition(CausalStudyStatus.Draft, CausalStudyStatus.Validating);
    public void AcknowledgeAssumptions() { EnsureMutable(); AssumptionStatus = AssumptionStatus.Acknowledged; }
    public void MarkReady() { if (AssumptionStatus != AssumptionStatus.Acknowledged) throw new InvalidOperationException("Identification assumptions must be acknowledged."); Transition(CausalStudyStatus.Validating, CausalStudyStatus.Ready); }
    public void MarkEstimating() => Transition(CausalStudyStatus.Ready, CausalStudyStatus.Estimating);
    public void MarkEstimated() => Transition(CausalStudyStatus.Estimating, CausalStudyStatus.Estimated);
    public void MarkDiagnosticsFailed() { EnsureMutable(); Status = CausalStudyStatus.DiagnosticsFailed; }
    public void Reject() { EnsureMutable(); Status = CausalStudyStatus.Rejected; }
    public void FinalizeStudy(DateTime atUtc) { if (Status != CausalStudyStatus.Estimated || AssumptionStatus != AssumptionStatus.Acknowledged) throw new InvalidOperationException("Only an estimated study with acknowledged assumptions can be finalized."); FinalizedAtUtc = Utc(atUtc); if (FinalizedAtUtc < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(atUtc)); Status = CausalStudyStatus.Finalized; }
    public void Archive() => Transition(CausalStudyStatus.Finalized, CausalStudyStatus.Archived);
    private void Transition(CausalStudyStatus from, CausalStudyStatus to) { EnsureMutable(); if (Status != from) throw new InvalidOperationException($"Transition requires {from}; current status is {Status}."); Status = to; }
    private void EnsureMutable() { if (Status is CausalStudyStatus.Finalized or CausalStudyStatus.Archived) throw new InvalidOperationException("Finalized causal studies are immutable."); }
    private static string Required(string value, int max) { if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required."); value = value.Trim(); if (value.Length > max) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.");
}

public sealed record DagNode(string Name, DagNodeRole Role, string Note);
public sealed record DagEdge(string From, string To, string Note);
public sealed record CausalDag(string Version, IReadOnlyList<DagNode> Nodes, IReadOnlyList<DagEdge> Edges)
{
    public void Validate(IReadOnlyCollection<string> adjustmentSet)
    {
        var treatment = Nodes.SingleOrDefault(x => x.Role == DagNodeRole.Treatment) ?? throw new InvalidOperationException("DAG requires one treatment.");
        _ = Nodes.SingleOrDefault(x => x.Role == DagNodeRole.Outcome) ?? throw new InvalidOperationException("DAG requires one outcome.");
        if (Nodes.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != Nodes.Count) throw new InvalidOperationException("DAG node names must be unique.");
        if (Edges.Any(x => Nodes.All(n => n.Name != x.From) || Nodes.All(n => n.Name != x.To))) throw new InvalidOperationException("DAG edge references an unknown node.");
        var descendants = Descendants(treatment.Name); if (adjustmentSet.Any(descendants.Contains)) throw new InvalidOperationException("Adjustment set contains a declared descendant of treatment.");
        if (adjustmentSet.Any(x => Nodes.SingleOrDefault(n => n.Name == x)?.Role is DagNodeRole.Mediator or DagNodeRole.Collider)) throw new InvalidOperationException("Adjustment set contains a declared mediator or collider.");
    }
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    public string ToDot() => "digraph CausalDag {\n" + string.Join('\n', Nodes.Select(x => $"  \"{x.Name}\" [label=\"{x.Name}\\n{x.Role}\"];").Concat(Edges.Select(x => $"  \"{x.From}\" -> \"{x.To}\";"))) + "\n}";
    public string Hash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ToJson())));
    private HashSet<string> Descendants(string start) { var result = new HashSet<string>(StringComparer.Ordinal); var queue = new Queue<string>(); queue.Enqueue(start); while (queue.Count > 0) { var current = queue.Dequeue(); foreach (var next in Edges.Where(x => x.From == current).Select(x => x.To)) if (result.Add(next)) queue.Enqueue(next); } return result; }
}

public sealed record AdjustmentVariable(string Name, string AvailabilityRule, DagNodeRole Role, string InclusionReason, bool IsLeakageSafe, string MissingValuePolicy, string Transformation);
public sealed record CausalAdjustmentDefinition(string Version, IReadOnlyList<AdjustmentVariable> Variables)
{
    public void Validate(CausalDag dag) { if (Variables.Count == 0 || Variables.Select(x => x.Name).Distinct().Count() != Variables.Count) throw new InvalidOperationException("Adjustment variables must be nonempty and unique."); if (Variables.Any(x => !x.IsLeakageSafe || x.Role != DagNodeRole.Confounder)) throw new InvalidOperationException("Only leakage-safe declared confounders may be adjusted for."); dag.Validate(Variables.Select(x => x.Name).ToArray()); }
}

public sealed record TreatmentDefinition(string Version, ExposureType ExposureType, double Threshold, int MinimumPeerCount, int LagQuarters, string ReferenceVersion, string InclusionPolicy)
{
    public TreatmentAssignment Assign(double continuousExposure, int eligiblePeers, int exposureQuarterId, int outcomeQuarterId)
    {
        if (Threshold is < 0 or > 1 || MinimumPeerCount < 1 || LagQuarters < 0) throw new InvalidOperationException("Treatment configuration is invalid.");
        if (double.IsNaN(continuousExposure) || continuousExposure is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(continuousExposure));
        if (eligiblePeers < MinimumPeerCount) return new(continuousExposure, false, false, "Insufficient eligible peers.", ["Treatment is unsupported for this row."]);
        if (exposureQuarterId + LagQuarters >= outcomeQuarterId) throw new InvalidOperationException("Treatment must be measured before the outcome period.");
        return new(continuousExposure, continuousExposure >= Threshold, true, $"{ExposureType}={continuousExposure:G6}; threshold={Threshold:G6}; peers={eligiblePeers}.", []);
    }
}
public sealed record TreatmentAssignment(double ContinuousValue, bool BinaryValue, bool IsSupported, string Reason, IReadOnlyList<string> Warnings);
public sealed record TemporalOrderFinding(string Code, DiagnosticSeverity Severity, string Description);
public static class TemporalOrderPolicy
{
    public static IReadOnlyList<TemporalOrderFinding> Validate(int baselineQuarterId, int treatmentQuarterId, int observationQuarterId, int outcomeQuarterId, bool usesFutureAdoption)
    {
        var findings = new List<TemporalOrderFinding>();
        if (baselineQuarterId > treatmentQuarterId) findings.Add(new("PostTreatmentConfounder", DiagnosticSeverity.Blocking, "A baseline confounder was measured after treatment."));
        if (treatmentQuarterId > observationQuarterId || treatmentQuarterId >= outcomeQuarterId) findings.Add(new("TreatmentAfterOutcome", DiagnosticSeverity.Blocking, "Treatment does not precede the outcome."));
        if (outcomeQuarterId != observationQuarterId + 1) findings.Add(new("IncompleteOutcomeWindow", DiagnosticSeverity.Blocking, "Outcome is not the next observable quarter."));
        if (usesFutureAdoption) findings.Add(new("FutureTreatmentInformation", DiagnosticSeverity.Blocking, "Treatment uses future adoption information."));
        return findings;
    }
}

public sealed record CounterfactualIntervention(double BaselineTreatment, double InterventionTreatment)
{
    public SimulationSupportStatus Classify(double observedMinimum, double observedMaximum, double weakMargin = .1)
    {
        if (BaselineTreatment is < 0 or > 1 || InterventionTreatment is < 0 or > 1) return SimulationSupportStatus.Unsupported;
        if (InterventionTreatment < observedMinimum || InterventionTreatment > observedMaximum) return SimulationSupportStatus.Extrapolative;
        return Math.Min(InterventionTreatment - observedMinimum, observedMaximum - InterventionTreatment) < weakMargin ? SimulationSupportStatus.WeaklySupported : SimulationSupportStatus.Supported;
    }
}
