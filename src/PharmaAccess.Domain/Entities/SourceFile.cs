using PharmaAccess.Domain.Common;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Entities;

public sealed class SourceFile
{
    private SourceFile() { }

    public SourceFile(int datasetVersionId, SourceType sourceType, string originalFileName, string sha256, long byteSize, string schemaVersion, DateTime importedAtUtc, SourceFileImportStatus importStatus = SourceFileImportStatus.Registered, string? sourceUrl = null, DateTime? retrievedAtUtc = null, string? reportingPeriod = null, long? rowCount = null, long? rejectedRowCount = null, string? licenseNote = null, string? errorDetails = null)
    {
        if (datasetVersionId <= 0) throw new ArgumentOutOfRangeException(nameof(datasetVersionId));
        if (byteSize < 0) throw new ArgumentOutOfRangeException(nameof(byteSize));
        if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
        if (rejectedRowCount < 0) throw new ArgumentOutOfRangeException(nameof(rejectedRowCount));
        if (rowCount.HasValue && rejectedRowCount > rowCount) throw new ArgumentException("Rejected rows cannot exceed total rows.");

        DatasetVersionId = datasetVersionId;
        SourceType = sourceType;
        OriginalFileName = DomainGuard.RequiredText(originalFileName, 512, nameof(originalFileName));
        SourceUrl = DomainGuard.OptionalText(sourceUrl, 2048, nameof(sourceUrl));
        RetrievedAtUtc = retrievedAtUtc.HasValue ? DomainGuard.Utc(retrievedAtUtc.Value, nameof(retrievedAtUtc)) : null;
        ImportedAtUtc = DomainGuard.Utc(importedAtUtc, nameof(importedAtUtc));
        ReportingPeriod = DomainGuard.OptionalText(reportingPeriod, 64, nameof(reportingPeriod));
        Sha256 = ValidateSha256(sha256);
        ByteSize = byteSize;
        RowCount = rowCount;
        RejectedRowCount = rejectedRowCount;
        SchemaVersion = DomainGuard.RequiredText(schemaVersion, 64, nameof(schemaVersion));
        LicenseNote = DomainGuard.OptionalText(licenseNote, 1024, nameof(licenseNote));
        ImportStatus = importStatus;
        ErrorDetails = DomainGuard.OptionalText(errorDetails, 4000, nameof(errorDetails));
    }

    public int SourceFileId { get; private set; }
    public int DatasetVersionId { get; private set; }
    public SourceType SourceType { get; private set; }
    public string OriginalFileName { get; private set; } = null!;
    public string? SourceUrl { get; private set; }
    public DateTime? RetrievedAtUtc { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
    public string? ReportingPeriod { get; private set; }
    public string Sha256 { get; private set; } = null!;
    public long ByteSize { get; private set; }
    public long? RowCount { get; private set; }
    public long? RejectedRowCount { get; private set; }
    public string SchemaVersion { get; private set; } = null!;
    public string? LicenseNote { get; private set; }
    public SourceFileImportStatus ImportStatus { get; private set; }
    public string? ErrorDetails { get; private set; }

    private static string ValidateSha256(string value)
    {
        var normalized = DomainGuard.RequiredText(value, 64, nameof(value)).ToUpperInvariant();
        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character))) throw new ArgumentException("SHA-256 must be 64 hexadecimal characters.", nameof(value));
        return normalized;
    }
}
