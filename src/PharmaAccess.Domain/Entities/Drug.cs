using PharmaAccess.Domain.Common;

namespace PharmaAccess.Domain.Entities;

public sealed class Drug
{
    private Drug() { }

    public Drug(string normalizedIngredient, DateTime createdAtUtc, string? ingredientCombination = null, string? dosageForm = null, string? route = null, string? strength = null, string? rxNormId = null, string? therapeuticClass = null)
    {
        NormalizedIngredient = DomainGuard.RequiredText(normalizedIngredient, 256, nameof(normalizedIngredient));
        IngredientCombination = DomainGuard.OptionalText(ingredientCombination, 512, nameof(ingredientCombination));
        DosageForm = DomainGuard.OptionalText(dosageForm, 128, nameof(dosageForm));
        Route = DomainGuard.OptionalText(route, 128, nameof(route));
        Strength = DomainGuard.OptionalText(strength, 128, nameof(strength));
        RxNormId = DomainGuard.OptionalText(rxNormId, 64, nameof(rxNormId));
        TherapeuticClass = DomainGuard.OptionalText(therapeuticClass, 256, nameof(therapeuticClass));
        CreatedAtUtc = DomainGuard.Utc(createdAtUtc, nameof(createdAtUtc));
        UpdatedAtUtc = CreatedAtUtc;
    }

    public int DrugId { get; private set; }
    public string NormalizedIngredient { get; private set; } = null!;
    public string? IngredientCombination { get; private set; }
    public string? DosageForm { get; private set; }
    public string? Route { get; private set; }
    public string? Strength { get; private set; }
    public string? RxNormId { get; private set; }
    public string? TherapeuticClass { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void MarkUpdated(DateTime updatedAtUtc)
    {
        var value = DomainGuard.Utc(updatedAtUtc, nameof(updatedAtUtc));
        if (value < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(updatedAtUtc));
        UpdatedAtUtc = value;
    }
}
