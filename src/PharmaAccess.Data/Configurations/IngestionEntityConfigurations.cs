using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaAccess.Domain.Entities;

namespace PharmaAccess.Data.Configurations;

internal static class IngestionConfiguration
{
    internal static void ConfigureRaw<T>(EntityTypeBuilder<T> builder) where T : class
    {
        builder.Property("SourceRowNumber").IsRequired();
        builder.Property("ParseStatus").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property("ErrorCode").HasMaxLength(64);
        builder.Property("ErrorMessage").HasMaxLength(4000);
        builder.HasOne<SourceFile>().WithMany().HasForeignKey("SourceFileId").OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex("SourceFileId", "SourceRowNumber").IsUnique();
        builder.HasIndex("SourceFileId", "ParseStatus");
    }

    internal static void ConfigureStaging<T>(EntityTypeBuilder<T> builder) where T : class
    {
        builder.Property("SourceRowNumber").IsRequired();
        builder.Property("ValidationStatus").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property("ValidationMessagesJson").HasMaxLength(4000);
        builder.HasOne<SourceFile>().WithMany().HasForeignKey("SourceFileId").OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex("SourceFileId", "SourceRowNumber").IsUnique();
        builder.HasIndex("SourceFileId", "ValidationStatus");
    }
}

public sealed class FdaFirstGenericApprovalRawConfiguration : IEntityTypeConfiguration<FdaFirstGenericApprovalRaw>
{
    public void Configure(EntityTypeBuilder<FdaFirstGenericApprovalRaw> builder)
    {
        builder.ToTable("FdaFirstGenericApprovalRaw", "raw");
        builder.HasKey(x => x.RawRecordId);
        IngestionConfiguration.ConfigureRaw(builder);
        builder.Property(x => x.ApplicationNumberRaw).HasMaxLength(128);
        builder.Property(x => x.ProductNumberRaw).HasMaxLength(128);
        builder.Property(x => x.ActiveIngredientRaw).HasMaxLength(1000);
        builder.Property(x => x.DosageFormRaw).HasMaxLength(512);
        builder.Property(x => x.StrengthRaw).HasMaxLength(512);
        builder.Property(x => x.ApplicantRaw).HasMaxLength(512);
        builder.Property(x => x.ApprovalDateRaw).HasMaxLength(128);
        builder.Property(x => x.ParsedApprovalDate).HasColumnType("date");
    }
}

public sealed class MedicaidStateDrugUtilizationRawConfiguration : IEntityTypeConfiguration<MedicaidStateDrugUtilizationRaw>
{
    public void Configure(EntityTypeBuilder<MedicaidStateDrugUtilizationRaw> builder)
    {
        builder.ToTable("MedicaidStateDrugUtilizationRaw", "raw");
        builder.HasKey(x => x.RawRecordId);
        IngestionConfiguration.ConfigureRaw(builder);
        builder.Property(x => x.UtilizationTypeRaw).HasMaxLength(128);
        builder.Property(x => x.StateCodeRaw).HasMaxLength(32);
        builder.Property(x => x.NdcRaw).HasMaxLength(128);
        builder.Property(x => x.ProductNameRaw).HasMaxLength(1000);
        builder.Property(x => x.PackageSizeRaw).HasMaxLength(128);
        builder.Property(x => x.YearRaw).HasMaxLength(64);
        builder.Property(x => x.QuarterRaw).HasMaxLength(64);
        builder.Property(x => x.PrescriptionCountRaw).HasMaxLength(128);
        builder.Property(x => x.ReimbursementAmountRaw).HasMaxLength(128);
        builder.Property(x => x.ParsedReimbursementAmount).HasPrecision(19, 4);
    }
}

public sealed class StateReferenceRawConfiguration : IEntityTypeConfiguration<StateReferenceRaw>
{
    public void Configure(EntityTypeBuilder<StateReferenceRaw> builder)
    {
        builder.ToTable("StateReferenceRaw", "raw");
        builder.HasKey(x => x.RawRecordId);
        IngestionConfiguration.ConfigureRaw(builder);
        builder.Property(x => x.StateCodeRaw).HasMaxLength(32);
        builder.Property(x => x.StateNameRaw).HasMaxLength(512);
        builder.Property(x => x.RegionRaw).HasMaxLength(256);
        builder.Property(x => x.DivisionRaw).HasMaxLength(256);
        builder.Property(x => x.EligibilityRaw).HasMaxLength(128);
    }
}

public sealed class FdaFirstGenericApprovalNormalizedConfiguration : IEntityTypeConfiguration<FdaFirstGenericApprovalNormalized>
{
    public void Configure(EntityTypeBuilder<FdaFirstGenericApprovalNormalized> builder)
    {
        builder.ToTable("FdaFirstGenericApprovalNormalized", "stg");
        builder.HasKey(x => x.StagingId);
        IngestionConfiguration.ConfigureStaging(builder);
        builder.Property(x => x.NormalizedIngredient).HasMaxLength(512).IsRequired();
        builder.Property(x => x.DosageForm).HasMaxLength(256);
        builder.Property(x => x.Strength).HasMaxLength(256);
        builder.Property(x => x.Applicant).HasMaxLength(512);
        builder.Property(x => x.ApplicationNumber).HasMaxLength(128);
        builder.Property(x => x.ApprovalDate).HasColumnType("date");
        builder.Property(x => x.MappingStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.NormalizedIngredient, x.ApprovalDate });
    }
}

public sealed class MedicaidStateDrugUtilizationNormalizedConfiguration : IEntityTypeConfiguration<MedicaidStateDrugUtilizationNormalized>
{
    public void Configure(EntityTypeBuilder<MedicaidStateDrugUtilizationNormalized> builder)
    {
        builder.ToTable("MedicaidStateDrugUtilizationNormalized", "stg");
        builder.HasKey(x => x.StagingId);
        IngestionConfiguration.ConfigureStaging(builder);
        builder.Property(x => x.StateCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(x => x.OriginalNdc).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedNdc).HasMaxLength(11).IsFixedLength();
        builder.Property(x => x.ProductName).HasMaxLength(1000);
        builder.Property(x => x.ReimbursementAmount).HasPrecision(19, 4);
        builder.Property(x => x.MappingStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.StateCode, x.CalendarYear, x.QuarterNumber });
        builder.HasIndex(x => x.NormalizedNdc);
    }
}

public sealed class StateReferenceNormalizedConfiguration : IEntityTypeConfiguration<StateReferenceNormalized>
{
    public void Configure(EntityTypeBuilder<StateReferenceNormalized> builder)
    {
        builder.ToTable("StateReferenceNormalized", "stg");
        builder.HasKey(x => x.StagingId);
        IngestionConfiguration.ConfigureStaging(builder);
        builder.Property(x => x.StateCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(x => x.StateName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Region).HasMaxLength(128);
        builder.Property(x => x.Division).HasMaxLength(128);
        builder.HasIndex(x => new { x.SourceFileId, x.StateCode }).IsUnique();
    }
}
