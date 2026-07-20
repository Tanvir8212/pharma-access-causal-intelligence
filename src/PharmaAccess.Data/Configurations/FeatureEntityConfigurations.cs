using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;

namespace PharmaAccess.Data.Configurations;

internal static class FeatureMapping
{
    internal static void Restrict<TEntity, TPrincipal>(EntityTypeBuilder<TEntity> builder, System.Linq.Expressions.Expression<Func<TEntity, object?>> foreignKey) where TEntity : class where TPrincipal : class =>
        builder.HasOne<TPrincipal>().WithMany().HasForeignKey(foreignKey).OnDelete(DeleteBehavior.Restrict);
    internal static void Decimal<TEntity>(EntityTypeBuilder<TEntity> builder, System.Linq.Expressions.Expression<Func<TEntity, decimal>> property) where TEntity : class => builder.Property(property).HasPrecision(19, 6);
}

public sealed class GenericLaunchConfiguration : IEntityTypeConfiguration<GenericLaunch>
{
    public void Configure(EntityTypeBuilder<GenericLaunch> builder)
    {
        builder.ToTable("GenericLaunch", "core"); builder.HasKey(x => x.GenericLaunchId);
        builder.Property(x => x.ApprovalDate).HasColumnType("date");
        builder.Property(x => x.LaunchReferenceType).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExclusionReason).HasMaxLength(512);
        builder.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FirstGenericApproval>().WithMany().HasForeignKey(x => x.PrimaryApprovalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ApprovalQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ObservationStartQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ObservationEndQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PrimaryApprovalId).IsUnique();
        builder.HasIndex(x => new { x.DrugId, x.ApprovalQuarterId });
    }
}

public sealed class FeatureSetVersionConfiguration : IEntityTypeConfiguration<FeatureSetVersion>
{
    public void Configure(EntityTypeBuilder<FeatureSetVersion> builder)
    {
        builder.ToTable("FeatureSetVersion", "feature"); builder.HasKey(x => x.FeatureSetVersionId);
        builder.Property(x => x.VersionCode).HasMaxLength(64).IsRequired(); builder.HasIndex(x => x.VersionCode).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(1024); builder.Property(x => x.DefinitionHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.CodeCommitHash).HasMaxLength(64); builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); builder.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FeatureDefinitionConfiguration : IEntityTypeConfiguration<FeatureDefinition>
{
    public void Configure(EntityTypeBuilder<FeatureDefinition> builder)
    {
        builder.ToTable("FeatureDefinition", "feature"); builder.HasKey(x => x.FeatureDefinitionId);
        builder.Property(x => x.FeatureName).HasMaxLength(128).IsRequired(); builder.Property(x => x.Description).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.DataType).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.Source).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Formula).HasMaxLength(2000).IsRequired(); builder.Property(x => x.AvailableAsOfRule).HasMaxLength(1000).IsRequired(); builder.Property(x => x.MissingValuePolicy).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ValidMinimum).HasPrecision(19, 6); builder.Property(x => x.ValidMaximum).HasPrecision(19, 6);
        builder.Property(x => x.LeakageRisk).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.FeatureCategory).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<FeatureSetVersion>().WithMany().HasForeignKey(x => x.FeatureSetVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FeatureSetVersionId, x.FeatureName }).IsUnique();
    }
}

public sealed class DrugStateQuarterFeatureConfiguration : IEntityTypeConfiguration<DrugStateQuarterFeature>
{
    public void Configure(EntityTypeBuilder<DrugStateQuarterFeature> builder)
    {
        builder.ToTable("DrugStateQuarterFeature", "feature"); builder.HasKey(x => x.DrugStateQuarterFeatureId);
        builder.Property(x => x.ObservedReimbursementAmount).HasPrecision(19, 4); builder.Property(x => x.Lag1ReimbursementAmount).HasPrecision(19, 4); builder.Property(x => x.Lag2ReimbursementAmount).HasPrecision(19, 4);
        foreach (var name in new[] { nameof(DrugStateQuarterFeature.PrescriptionGrowthRate), nameof(DrugStateQuarterFeature.ReimbursementGrowthRate), nameof(DrugStateQuarterFeature.PreviousQuarterNumericDistribution), nameof(DrugStateQuarterFeature.PreviousQuarterWeightedDistribution), nameof(DrugStateQuarterFeature.PreviousQuarterAccessGap), nameof(DrugStateQuarterFeature.NationalActiveStateSharePreviousQuarter), nameof(DrugStateQuarterFeature.StateHistoricalEntryRate), nameof(DrugStateQuarterFeature.StateHistoricalMedianEntryDelay), nameof(DrugStateQuarterFeature.StateHistoricalMarketWeight), nameof(DrugStateQuarterFeature.StateVolumePercentile), nameof(DrugStateQuarterFeature.StateDataCompleteness), nameof(DrugStateQuarterFeature.RegionActiveStateShare), nameof(DrugStateQuarterFeature.RegionHistoricalEntryRate), nameof(DrugStateQuarterFeature.NeighborStateAdoptionShare), nameof(DrugStateQuarterFeature.SimilarStateAdoptionShare), nameof(DrugStateQuarterFeature.RegionPrescriptionGrowth), nameof(DrugStateQuarterFeature.NationalPrescriptionGrowth), nameof(DrugStateQuarterFeature.LabelFutureQ4NumericDistribution), nameof(DrugStateQuarterFeature.LabelFutureQ4WeightedDistribution), nameof(DrugStateQuarterFeature.LabelFutureQ4AccessGap) }) builder.Property(name).HasPrecision(19, 6);
        builder.Property(x => x.NationalReimbursementPreviousQuarter).HasPrecision(19, 4); builder.Property(x => x.LaunchCohort).HasMaxLength(32).IsRequired(); builder.Property(x => x.FeatureDefinitionHash).HasMaxLength(64).IsFixedLength().IsRequired(); builder.Property(x => x.DataQualityStatus).HasConversion<string>().HasMaxLength(32);
        builder.HasOne<FeatureSetVersion>().WithMany().HasForeignKey(x => x.FeatureSetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<GenericLaunch>().WithMany().HasForeignKey(x => x.GenericLaunchId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<State>().WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ObservationQuarterId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ApprovalQuarterId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.AvailableAsOfQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FeatureSetVersionId, x.GenericLaunchId, x.StateId, x.ObservationQuarterId }).IsUnique();
        builder.HasIndex(x => new { x.FeatureSetVersionId, x.DrugId, x.StateId, x.ObservationQuarterId });
    }
}

