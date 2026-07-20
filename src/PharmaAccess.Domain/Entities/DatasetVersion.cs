using PharmaAccess.Domain.Common;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Entities;

public sealed class DatasetVersion
{
    private DatasetVersion() { }

    public DatasetVersion(DatasetVersionCode versionCode, string schemaVersion, int totalSourceFiles, DateTime createdAtUtc, string? description = null, string? featureVersion = null, string? codeCommitHash = null, string? notes = null)
    {
        if (totalSourceFiles < 0) throw new ArgumentOutOfRangeException(nameof(totalSourceFiles));
        VersionCode = versionCode;
        SchemaVersion = DomainGuard.RequiredText(schemaVersion, 64, nameof(schemaVersion));
        TotalSourceFiles = totalSourceFiles;
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        Description = DomainGuard.OptionalText(description, 1024, nameof(description));
        FeatureVersion = DomainGuard.OptionalText(featureVersion, 64, nameof(featureVersion));
        CodeCommitHash = DomainGuard.OptionalText(codeCommitHash, 64, nameof(codeCommitHash));
        Notes = DomainGuard.OptionalText(notes, 4000, nameof(notes));
        Status = DatasetVersionStatus.Draft;
        ValidationStatus = DatasetValidationStatus.NotValidated;
    }

    public int DatasetVersionId { get; private set; }
    public DatasetVersionCode VersionCode { get; private set; }
    public string? Description { get; private set; }
    public string SchemaVersion { get; private set; } = null!;
    public string? FeatureVersion { get; private set; }
    public DatasetVersionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? FinalizedAtUtc { get; private set; }
    public string? CodeCommitHash { get; private set; }
    public int TotalSourceFiles { get; private set; }
    public long? TotalRows { get; private set; }
    public DatasetValidationStatus ValidationStatus { get; private set; }
    public string? Notes { get; private set; }

    public void MarkValidating()
    {
        RequireStatus(DatasetVersionStatus.Draft);
        Status = DatasetVersionStatus.Validating;
        ValidationStatus = DatasetValidationStatus.InProgress;
    }

    public void MarkValidated(long? totalRows = null)
    {
        RequireStatus(DatasetVersionStatus.Validating);
        if (totalRows < 0) throw new ArgumentOutOfRangeException(nameof(totalRows));
        TotalRows = totalRows;
        Status = DatasetVersionStatus.Validated;
        ValidationStatus = DatasetValidationStatus.Passed;
    }

    public void MarkRejected()
    {
        if (Status is not (DatasetVersionStatus.Draft or DatasetVersionStatus.Validating)) throw new InvalidOperationException("Only draft or validating datasets can be rejected.");
        Status = DatasetVersionStatus.Rejected;
        ValidationStatus = DatasetValidationStatus.Failed;
    }

    public void FinalizeVersion(DateTime finalizedAtUtc)
    {
        RequireStatus(DatasetVersionStatus.Validated);
        var value = DomainGuard.Utc(finalizedAtUtc, nameof(finalizedAtUtc));
        if (value < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(finalizedAtUtc));
        Status = DatasetVersionStatus.Finalized;
        FinalizedAtUtc = value;
    }

    public void Archive()
    {
        if (Status != DatasetVersionStatus.Finalized) throw new InvalidOperationException("Only finalized datasets can be archived.");
        Status = DatasetVersionStatus.Archived;
    }

    private void RequireStatus(DatasetVersionStatus expected)
    {
        if (Status != expected) throw new InvalidOperationException($"Transition requires {expected} status; current status is {Status}.");
    }
}
