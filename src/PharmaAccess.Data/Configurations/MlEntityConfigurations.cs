using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmaAccess.Data.Entities;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;

namespace PharmaAccess.Data.Configurations;

public sealed class MlExperimentConfiguration : IEntityTypeConfiguration<MlExperiment>
{
    public void Configure(EntityTypeBuilder<MlExperiment> b) { b.ToTable("MlExperiment", "ml"); b.HasKey(x => x.ExperimentId); b.Property(x => x.ExperimentName).HasMaxLength(128).IsRequired(); b.Property(x => x.TaskName).HasMaxLength(64).IsRequired(); b.Property(x => x.FeatureSelectionVersion).HasMaxLength(64).IsRequired(); b.Property(x => x.SplitManifestVersion).HasMaxLength(128).IsRequired(); b.Property(x => x.ResearchQuestion).HasMaxLength(2000).IsRequired(); b.Property(x => x.PrimaryMetric).HasMaxLength(64).IsRequired(); b.Property(x => x.ConfigurationJson).HasMaxLength(8000).IsRequired(); b.Property(x => x.CodeCommitHash).HasMaxLength(64); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); b.Property(x => x.FailureMessage).HasMaxLength(4000); b.Property(x => x.Notes).HasMaxLength(4000); b.HasOne<DatasetVersion>().WithMany().HasForeignKey(x => x.DatasetVersionId).OnDelete(DeleteBehavior.Restrict); b.HasOne<FeatureSetVersion>().WithMany().HasForeignKey(x => x.FeatureSetVersionId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.TaskName, x.ExperimentName }).IsUnique(); }
}
public sealed class ModelTrainingRunConfiguration : IEntityTypeConfiguration<ModelTrainingRun>
{
    public void Configure(EntityTypeBuilder<ModelTrainingRun> b) { b.ToTable("ModelTrainingRun", "ml"); b.HasKey(x => x.ModelTrainingRunId); b.Property(x => x.TrainerName).HasMaxLength(128).IsRequired(); b.Property(x => x.Algorithm).HasMaxLength(128).IsRequired(); b.Property(x => x.HyperparametersJson).HasMaxLength(4000).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); b.Property(x => x.FailureMessage).HasMaxLength(4000); b.HasOne<MlExperiment>().WithMany().HasForeignKey(x => x.ExperimentId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.ExperimentId, x.TrainerName }); }
}
public sealed class ModelMetricConfiguration : IEntityTypeConfiguration<ModelMetric>
{
    public void Configure(EntityTypeBuilder<ModelMetric> b) { b.ToTable("ModelMetric", "ml"); b.HasKey(x => x.ModelMetricId); b.Property(x => x.Partition).HasConversion<string>().HasMaxLength(32); b.Property(x => x.MetricName).HasMaxLength(64).IsRequired(); b.Property(x => x.MetricValue).HasPrecision(20, 10); b.Property(x => x.Threshold).HasPrecision(20, 10); b.Property(x => x.SubgroupName).HasMaxLength(128); b.Property(x => x.SubgroupValue).HasMaxLength(256); b.HasOne<ModelTrainingRun>().WithMany().HasForeignKey(x => x.ModelTrainingRunId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.ModelTrainingRunId, x.Partition, x.MetricName, x.SubgroupName, x.SubgroupValue }).IsUnique(); }
}
public sealed class ModelArtifactConfiguration : IEntityTypeConfiguration<ModelArtifact>
{
    public void Configure(EntityTypeBuilder<ModelArtifact> b) { b.ToTable("ModelArtifact", "ml"); b.HasKey(x => x.ModelArtifactId); b.Property(x => x.ModelVersionCode).HasMaxLength(200).IsRequired(); b.Property(x => x.ArtifactPath).HasMaxLength(2048).IsRequired(); b.Property(x => x.Sha256).HasMaxLength(64).IsFixedLength().IsRequired(); b.Property(x => x.InputSchemaHash).HasMaxLength(64).IsFixedLength().IsRequired(); b.Property(x => x.FeatureSchemaHash).HasMaxLength(64).IsFixedLength().IsRequired(); b.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(32); b.Property(x => x.ApprovalNotes).HasMaxLength(2000); b.HasOne<ModelTrainingRun>().WithMany().HasForeignKey(x => x.ModelTrainingRunId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.ModelVersionCode).IsUnique(); b.HasIndex(x => x.Sha256).IsUnique(); }
}
public sealed class PredictionRecordConfiguration : IEntityTypeConfiguration<PredictionRecord>
{
    public void Configure(EntityTypeBuilder<PredictionRecord> b) { b.ToTable("PredictionRecord", "ml"); b.HasKey(x => x.PredictionRecordId); b.Property(x => x.Partition).HasConversion<string>().HasMaxLength(32); b.Property(x => x.Score).HasPrecision(20, 10); b.Property(x => x.Probability).HasPrecision(20, 10); b.Property(x => x.Threshold).HasPrecision(20, 10); b.HasOne<ModelTrainingRun>().WithMany().HasForeignKey(x => x.ModelTrainingRunId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Drug>().WithMany().HasForeignKey(x => x.DrugId).OnDelete(DeleteBehavior.Restrict); b.HasOne<State>().WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.Restrict); b.HasOne<QuarterDimension>().WithMany().HasForeignKey(x => x.ObservationQuarterId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.ModelTrainingRunId, x.Partition, x.DrugId, x.StateId, x.ObservationQuarterId }).IsUnique(); }
}
