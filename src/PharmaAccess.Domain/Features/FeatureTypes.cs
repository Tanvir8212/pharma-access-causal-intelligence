using PharmaAccess.Domain.Common;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Features;

public enum FeatureSetStatus { Draft, Building, Validating, Validated, Rejected, Finalized, Archived }
public enum FeatureValidationStatus { NotValidated, InProgress, Passed, Failed }
public enum FeatureDataType { Boolean, Integer, Long, Decimal, Double, Text }
public enum FeatureCategory { Identifier, Timing, Drug, State, Utilization, Historical, Regional, Geographic, Quality, Label }
public enum LeakageRisk { None, Low, Medium, High, Prohibited }
public enum LaunchReferenceType { FdaFirstGenericApprovalProxy, OtherDocumentedProxy }
public enum LeakageSeverity { Information, Warning, Blocking }

public sealed class FeatureSetVersion
{
    private FeatureSetVersion() { }

    public FeatureSetVersion(string versionCode, int datasetVersionId, string definitionHash, DateTime createdAtUtc, string? description = null, string? codeCommitHash = null, string? notes = null)
    {
        if (datasetVersionId <= 0) throw new ArgumentOutOfRangeException(nameof(datasetVersionId));
        VersionCode = DomainGuard.RequiredText(versionCode, 64, nameof(versionCode));
        DatasetVersionId = datasetVersionId;
        DefinitionHash = ValidateHash(definitionHash);
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        Description = DomainGuard.OptionalText(description, 1024, nameof(description));
        CodeCommitHash = DomainGuard.OptionalText(codeCommitHash, 64, nameof(codeCommitHash));
        Notes = DomainGuard.OptionalText(notes, 4000, nameof(notes));
        Status = FeatureSetStatus.Draft;
        ValidationStatus = FeatureValidationStatus.NotValidated;
    }

