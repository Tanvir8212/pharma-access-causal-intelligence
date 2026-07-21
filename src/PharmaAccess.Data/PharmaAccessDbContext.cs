using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;
using PharmaAccess.Data.Entities;

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
    public DbSet<FdaFirstGenericApprovalRaw> FdaFirstGenericApprovalRawRecords => Set<FdaFirstGenericApprovalRaw>();
    public DbSet<MedicaidStateDrugUtilizationRaw> MedicaidStateDrugUtilizationRawRecords => Set<MedicaidStateDrugUtilizationRaw>();
    public DbSet<StateReferenceRaw> StateReferenceRawRecords => Set<StateReferenceRaw>();
    public DbSet<FdaFirstGenericApprovalNormalized> FdaFirstGenericApprovalNormalizedRecords => Set<FdaFirstGenericApprovalNormalized>();
    public DbSet<MedicaidStateDrugUtilizationNormalized> MedicaidStateDrugUtilizationNormalizedRecords => Set<MedicaidStateDrugUtilizationNormalized>();
    public DbSet<StateReferenceNormalized> StateReferenceNormalizedRecords => Set<StateReferenceNormalized>();
    public DbSet<GenericLaunch> GenericLaunches => Set<GenericLaunch>();
    public DbSet<FeatureSetVersion> FeatureSetVersions => Set<FeatureSetVersion>();
    public DbSet<FeatureDefinition> FeatureDefinitions => Set<FeatureDefinition>();
    public DbSet<DrugStateQuarterFeature> DrugStateQuarterFeatures => Set<DrugStateQuarterFeature>();
    public DbSet<LaunchQuarterSummary> LaunchQuarterSummaries => Set<LaunchQuarterSummary>();
    public DbSet<StateHistoricalProfile> StateHistoricalProfiles => Set<StateHistoricalProfile>();
    public DbSet<RegionalHistoricalProfile> RegionalHistoricalProfiles => Set<RegionalHistoricalProfile>();
    public DbSet<MlExperiment> MlExperiments => Set<MlExperiment>();
    public DbSet<ModelTrainingRun> ModelTrainingRuns => Set<ModelTrainingRun>();
    public DbSet<ModelMetric> ModelMetrics => Set<ModelMetric>();
    public DbSet<ModelArtifact> ModelArtifacts => Set<ModelArtifact>();
    public DbSet<PredictionRecord> PredictionRecords => Set<PredictionRecord>();
    public DbSet<ModelCalibration> ModelCalibrations => Set<ModelCalibration>();
    public DbSet<CalibrationMetricEntity> CalibrationMetrics => Set<CalibrationMetricEntity>();
    public DbSet<CalibrationBinEntity> CalibrationBins => Set<CalibrationBinEntity>();
    public DbSet<FeatureImportanceEntity> FeatureImportanceResults => Set<FeatureImportanceEntity>();
    public DbSet<SubgroupMetricEntity> SubgroupMetrics => Set<SubgroupMetricEntity>();
    public DbSet<ModelErrorAnalysisEntity> ModelErrorAnalyses => Set<ModelErrorAnalysisEntity>();
    public DbSet<ThresholdEvaluationEntity> ThresholdEvaluations => Set<ThresholdEvaluationEntity>();
    public DbSet<ModelApprovalEntity> ModelApprovals => Set<ModelApprovalEntity>();
    public DbSet<ModelRegistryEntry> ModelRegistryEntries => Set<ModelRegistryEntry>();
    public DbSet<CausalStudyEntity> CausalStudies => Set<CausalStudyEntity>();
    public DbSet<CausalDagDefinitionEntity> CausalDagDefinitions => Set<CausalDagDefinitionEntity>();
    public DbSet<CausalAdjustmentSetEntity> CausalAdjustmentSets => Set<CausalAdjustmentSetEntity>();
    public DbSet<TreatmentDefinitionEntity> TreatmentDefinitions => Set<TreatmentDefinitionEntity>();
    public DbSet<CausalAnalysisRowEntity> CausalAnalysisRows => Set<CausalAnalysisRowEntity>();
    public DbSet<CausalEstimateEntity> CausalEstimates => Set<CausalEstimateEntity>();
    public DbSet<CausalDiagnosticEntity> CausalDiagnostics => Set<CausalDiagnosticEntity>();
    public DbSet<CounterfactualScenarioEntity> CounterfactualScenarios => Set<CounterfactualScenarioEntity>();
    public DbSet<CounterfactualResultEntity> CounterfactualResults => Set<CounterfactualResultEntity>();

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
