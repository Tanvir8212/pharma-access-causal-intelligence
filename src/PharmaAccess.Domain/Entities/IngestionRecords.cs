namespace PharmaAccess.Domain.Entities;

// Raw records intentionally retain source text. Normalization belongs in the staging records.
public sealed class FdaFirstGenericApprovalRaw
{
    private FdaFirstGenericApprovalRaw() { }
    public long RawRecordId { get; private set; }
    public int SourceFileId { get; private set; }
    public long SourceRowNumber { get; private set; }
    public string? ApplicationNumberRaw { get; private set; }
    public string? ProductNumberRaw { get; private set; }
    public string? ActiveIngredientRaw { get; private set; }
    public string? DosageFormRaw { get; private set; }
    public string? StrengthRaw { get; private set; }
    public string? ApplicantRaw { get; private set; }
    public string? ApprovalDateRaw { get; private set; }
    public DateTime? ParsedApprovalDate { get; private set; }
    public RawParseStatus ParseStatus { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
}

public sealed class MedicaidStateDrugUtilizationRaw
{
    private MedicaidStateDrugUtilizationRaw() { }
    public long RawRecordId { get; private set; }
    public int SourceFileId { get; private set; }
    public long SourceRowNumber { get; private set; }
    public string? UtilizationTypeRaw { get; private set; }
    public string? StateCodeRaw { get; private set; }
    public string? NdcRaw { get; private set; }
    public string? ProductNameRaw { get; private set; }
    public string? PackageSizeRaw { get; private set; }
    public string? YearRaw { get; private set; }
    public string? QuarterRaw { get; private set; }
    public string? PrescriptionCountRaw { get; private set; }
    public string? ReimbursementAmountRaw { get; private set; }
    public int? ParsedYear { get; private set; }
    public int? ParsedQuarter { get; private set; }
    public long? ParsedPrescriptionCount { get; private set; }
    public decimal? ParsedReimbursementAmount { get; private set; }
    public RawParseStatus ParseStatus { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
}

public sealed class StateReferenceRaw
{
    private StateReferenceRaw() { }
    public long RawRecordId { get; private set; }
    public int SourceFileId { get; private set; }
    public long SourceRowNumber { get; private set; }
    public string? StateCodeRaw { get; private set; }
    public string? StateNameRaw { get; private set; }
    public string? RegionRaw { get; private set; }
    public string? DivisionRaw { get; private set; }
    public string? EligibilityRaw { get; private set; }
    public RawParseStatus ParseStatus { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime ImportedAtUtc { get; private set; }
}

public sealed class FdaFirstGenericApprovalNormalized
{
    private FdaFirstGenericApprovalNormalized() { }
    public long StagingId { get; private set; }
    public int SourceFileId { get; private set; }
    public long SourceRowNumber { get; private set; }
    public string NormalizedIngredient { get; private set; } = null!;
    public string? DosageForm { get; private set; }
    public string? Strength { get; private set; }
    public string? Applicant { get; private set; }
    public string? ApplicationNumber { get; private set; }
    public DateTime ApprovalDate { get; private set; }
    public StagingMappingStatus MappingStatus { get; private set; }
    public IngestionValidationStatus ValidationStatus { get; private set; }
    public string? ValidationMessagesJson { get; private set; }
}

public sealed class MedicaidStateDrugUtilizationNormalized
{
    private MedicaidStateDrugUtilizationNormalized() { }
    public long StagingId { get; private set; }
    public int SourceFileId { get; private set; }
    public long SourceRowNumber { get; private set; }
    public string StateCode { get; private set; } = null!;
    public string OriginalNdc { get; private set; } = null!;
    public string? NormalizedNdc { get; private set; }
    public string? ProductName { get; private set; }
    public int CalendarYear { get; private set; }
    public int QuarterNumber { get; private set; }
    public long PrescriptionCount { get; private set; }
    public decimal ReimbursementAmount { get; private set; }
    public bool IsSuppressed { get; private set; }
    public StagingMappingStatus MappingStatus { get; private set; }
    public IngestionValidationStatus ValidationStatus { get; private set; }
    public string? ValidationMessagesJson { get; private set; }
}

public sealed class StateReferenceNormalized
{
    private StateReferenceNormalized() { }
    public long StagingId { get; private set; }
    public int SourceFileId { get; private set; }
    public long SourceRowNumber { get; private set; }
    public string StateCode { get; private set; } = null!;
    public string StateName { get; private set; } = null!;
    public string? Region { get; private set; }
    public string? Division { get; private set; }
    public bool IsEligible { get; private set; }
    public IngestionValidationStatus ValidationStatus { get; private set; }
    public string? ValidationMessagesJson { get; private set; }
}
