using PharmaAccess.Domain.Common;

namespace PharmaAccess.Domain.Entities;

public sealed class FirstGenericApproval
{
    private FirstGenericApproval() { }

    public FirstGenericApproval(int drugId, DateOnly approvalDate, string approvalSource, bool isPrimaryLaunchReference, DateTime createdAtUtc, string? applicationNumber = null, string? applicant = null)
    {
        if (drugId <= 0) throw new ArgumentOutOfRangeException(nameof(drugId));
        if (approvalDate.Year < 1900) throw new ArgumentOutOfRangeException(nameof(approvalDate));
        DrugId = drugId;
        ApprovalDate = approvalDate;
        ApplicationNumber = DomainGuard.OptionalText(applicationNumber, 64, nameof(applicationNumber));
        Applicant = DomainGuard.OptionalText(applicant, 256, nameof(applicant));
        ApprovalSource = DomainGuard.RequiredText(approvalSource, 128, nameof(approvalSource));
        IsPrimaryLaunchReference = isPrimaryLaunchReference;
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public int ApprovalId { get; private set; }
    public int DrugId { get; private set; }
    public DateOnly ApprovalDate { get; private set; }
    public string? ApplicationNumber { get; private set; }
    public string? Applicant { get; private set; }
    public string ApprovalSource { get; private set; } = null!;
    public bool IsPrimaryLaunchReference { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