    public int FeatureSetVersionId { get; private set; }
    public string VersionCode { get; private set; } = null!;
    public string? Description { get; private set; }
    public int DatasetVersionId { get; private set; }
    public FeatureSetStatus Status { get; private set; }
    public string DefinitionHash { get; private set; } = null!;
    public string? CodeCommitHash { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? FinalizedAtUtc { get; private set; }
    public FeatureValidationStatus ValidationStatus { get; private set; }
    public string? Notes { get; private set; }

    public void MarkBuilding() => Transition(FeatureSetStatus.Draft, FeatureSetStatus.Building);
    public void MarkValidating() { Transition(FeatureSetStatus.Building, FeatureSetStatus.Validating); ValidationStatus = FeatureValidationStatus.InProgress; }
    public void MarkValidated() { Transition(FeatureSetStatus.Validating, FeatureSetStatus.Validated); ValidationStatus = FeatureValidationStatus.Passed; }
    public void MarkRejected() { if (Status is FeatureSetStatus.Finalized or FeatureSetStatus.Archived) throw new InvalidOperationException("Finalized feature lineage is immutable."); Status = FeatureSetStatus.Rejected; ValidationStatus = FeatureValidationStatus.Failed; }
    public void FinalizeVersion(DateTime atUtc) { Transition(FeatureSetStatus.Validated, FeatureSetStatus.Finalized); var value = DomainGuard.Utc(atUtc, nameof(atUtc)); if (value < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(atUtc)); FinalizedAtUtc = value; }
    public void Archive() => Transition(FeatureSetStatus.Finalized, FeatureSetStatus.Archived);

    private void Transition(FeatureSetStatus from, FeatureSetStatus to) { if (Status != from) throw new InvalidOperationException($"Transition requires {from}; current status is {Status}."); Status = to; }
    private static string ValidateHash(string value) { var hash = DomainGuard.RequiredText(value, 64, nameof(value)).ToUpperInvariant(); if (hash.Length != 64 || hash.Any(c => !Uri.IsHexDigit(c))) throw new ArgumentException("Definition hash must be a SHA-256 hexadecimal value.", nameof(value)); return hash; }
}

public sealed class GenericLaunch
{
    private GenericLaunch() { }
    public GenericLaunch(int drugId, int primaryApprovalId, DateOnly approvalDate, int approvalQuarterId, int observationStartQuarterId, LaunchReferenceType launchReferenceType, DateTime createdAtUtc, int? observationEndQuarterId = null, bool isEligibleForAnalysis = true, string? exclusionReason = null)
    {
        if (drugId <= 0 || primaryApprovalId <= 0 || approvalQuarterId <= 0 || observationStartQuarterId <= 0) throw new ArgumentOutOfRangeException(nameof(drugId));
        if (approvalDate.Year < 1900) throw new ArgumentOutOfRangeException(nameof(approvalDate));
        if (observationEndQuarterId.HasValue && observationEndQuarterId < observationStartQuarterId) throw new ArgumentOutOfRangeException(nameof(observationEndQuarterId));
        DrugId = drugId; PrimaryApprovalId = primaryApprovalId; ApprovalDate = approvalDate; ApprovalQuarterId = approvalQuarterId; ObservationStartQuarterId = observationStartQuarterId; ObservationEndQuarterId = observationEndQuarterId; LaunchReferenceType = launchReferenceType; IsEligibleForAnalysis = isEligibleForAnalysis; ExclusionReason = DomainGuard.OptionalText(exclusionReason, 512, nameof(exclusionReason)); CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
    }
    public int GenericLaunchId { get; private set; }
    public int DrugId { get; private set; }
    public int PrimaryApprovalId { get; private set; }
    public DateOnly ApprovalDate { get; private set; }
    public int ApprovalQuarterId { get; private set; }
    public LaunchReferenceType LaunchReferenceType { get; private set; }
    public int ObservationStartQuarterId { get; private set; }
    public int? ObservationEndQuarterId { get; private set; }
    public bool IsEligibleForAnalysis { get; private set; }
    public string? ExclusionReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

public sealed class FeatureDefinition
{
    private FeatureDefinition() { }
    public int FeatureDefinitionId { get; private set; }
    public int FeatureSetVersionId { get; private set; }
    public string FeatureName { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public FeatureDataType DataType { get; private set; }
    public string Source { get; private set; } = null!;
    public string Formula { get; private set; } = null!;
    public string AvailableAsOfRule { get; private set; } = null!;
    public string MissingValuePolicy { get; private set; } = null!;
    public decimal? ValidMinimum { get; private set; }
    public decimal? ValidMaximum { get; private set; }
    public LeakageRisk LeakageRisk { get; private set; }
    public FeatureCategory FeatureCategory { get; private set; }
    public bool IsModelInput { get; private set; }
    public bool IsLabel { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}

public readonly record struct StateEntryObservation(long? PrescriptionCount, decimal? ReimbursementAmount, bool IsSuppressed);
public sealed record StateEntryResult(bool IsPresent, bool IsFirstEntry, string PolicyName, string Thresholds, string Reason, IReadOnlyList<string> Warnings);
public sealed record GrowthResult(decimal? Value, string? UndefinedReason);
public sealed record ConcentrationResult(decimal? ConcentrationIndex, decimal? TopStateShare, decimal? TopFiveStateShare, string? UndefinedReason);
public sealed record LeakageFinding(string Code, LeakageSeverity Severity, string? FeatureName, string RowIdentifier, string Description, string Recommendation);

public interface IStateEntryPolicy
{
    string Name { get; }
    StateEntryResult Evaluate(StateEntryObservation observation, bool previouslyPresent, int consecutivePositiveQuarters = 1);
}

public sealed class AnyPositiveUtilizationPolicy : IStateEntryPolicy
{
    public string Name => "AnyPositiveUtilization";
    public StateEntryResult Evaluate(StateEntryObservation value, bool previous, int consecutivePositiveQuarters = 1)
    {
        var present = !value.IsSuppressed && value.PrescriptionCount > 0;
        return new(present, present && !previous, Name, "PrescriptionCount > 0", value.IsSuppressed ? "Suppressed observation is not classified as present." : present ? "Positive utilization observed." : "No positive utilization observed.", value.IsSuppressed ? ["Suppressed values remain unknown."] : []);
    }
}

public sealed class MinimumPrescriptionThresholdPolicy(long threshold) : IStateEntryPolicy
{
    public string Name => "MinimumPrescriptionThreshold";
    public StateEntryResult Evaluate(StateEntryObservation value, bool previous, int consecutivePositiveQuarters = 1) { if (threshold < 1) throw new InvalidOperationException("Threshold must be positive."); var present = !value.IsSuppressed && value.PrescriptionCount >= threshold; return new(present, present && !previous, Name, $"PrescriptionCount >= {threshold}", present ? "Threshold met." : "Threshold not met or observation unavailable.", value.IsSuppressed ? ["Suppressed values remain unknown."] : []); }
}

public sealed class PositivePrescriptionAndReimbursementPolicy : IStateEntryPolicy
{
    public string Name => "PositivePrescriptionAndReimbursement";
    public StateEntryResult Evaluate(StateEntryObservation value, bool previous, int consecutivePositiveQuarters = 1) { var present = !value.IsSuppressed && value.PrescriptionCount > 0 && value.ReimbursementAmount > 0; return new(present, present && !previous, Name, "PrescriptionCount > 0 and ReimbursementAmount > 0", present ? "Both measures are positive." : "One or more measures are unavailable or non-positive.", value.IsSuppressed ? ["Suppressed values remain unknown."] : []); }
}

public sealed class ConsecutiveQuarterConfirmationPolicy(int requiredQuarters) : IStateEntryPolicy
{
    public string Name => "ConsecutiveQuarterConfirmation";
    public StateEntryResult Evaluate(StateEntryObservation value, bool previous, int consecutivePositiveQuarters = 1) { if (requiredQuarters < 2) throw new InvalidOperationException("Consecutive confirmation requires at least two quarters."); var present = !value.IsSuppressed && value.PrescriptionCount > 0 && consecutivePositiveQuarters >= requiredQuarters; return new(present, present && !previous, Name, $"{requiredQuarters} consecutive positive quarters", present ? "Confirmation window met." : "Confirmation window not met.", value.IsSuppressed ? ["Suppressed values remain unknown."] : []); }
}

public static class FeatureCalculations
{
    public static GrowthResult OrdinaryGrowth(decimal? current, decimal? previous)
    {
        if (!current.HasValue || !previous.HasValue) return new(null, "Current or lag value is missing.");
        if (previous == 0) return new(null, "Lag denominator is zero.");
        return new((current.Value - previous.Value) / previous.Value, null);
    }

    public static ConcentrationResult Concentration(IEnumerable<decimal> utilization)
    {
        var values = utilization.ToArray();
        if (values.Any(x => x < 0)) throw new ArgumentOutOfRangeException(nameof(utilization));
        var total = values.Sum();
        if (total == 0) return new(null, null, null, "Total utilization is zero.");
        var shares = values.Select(x => x / total).OrderByDescending(x => x).ToArray();
        return new(shares.Sum(x => x * x), shares[0], shares.Take(5).Sum(), null);
    }

    public static bool PersistentInequality(IEnumerable<decimal> gaps, decimal threshold, int requiredConsecutiveQuarters)
    {
        if (threshold is < -100 or > 100 || requiredConsecutiveQuarters <= 0) throw new ArgumentOutOfRangeException();
        var run = 0;
        foreach (var gap in gaps) { run = gap <= threshold ? run + 1 : 0; if (run >= requiredConsecutiveQuarters) return true; }
        return false;
    }

    public static IReadOnlyDictionary<int, decimal> FrozenMarketWeights(IEnumerable<(int StateId, decimal Volume, CalendarQuarter Quarter)> observations, CalendarQuarter windowStart, CalendarQuarter windowEnd)
    {
        if (windowEnd < windowStart) throw new ArgumentException("Reference window is invalid.");
        var filtered = observations.Where(x => x.Quarter >= windowStart && x.Quarter <= windowEnd).ToArray();
        if (filtered.Any(x => x.Volume < 0)) throw new ArgumentOutOfRangeException(nameof(observations));
        var weights = filtered.GroupBy(x => x.StateId).ToDictionary(x => x.Key, x => x.Sum(y => y.Volume));
        if (weights.Count == 0 || weights.Values.Sum() == 0) throw new InvalidOperationException("Frozen market-weight total is zero.");
        return weights;
    }
}
