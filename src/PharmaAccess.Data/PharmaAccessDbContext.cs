using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PharmaAccess.Domain.Entities;

namespace PharmaAccess.Data;

public sealed class PharmaAccessDbContext(DbContextOptions<PharmaAccessDbContext> options) : DbContext(options)
{
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<DrugProduct> DrugProducts => Set<DrugProduct>();
    public DbSet<FirstGenericApproval> FirstGenericApprovals => Set<FirstGenericApproval>();
    public DbSet<State> States => Set<State>();
    public DbSet<QuarterDimension> CalendarQuarters => Set<QuarterDimension>();
    public DbSet<DatasetVersion> DatasetVersions => Set<DatasetVersion>();
    public DbSet<SourceFile> SourceFiles => Set<SourceFile>();
    public DbSet<StateDrugUtilization> StateDrugUtilizations => Set<StateDrugUtilization>();
    public DbSet<JobRun> JobRuns => Set<JobRun>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PharmaAccessDbContext).Assembly);
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => value,
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        value => value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null);
}
