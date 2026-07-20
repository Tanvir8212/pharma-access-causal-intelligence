using PharmaAccess.Domain.Common;

namespace PharmaAccess.Domain.Entities;

public sealed class StateDrugUtilization
{
    private StateDrugUtilization() { }

    public StateDrugUtilization(int drugId, int? drugProductId, int stateId, int quarterId, long prescriptionCount, decimal reimbursementAmount, int sourceRowCount, bool isSuppressed, DataQualityStatus dataQualityStatus, int datasetVersionId, DateTime createdAtUtc)
    {
        if (drugId <= 0) throw new ArgumentOutOfRangeException(nameof(drugId));
        if (drugProductId <= 0) throw new ArgumentOutOfRangeException(nameof(drugProductId));
        if (stateId <= 0) throw new ArgumentOutOfRangeException(nameof(stateId));
        if (quarterId <= 0) throw new ArgumentOutOfRangeException(nameof(quarterId));
        if (prescriptionCount < 0) throw new ArgumentOutOfRangeException(nameof(prescriptionCount));
        if (sourceRowCount < 0) throw new ArgumentOutOfRangeException(nameof(sourceRowCount));
        if (datasetVersionId <= 0) throw new ArgumentOutOfRangeException(nameof(datasetVersionId));
        DrugId = drugId;
        DrugProductId = drugProductId;
        StateId = stateId;
        QuarterId = quarterId;
        PrescriptionCount = prescriptionCount;
        ReimbursementAmount = reimbursementAmount;
        SourceRowCount = sourceRowCount;
        IsSuppressed = isSuppressed;
        DataQualityStatus = dataQualityStatus;
        DatasetVersionId = datasetVersionId;
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public int StateDrugUtilizationId { get; private set; }
    public int DrugId { get; private set; }
    public int? DrugProductId { get; private set; }
    public int StateId { get; private set; }
    public int QuarterId { get; private set; }
    public long PrescriptionCount { get; private set; }
    public decimal ReimbursementAmount { get; private set; }
    public int SourceRowCount { get; private set; }
    public bool IsSuppressed { get; private set; }
    public DataQualityStatus DataQualityStatus { get; private set; }
    public int DatasetVersionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
