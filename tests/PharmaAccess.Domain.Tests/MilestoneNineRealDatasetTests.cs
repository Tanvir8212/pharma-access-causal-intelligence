using PharmaAccess.Domain.Research;
using Xunit;

namespace PharmaAccess.Domain.Tests;

public sealed class MilestoneNineRealDatasetTests
{
    [Fact] public void Real_dataset_requires_reconciliation_and_mapping_resolution()
    {
        var value = new RealDatasetExecution("real-v1", "protocol-real-v1", Utc); value.BeginImport(); value.BeginNormalization(); value.BeginMappingReview(1); Assert.False(value.IsSynthetic); Assert.Throws<InvalidOperationException>(() => value.BeginValidation(new string('A', 64)));
    }
    [Fact] public void Real_dataset_validation_and_finalization_are_explicit()
    {
        var value = new RealDatasetExecution("real-v1", "protocol-real-v1", Utc); value.BeginImport(); value.BeginNormalization(); value.BeginMappingReview(0); value.BeginValidation(new string('A', 64)); Assert.Throws<InvalidOperationException>(() => value.MarkValidated(false)); value.MarkValidated(true); Assert.Equal(RealDatasetStatus.Validated, value.Status); value.Finalize("human", Utc.AddMinutes(1)); Assert.Equal(RealDatasetStatus.Finalized, value.Status); Assert.Throws<InvalidOperationException>(value.Reject);
    }
    [Fact] public void Fuzzy_or_ambiguous_mapping_cannot_auto_resolve()
    {
        var mapping = new DrugMappingReview(new string('A', 64), 1, "ingredient combo", "ingredient+combo", "map-v1"); mapping.MarkAmbiguous("Multiple candidates"); Assert.Equal(MappingReviewStatus.Ambiguous, mapping.Status); Assert.Null(mapping.TargetDrugId); mapping.ResolveManually(7, "reviewed source authority", "reviewer", Utc, "combination preserved"); Assert.Equal(MappingReviewStatus.ManuallyResolved, mapping.Status);
    }
    private static readonly DateTime Utc = new(2026, 7, 21, 0, 0, 0, DateTimeKind.Utc);
}
