using System.Security.Cryptography;
using System.Text;

namespace PharmaAccess.Domain.Research;

public enum RealDatasetStatus { Draft, Importing, Normalizing, MappingReview, Validating, Validated, Rejected, Finalized, Archived }
public enum MappingReviewStatus { AutomaticallyResolved, ManuallyResolved, Ambiguous, Conflicting, Unmapped, Excluded, Superseded }

public sealed class RealDatasetExecution
{
    public RealDatasetExecution(string versionCode, string protocolId, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(versionCode) || string.IsNullOrWhiteSpace(protocolId)) throw new ArgumentException("Dataset version and protocol are required.");
        if (createdAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("UTC is required.");
        VersionCode = versionCode.Trim(); ProtocolId = protocolId.Trim(); CreatedAtUtc = createdAtUtc;
    }
    public string VersionCode { get; }
    public string ProtocolId { get; }
    public bool IsSynthetic => false;
    public RealDatasetStatus Status { get; private set; } = RealDatasetStatus.Draft;
    public DateTime CreatedAtUtc { get; }
    public DateTime? FinalizedAtUtc { get; private set; }
    public string? ReconciliationHash { get; private set; }
    public int BlockingMappings { get; private set; }

    public void BeginImport() => Move(RealDatasetStatus.Draft, RealDatasetStatus.Importing);
    public void BeginNormalization() => Move(RealDatasetStatus.Importing, RealDatasetStatus.Normalizing);
    public void BeginMappingReview(int blockingMappings) { if (blockingMappings < 0) throw new ArgumentOutOfRangeException(nameof(blockingMappings)); Move(RealDatasetStatus.Normalizing, RealDatasetStatus.MappingReview); BlockingMappings = blockingMappings; }
    public void BeginValidation(string reconciliationHash)
    {
        if (BlockingMappings != 0) throw new InvalidOperationException("Unresolved blocking mappings prevent validation.");
        if (!IsSha(reconciliationHash)) throw new ArgumentException("A reconciliation SHA-256 is required.");
        Move(RealDatasetStatus.MappingReview, RealDatasetStatus.Validating); ReconciliationHash = reconciliationHash;
    }
    public void MarkValidated(bool reconciliationPassed) { if (!reconciliationPassed || ReconciliationHash is null) throw new InvalidOperationException("Successful reconciliation is required."); Move(RealDatasetStatus.Validating, RealDatasetStatus.Validated); }
    public void Finalize(string actor, DateTime atUtc)
    {
        if (string.IsNullOrWhiteSpace(actor) || atUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Finalization requires actor and UTC time.");
        Move(RealDatasetStatus.Validated, RealDatasetStatus.Finalized); FinalizedAtUtc = atUtc;
    }
    public void Reject() { if (Status is RealDatasetStatus.Finalized or RealDatasetStatus.Archived) throw new InvalidOperationException("A finalized dataset cannot be rejected."); Status = RealDatasetStatus.Rejected; }
    private void Move(RealDatasetStatus expected, RealDatasetStatus next) { if (Status != expected) throw new InvalidOperationException($"Expected {expected}, found {Status}."); Status = next; }
    private static bool IsSha(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
}

public sealed class DrugMappingReview
{
    public DrugMappingReview(string sourceHash, long sourceRowNumber, string sourceValue, string normalizedValue, string mappingVersion)
    {
        if (sourceHash.Length != 64 || !sourceHash.All(Uri.IsHexDigit) || sourceRowNumber <= 0 || string.IsNullOrWhiteSpace(sourceValue) || string.IsNullOrWhiteSpace(mappingVersion)) throw new ArgumentException("Complete mapping lineage is required.");
        SourceHash = sourceHash; SourceRowNumber = sourceRowNumber; SourceValue = sourceValue; NormalizedValue = normalizedValue; MappingVersion = mappingVersion;
    }
    public string SourceHash { get; }
    public long SourceRowNumber { get; }
    public string SourceValue { get; }
    public string NormalizedValue { get; }
    public string MappingVersion { get; }
    public MappingReviewStatus Status { get; private set; } = MappingReviewStatus.Unmapped;
    public int? TargetDrugId { get; private set; }
    public string? MappingMethod { get; private set; }
    public string? ConfidenceCategory { get; private set; }
    public string? Evidence { get; private set; }
    public string? Reviewer { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public void ResolveExact(int targetDrugId, string evidence) => Resolve(MappingReviewStatus.AutomaticallyResolved, targetDrugId, "ExactNormalizedMatch", "Exact", evidence, null, null, null);
    public void ResolveManually(int targetDrugId, string evidence, string reviewer, DateTime reviewedAtUtc, string notes) => Resolve(MappingReviewStatus.ManuallyResolved, targetDrugId, "ManualReview", "HumanReviewed", evidence, reviewer, reviewedAtUtc, notes);
    public void MarkAmbiguous(string evidence) { Evidence = Required(evidence); Status = MappingReviewStatus.Ambiguous; }
    public void Exclude(string reviewer, DateTime atUtc, string notes) => Resolve(MappingReviewStatus.Excluded, 0, "ExplicitExclusion", "HumanReviewed", notes, reviewer, atUtc, notes);
    private void Resolve(MappingReviewStatus status, int target, string method, string confidence, string evidence, string? reviewer, DateTime? at, string? notes)
    {
        if (status == MappingReviewStatus.ManuallyResolved && (string.IsNullOrWhiteSpace(reviewer) || at?.Kind != DateTimeKind.Utc)) throw new ArgumentException("Manual review requires reviewer and UTC timestamp.");
        if (target < 0) throw new ArgumentOutOfRangeException(nameof(target)); Status = status; TargetDrugId = target == 0 ? null : target; MappingMethod = method; ConfidenceCategory = confidence; Evidence = Required(evidence); Reviewer = reviewer; ReviewedAtUtc = at; Notes = notes;
    }
    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.") : value.Trim();
}

public static class ReconciliationHasher
{
    public static string Hash(params long[] orderedCounts) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', orderedCounts))));
}
