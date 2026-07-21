using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;
using PharmaAccess.Data.Entities;
using Xunit;

namespace PharmaAccess.Data.Tests;

public sealed class ModelMetadataTests
{
    private static IModel Model
    {
        get
        {
            var options = new DbContextOptionsBuilder<Data.PharmaAccessDbContext>()
                .UseSqlServer("Server=localhost;Database=MetadataOnly;Trusted_Connection=True;TrustServerCertificate=True")
                .Options;
            using var context = new Data.PharmaAccessDbContext(options);
            return context.Model;
        }
    }

    [Theory]
    [InlineData(typeof(Drug), "Drug", "core")]
    [InlineData(typeof(DrugProduct), "DrugProduct", "core")]
    [InlineData(typeof(FirstGenericApproval), "FirstGenericApproval", "core")]
    [InlineData(typeof(State), "State", "core")]
    [InlineData(typeof(QuarterDimension), "CalendarQuarter", "core")]
    [InlineData(typeof(DatasetVersion), "DatasetVersion", "core")]
    [InlineData(typeof(SourceFile), "SourceFile", "core")]
    [InlineData(typeof(StateDrugUtilization), "StateDrugUtilization", "core")]
    [InlineData(typeof(JobRun), "JobRun", "audit")]
    [InlineData(typeof(FdaFirstGenericApprovalRaw), "FdaFirstGenericApprovalRaw", "raw")]
    [InlineData(typeof(MedicaidStateDrugUtilizationRaw), "MedicaidStateDrugUtilizationRaw", "raw")]
    [InlineData(typeof(StateReferenceRaw), "StateReferenceRaw", "raw")]
    [InlineData(typeof(FdaFirstGenericApprovalNormalized), "FdaFirstGenericApprovalNormalized", "stg")]
    [InlineData(typeof(MedicaidStateDrugUtilizationNormalized), "MedicaidStateDrugUtilizationNormalized", "stg")]
    [InlineData(typeof(StateReferenceNormalized), "StateReferenceNormalized", "stg")]
    [InlineData(typeof(GenericLaunch), "GenericLaunch", "core")]
    [InlineData(typeof(FeatureSetVersion), "FeatureSetVersion", "feature")]
    [InlineData(typeof(FeatureDefinition), "FeatureDefinition", "feature")]
    [InlineData(typeof(DrugStateQuarterFeature), "DrugStateQuarterFeature", "feature")]
    [InlineData(typeof(LaunchQuarterSummary), "LaunchQuarterSummary", "feature")]
    [InlineData(typeof(StateHistoricalProfile), "StateHistoricalProfile", "feature")]
    [InlineData(typeof(RegionalHistoricalProfile), "RegionalHistoricalProfile", "feature")]
    [InlineData(typeof(MlExperiment), "MlExperiment", "ml")]
    [InlineData(typeof(ModelTrainingRun), "ModelTrainingRun", "ml")]
    [InlineData(typeof(ModelMetric), "ModelMetric", "ml")]
    [InlineData(typeof(ModelArtifact), "ModelArtifact", "ml")]
    [InlineData(typeof(PredictionRecord), "PredictionRecord", "ml")]
    [InlineData(typeof(ModelCalibration), "ModelCalibration", "ml")]
    [InlineData(typeof(CalibrationMetricEntity), "CalibrationMetric", "ml")]
    [InlineData(typeof(CalibrationBinEntity), "CalibrationBin", "ml")]
    [InlineData(typeof(FeatureImportanceEntity), "FeatureImportanceResult", "ml")]
    [InlineData(typeof(SubgroupMetricEntity), "SubgroupMetric", "ml")]
    [InlineData(typeof(ModelErrorAnalysisEntity), "ModelErrorAnalysis", "ml")]
    [InlineData(typeof(ThresholdEvaluationEntity), "ThresholdEvaluation", "ml")]
    [InlineData(typeof(ModelApprovalEntity), "ModelApproval", "ml")]
    [InlineData(typeof(ModelRegistryEntry), "ModelRegistryEntry", "ml")]
    public void Entities_use_expected_tables(Type type, string table, string schema)
    {
        var entity = Model.FindEntityType(type);
        Assert.NotNull(entity);
        Assert.Equal(table, entity.GetTableName());
        Assert.Equal(schema, entity.GetSchema());
    }