public sealed class LaunchQuarterSummaryConfiguration : IEntityTypeConfiguration<LaunchQuarterSummary>
{
    public void Configure(EntityTypeBuilder<LaunchQuarterSummary> builder)
    {
        builder.ToTable("LaunchQuarterSummary", "feature"); builder.HasKey(x => x.LaunchQuarterSummaryId);
        foreach (var name in new[] { nameof(LaunchQuarterSummary.NumericDistribution), nameof(LaunchQuarterSummary.WeightedDistribution), nameof(LaunchQuarterSummary.AccessGap), nameof(LaunchQuarterSummary.ConcentrationIndex), nameof(LaunchQuarterSummary.TopStateShare), nameof(LaunchQuarterSummary.TopFiveStateShare) }) builder.Property(name).HasPrecision(19, 6);
        builder.Property(x => x.TotalReimbursementAmount).HasPrecision(19, 4); builder.Property(x => x.MarketWeightVersion).HasMaxLength(64).IsRequired();
        builder.HasOne<FeatureSetVersion>().WithMany().HasForeignKey(x => x.FeatureSetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<GenericLaunch>().WithMany().HasForeignKey(x => x.GenericLaunchId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ObservationQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FeatureSetVersionId, x.GenericLaunchId, x.ObservationQuarterId }).IsUnique(); builder.HasIndex(x => new { x.FeatureSetVersionId, x.DrugId, x.ObservationQuarterId });
    }
}

public sealed class StateHistoricalProfileConfiguration : IEntityTypeConfiguration<StateHistoricalProfile>
{
    public void Configure(EntityTypeBuilder<StateHistoricalProfile> builder)
    {
        builder.ToTable("StateHistoricalProfile", "feature"); builder.HasKey(x => x.StateHistoricalProfileId);
        foreach (var name in new[] { nameof(StateHistoricalProfile.HistoricalEntryRate), nameof(StateHistoricalProfile.HistoricalMedianEntryDelay), nameof(StateHistoricalProfile.HistoricalMarketWeight), nameof(StateHistoricalProfile.VolumePercentile), nameof(StateHistoricalProfile.DataCompleteness) }) builder.Property(name).HasPrecision(19, 6);
        builder.HasOne<FeatureSetVersion>().WithMany().HasForeignKey(x => x.FeatureSetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<State>().WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.AvailableAsOfQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FeatureSetVersionId, x.StateId, x.AvailableAsOfQuarterId }).IsUnique();
    }
}

public sealed class RegionalHistoricalProfileConfiguration : IEntityTypeConfiguration<RegionalHistoricalProfile>
{
    public void Configure(EntityTypeBuilder<RegionalHistoricalProfile> builder)
    {
        builder.ToTable("RegionalHistoricalProfile", "feature"); builder.HasKey(x => x.RegionalHistoricalProfileId);
        builder.Property(x => x.Region).HasMaxLength(128).IsRequired(); builder.Property(x => x.HistoricalEntryRate).HasPrecision(19, 6); builder.Property(x => x.ActiveStateShare).HasPrecision(19, 6); builder.Property(x => x.PrescriptionGrowth).HasPrecision(19, 6);
        builder.HasOne<FeatureSetVersion>().WithMany().HasForeignKey(x => x.FeatureSetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.AvailableAsOfQuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FeatureSetVersionId, x.Region, x.AvailableAsOfQuarterId }).IsUnique();
    }
}
