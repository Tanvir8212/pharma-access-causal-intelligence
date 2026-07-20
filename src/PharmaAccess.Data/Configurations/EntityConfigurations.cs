using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Data.Configurations;

internal static class ConfigurationHelpers
{
    public static void ConfigureEnum<TEntity, TEnum>(PropertyBuilder<TEnum> property)
        where TEntity : class where TEnum : struct, Enum => property.HasConversion<string>().HasMaxLength(32);
}

public sealed class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        builder.ToTable("Drug", "core");
        builder.HasKey(x => x.DrugId);
        builder.Property(x => x.NormalizedIngredient).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IngredientCombination).HasMaxLength(512);
        builder.Property(x => x.DosageForm).HasMaxLength(128);
        builder.Property(x => x.Route).HasMaxLength(128);
        builder.Property(x => x.Strength).HasMaxLength(128);
        builder.Property(x => x.RxNormId).HasMaxLength(64);
        builder.Property(x => x.TherapeuticClass).HasMaxLength(256);
        builder.HasIndex(x => x.NormalizedIngredient);
    }
}

public sealed class DrugProductConfiguration : IEntityTypeConfiguration<DrugProduct>
{
    public void Configure(EntityTypeBuilder<DrugProduct> builder)
    {
        builder.ToTable("DrugProduct", "core");
        builder.HasKey(x => x.DrugProductId);
        builder.Property(x => x.OriginalNdc).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedNdc).HasMaxLength(32);
        builder.Property(x => x.ProductName).HasMaxLength(512);
        builder.Property(x => x.Labeler).HasMaxLength(256);
        builder.Property(x => x.SourceSystem).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MappingConfidence).HasConversion(
            value => value.HasValue ? value.Value.Value : (double?)null,
            value => value.HasValue ? new Percentage(value.Value) : null);
        builder.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SourceSystem, x.OriginalNdc }).IsUnique();
    }
}

public sealed class FirstGenericApprovalConfiguration : IEntityTypeConfiguration<FirstGenericApproval>
{
    public void Configure(EntityTypeBuilder<FirstGenericApproval> builder)
    {
        builder.ToTable("FirstGenericApproval", "core");
        builder.HasKey(x => x.ApprovalId);
        builder.Property(x => x.ApprovalDate).HasColumnType("date");
        builder.Property(x => x.ApplicationNumber).HasMaxLength(64);
        builder.Property(x => x.Applicant).HasMaxLength(256);
        builder.Property(x => x.ApprovalSource).HasMaxLength(128).IsRequired();
        builder.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DrugId, x.ApprovalDate });
    }
}

public sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("State", "core");
        builder.HasKey(x => x.StateId);
        builder.Property(x => x.StateCode).HasConversion(value => value.Value, value => new StateCode(value)).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(x => x.StateName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(128);
        builder.Property(x => x.Division).HasMaxLength(128);
        builder.Property(x => x.ExclusionReason).HasMaxLength(512);
        builder.HasIndex(x => x.StateCode).IsUnique();
    }
}

public sealed class QuarterDimensionConfiguration : IEntityTypeConfiguration<QuarterDimension>
{
    public void Configure(EntityTypeBuilder<QuarterDimension> builder)
    {
        builder.ToTable("CalendarQuarter", "core");
        builder.HasKey(x => x.QuarterId);
        builder.Property(x => x.QuarterStartDate).HasColumnType("date");
        builder.Property(x => x.QuarterEndDate).HasColumnType("date");
        builder.Property(x => x.DisplayCode).HasMaxLength(7).IsRequired();
        builder.HasIndex(x => new { x.CalendarYear, x.QuarterNumber }).IsUnique();
        builder.HasIndex(x => x.DisplayCode).IsUnique();
    }
}

public sealed class DatasetVersionConfiguration : IEntityTypeConfiguration<DatasetVersion>
{
    public void Configure(EntityTypeBuilder<DatasetVersion> builder)
    {
        builder.ToTable("DatasetVersion", "core");
        builder.HasKey(x => x.DatasetVersionId);
        builder.Property(x => x.VersionCode).HasConversion(value => value.Value, value => new DatasetVersionCode(value)).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.SchemaVersion).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FeatureVersion).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CodeCommitHash).HasMaxLength(64);
        builder.Property(x => x.ValidationStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.HasIndex(x => x.VersionCode).IsUnique();
    }
}

public sealed class SourceFileConfiguration : IEntityTypeConfiguration<SourceFile>
{
    public void Configure(EntityTypeBuilder<SourceFile> builder)
    {
        builder.ToTable("SourceFile", "core");
        builder.HasKey(x => x.SourceFileId);
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.SourceUrl).HasMaxLength(2048);
        builder.Property(x => x.ReportingPeriod).HasMaxLength(64);
        builder.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LicenseNote).HasMaxLength(1024);
        builder.Property(x => x.ImportStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ErrorDetails).HasMaxLength(4000);
        builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DatasetVersionId, x.Sha256 }).IsUnique();
    }
}

public sealed class StateDrugUtilizationConfiguration : IEntityTypeConfiguration<StateDrugUtilization>
{
    public void Configure(EntityTypeBuilder<StateDrugUtilization> builder)
    {
        builder.ToTable("StateDrugUtilization", "core");
        builder.HasKey(x => x.StateDrugUtilizationId);
        builder.Property(x => x.ReimbursementAmount).HasPrecision(19, 4);
        builder.Property(x => x.DataQualityStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DrugProduct>().WithMany().HasForeignKey(x => x.DrugProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<State>().WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.QuarterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DatasetVersionId, x.DrugId, x.StateId, x.QuarterId }).IsUnique();
    }
}

public sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("JobRun", "audit");
        builder.HasKey(x => x.JobRunId);
        builder.Property(x => x.JobType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.MetadataJson).HasMaxLength(8000);
        builder.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CorrelationId);
    }
}