    [Fact] public void State_code_is_unique() => Assert.Contains(Entity<State>().GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(State.StateCode));
    [Fact] public void Dataset_version_code_is_unique() => Assert.Contains(Entity<DatasetVersion>().GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(DatasetVersion.VersionCode));
    [Fact] public void Quarter_year_and_number_are_unique() => Assert.Contains(Entity<QuarterDimension>().GetIndexes(), index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual([nameof(QuarterDimension.CalendarYear), nameof(QuarterDimension.QuarterNumber)]));
    [Fact] public void Provenance_relationships_restrict_deletion() { Assert.All(Entity<SourceFile>().GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior)); Assert.All(Entity<StateDrugUtilization>().GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior)); }
    [Fact] public void Reimbursement_has_required_precision() { var property = Entity<StateDrugUtilization>().FindProperty(nameof(StateDrugUtilization.ReimbursementAmount)); Assert.NotNull(property); Assert.Equal(19, property.GetPrecision()); Assert.Equal(4, property.GetScale()); }
    [Fact] public void Status_enums_are_converted_to_strings() { var property = Entity<DatasetVersion>().FindProperty(nameof(DatasetVersion.Status)); Assert.NotNull(property); Assert.Equal(typeof(string), property.GetProviderClrType()); }
    [Fact] public void Required_properties_are_not_nullable() { var property = Entity<Drug>().FindProperty(nameof(Drug.NormalizedIngredient)); Assert.NotNull(property); Assert.False(property.IsNullable); }
    [Fact]
    public void Ingestion_rows_have_unique_source_row_identity_and_restrict_provenance_deletion()
    {
        foreach (var type in new[] { typeof(FdaFirstGenericApprovalRaw), typeof(MedicaidStateDrugUtilizationRaw), typeof(StateReferenceRaw), typeof(FdaFirstGenericApprovalNormalized), typeof(MedicaidStateDrugUtilizationNormalized), typeof(StateReferenceNormalized) })
        {
            var entity = Model.FindEntityType(type);
            Assert.NotNull(entity);
            Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Select(p => p.Name).SequenceEqual(["SourceFileId", "SourceRowNumber"]));
            Assert.All(entity.GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
        }
    }

    [Fact]
    public void Medicaid_ingestion_reimbursement_uses_required_precision()
    {
        foreach (var property in new[]
        {
            Entity<MedicaidStateDrugUtilizationRaw>().FindProperty(nameof(MedicaidStateDrugUtilizationRaw.ParsedReimbursementAmount)),
            Entity<MedicaidStateDrugUtilizationNormalized>().FindProperty(nameof(MedicaidStateDrugUtilizationNormalized.ReimbursementAmount))
        })
        {
            Assert.NotNull(property);
            Assert.Equal(19, property.GetPrecision());
            Assert.Equal(4, property.GetScale());
        }
    }

    [Fact]
    public void Analytical_grains_and_feature_names_are_unique()
    {
        Assert.Contains(Entity<DrugStateQuarterFeature>().GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["FeatureSetVersionId", "GenericLaunchId", "StateId", "ObservationQuarterId"]));
        Assert.Contains(Entity<LaunchQuarterSummary>().GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["FeatureSetVersionId", "GenericLaunchId", "ObservationQuarterId"]));
        Assert.Contains(Entity<StateHistoricalProfile>().GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["FeatureSetVersionId", "StateId", "AvailableAsOfQuarterId"]));
        Assert.Contains(Entity<RegionalHistoricalProfile>().GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["FeatureSetVersionId", "Region", "AvailableAsOfQuarterId"]));
        Assert.Contains(Entity<FeatureDefinition>().GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["FeatureSetVersionId", "FeatureName"]));
    }

    [Fact]
    public void Feature_lineage_never_cascade_deletes()
    {
        foreach (var type in new[] { typeof(GenericLaunch), typeof(FeatureSetVersion), typeof(FeatureDefinition), typeof(DrugStateQuarterFeature), typeof(LaunchQuarterSummary), typeof(StateHistoricalProfile), typeof(RegionalHistoricalProfile) })
        {
            var entity = Model.FindEntityType(type); Assert.NotNull(entity); Assert.All(entity.GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
        }
    }

    [Fact]
    public void Feature_status_enums_are_strings_and_precision_is_explicit()
    {
        Assert.Equal(typeof(string), Entity<FeatureSetVersion>().FindProperty(nameof(FeatureSetVersion.Status))!.GetProviderClrType());
        var property = Entity<LaunchQuarterSummary>().FindProperty(nameof(LaunchQuarterSummary.NumericDistribution)); Assert.Equal(19, property!.GetPrecision()); Assert.Equal(6, property.GetScale());
        var money = Entity<LaunchQuarterSummary>().FindProperty(nameof(LaunchQuarterSummary.TotalReimbursementAmount)); Assert.Equal(19, money!.GetPrecision()); Assert.Equal(4, money.GetScale());
    }

    [Fact]
    public void Ml_lineage_is_restrictive_and_artifacts_are_unique()
    {
        foreach (var type in new[] { typeof(MlExperiment), typeof(ModelTrainingRun), typeof(ModelMetric), typeof(ModelArtifact), typeof(PredictionRecord) })
        {
            var entity = Model.FindEntityType(type); Assert.NotNull(entity); Assert.All(entity.GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
        }
        Assert.Contains(Entity<ModelArtifact>().GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(ModelArtifact.ModelVersionCode));
        Assert.Contains(Entity<ModelArtifact>().GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(ModelArtifact.Sha256));
    }

    [Fact]
    public void Ml_statuses_are_strings_and_metrics_have_precision()
    {
        Assert.Equal(typeof(string), Entity<MlExperiment>().FindProperty(nameof(MlExperiment.Status))!.GetProviderClrType());
        Assert.Equal(typeof(string), Entity<ModelArtifact>().FindProperty(nameof(ModelArtifact.ApprovalStatus))!.GetProviderClrType());
        var metric = Entity<ModelMetric>().FindProperty(nameof(ModelMetric.MetricValue)); Assert.Equal(20, metric!.GetPrecision()); Assert.Equal(10, metric.GetScale());
    }

    [Fact]
    public void Ml_evaluation_lineage_is_restrictive_and_one_champion_is_enforced()
    {
        foreach (var type in new[] { typeof(ModelCalibration), typeof(CalibrationMetricEntity), typeof(CalibrationBinEntity), typeof(FeatureImportanceEntity), typeof(SubgroupMetricEntity), typeof(ModelErrorAnalysisEntity), typeof(ThresholdEvaluationEntity), typeof(ModelApprovalEntity), typeof(ModelRegistryEntry) })
        {
            var entity = Model.FindEntityType(type); Assert.NotNull(entity); Assert.All(entity.GetForeignKeys(), key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));
        }
        Assert.Contains(Entity<ModelRegistryEntry>().GetIndexes(), x => x.IsUnique && x.GetFilter() == "[IsChampion] = 1");
        Assert.Equal(typeof(string), Entity<ModelCalibration>().FindProperty(nameof(ModelCalibration.Status))!.GetProviderClrType());
        Assert.Equal(20, Entity<CalibrationMetricEntity>().FindProperty(nameof(CalibrationMetricEntity.MetricValue))!.GetPrecision());
    }

    private static IEntityType Entity<TEntity>()
    {
        var entity = Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        return entity;
    }
}
