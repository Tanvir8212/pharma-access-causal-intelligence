using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;
using PharmaAccess.Domain.Research;
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
    [InlineData(typeof(CausalStudyEntity), "CausalStudy", "causal")]
    [InlineData(typeof(CausalDagDefinitionEntity), "CausalDagDefinition", "causal")]
    [InlineData(typeof(CausalAdjustmentSetEntity), "CausalAdjustmentSet", "causal")]
    [InlineData(typeof(TreatmentDefinitionEntity), "TreatmentDefinition", "causal")]
    [InlineData(typeof(CausalAnalysisRowEntity), "CausalAnalysisRow", "causal")]
    [InlineData(typeof(CausalEstimateEntity), "CausalEstimate", "causal")]
    [InlineData(typeof(CausalDiagnosticEntity), "CausalDiagnostic", "causal")]
    [InlineData(typeof(CounterfactualScenarioEntity), "CounterfactualScenario", "causal")]
    [InlineData(typeof(CounterfactualResultEntity), "CounterfactualResult", "causal")]
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
    public void Ml_statuses_are_strings_and_approximate_numbers_use_valid_sql_server_types()
    {
        Assert.Equal(typeof(string), Entity<MlExperiment>().FindProperty(nameof(MlExperiment.Status))!.GetProviderClrType());
        Assert.Equal(typeof(string), Entity<ModelArtifact>().FindProperty(nameof(ModelArtifact.ApprovalStatus))!.GetProviderClrType());
        var metric = Entity<ModelMetric>().FindProperty(nameof(ModelMetric.MetricValue))!; Assert.Equal("float(53)", metric.GetColumnType()); Assert.Null(metric.GetPrecision()); Assert.Null(metric.GetScale());
        var score = Entity<PredictionRecord>().FindProperty(nameof(PredictionRecord.Score))!; Assert.Equal("real", score.GetColumnType()); Assert.Null(score.GetPrecision()); Assert.Null(score.GetScale());
        var probability = Entity<PredictionRecord>().FindProperty(nameof(PredictionRecord.Probability))!; Assert.Equal("real", probability.GetColumnType()); Assert.Null(probability.GetPrecision()); Assert.Null(probability.GetScale());
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
        var metric = Entity<CalibrationMetricEntity>().FindProperty(nameof(CalibrationMetricEntity.MetricValue))!; Assert.Equal("float(53)", metric.GetColumnType()); Assert.Null(metric.GetPrecision()); Assert.Null(metric.GetScale());
    }

    [Fact]
    public void Causal_lineage_is_restrictive_uses_approximate_numbers_and_is_unique()
    {
        foreach (var type in new[] { typeof(CausalStudyEntity), typeof(CausalAnalysisRowEntity), typeof(CausalEstimateEntity), typeof(CausalDiagnosticEntity), typeof(CounterfactualScenarioEntity), typeof(CounterfactualResultEntity) }) { var entity=Model.FindEntityType(type);Assert.NotNull(entity);Assert.All(entity.GetForeignKeys(),x=>Assert.Equal(DeleteBehavior.Restrict,x.DeleteBehavior)); }
        Assert.Contains(Entity<CausalStudyEntity>().GetIndexes(),x=>x.IsUnique&&x.Properties.Single().Name==nameof(CausalStudyEntity.StudyCode));
        Assert.Contains(Entity<CausalAnalysisRowEntity>().GetIndexes(),x=>x.IsUnique&&x.Properties.Select(p=>p.Name).SequenceEqual(["CausalStudyId","DrugId","StateId","ObservationQuarterId"]));
        Assert.Equal(typeof(string),Entity<CausalStudyEntity>().FindProperty(nameof(CausalStudyEntity.Status))!.GetProviderClrType());
        var estimate=Entity<CausalEstimateEntity>().FindProperty(nameof(CausalEstimateEntity.Estimate))!;
        Assert.Equal("float(53)",estimate.GetColumnType());
        Assert.Null(estimate.GetPrecision());
        Assert.Null(estimate.GetScale());
    }

    private static IEntityType Entity<TEntity>()
    {
        var entity = Model.FindEntityType(typeof(TEntity));
        Assert.NotNull(entity);
        return entity;
    }
}

public sealed class MilestoneEightResearchMetadataTests
{
    private readonly Microsoft.EntityFrameworkCore.Metadata.IModel model=new Data.PharmaAccessDbContext(new DbContextOptionsBuilder<Data.PharmaAccessDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MetadataOnly;Trusted_Connection=True").Options).Model;
    [Theory][InlineData(typeof(ResearchProtocol),"ResearchProtocol")][InlineData(typeof(ResearchDataFreeze),"ResearchDataFreeze")][InlineData(typeof(ResearchSourceRegistration),"ResearchSourceRegistration")][InlineData(typeof(ResearchProtocolApprovalEntity),"ResearchProtocolApproval")][InlineData(typeof(ResearchFreezeSourceEntity),"ResearchFreezeSource")][InlineData(typeof(ResearchFreezeFindingEntity),"ResearchFreezeFinding")][InlineData(typeof(ResearchFreezeArtifactEntity),"ResearchFreezeArtifact")][InlineData(typeof(ResearchCohortDefinitionEntity),"ResearchCohortDefinition")][InlineData(typeof(ResearchExclusionRuleEntity),"ResearchExclusionRule")][InlineData(typeof(ResearchAnalysisPlanEntity),"ResearchAnalysisPlan")]
    public void Research_entities_map_to_research_schema(Type type,string table){var e=model.FindEntityType(type);Assert.NotNull(e);Assert.Equal("research",e.GetSchema());Assert.Equal(table,e.GetTableName());}
    [Fact]public void Research_versions_and_lineage_are_unique_and_restricted(){var p=model.FindEntityType(typeof(ResearchProtocol))!;Assert.Contains(p.GetIndexes(),x=>x.IsUnique&&x.Properties.Select(y=>y.Name).SequenceEqual([nameof(ResearchProtocol.ProtocolCode),nameof(ResearchProtocol.ProtocolVersion)]));var f=model.FindEntityType(typeof(ResearchDataFreeze))!;Assert.All(f.GetForeignKeys(),x=>Assert.Equal(DeleteBehavior.Restrict,x.DeleteBehavior));var s=model.FindEntityType(typeof(ResearchFreezeSourceEntity))!;Assert.All(s.GetForeignKeys(),x=>Assert.Equal(DeleteBehavior.Restrict,x.DeleteBehavior));}
    [Fact]public void Research_statuses_are_string_converted(){Assert.Equal(typeof(string),model.FindEntityType(typeof(ResearchProtocol))!.FindProperty(nameof(ResearchProtocol.Status))!.GetProviderClrType());Assert.Equal(typeof(string),model.FindEntityType(typeof(ResearchDataFreeze))!.FindProperty(nameof(ResearchDataFreeze.Status))!.GetProviderClrType());}
}
