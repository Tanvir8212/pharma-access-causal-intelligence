using PharmaAccess.Domain.Common;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Domain.Entities;

public sealed class DrugProduct
{
    private DrugProduct() { }

    public DrugProduct(int drugId, string originalNdc, string sourceSystem, DateTime createdAtUtc, string? normalizedNdc = null, string? productName = null, string? labeler = null, Percentage? mappingConfidence = null)
    {
        if (drugId <= 0) throw new ArgumentOutOfRangeException(nameof(drugId));
        DrugId = drugId;
        OriginalNdc = DomainGuard.RequiredText(originalNdc, 64, nameof(originalNdc));
        NormalizedNdc = DomainGuard.OptionalText(normalizedNdc, 32, nameof(normalizedNdc));
        ProductName = DomainGuard.OptionalText(productName, 512, nameof(productName));
        Labeler = DomainGuard.OptionalText(labeler, 256, nameof(labeler));
        SourceSystem = DomainGuard.RequiredText(sourceSystem, 128, nameof(sourceSystem));
        MappingConfidence = mappingConfidence;
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public int DrugProductId { get; private set; }
    public int DrugId { get; private set; }
    public string OriginalNdc { get; private set; } = null!;
    public string? NormalizedNdc { get; private set; }
    public string? ProductName { get; private set; }
    public string? Labeler { get; private set; }
    public string SourceSystem { get; private set; } = null!;
    public Percentage? MappingConfidence { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
