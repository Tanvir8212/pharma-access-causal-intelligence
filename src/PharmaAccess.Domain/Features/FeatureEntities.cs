using PharmaAccess.Domain.Entities;

namespace PharmaAccess.Domain.Features;

public sealed class DrugStateQuarterFeature
{
    private DrugStateQuarterFeature() { }
    public DrugStateQuarterFeature(int featureSetVersionId, int datasetVersionId, int genericLaunchId, int drugId, int stateId, int observationQuarterId, int approvalQuarterId, int quarterSinceApproval, int availableAsOfQuarterId, long? prescriptionCount, decimal? reimbursementAmount, bool isPresent, bool isFirstEntry, bool isMissing, bool isSuppressed, long? lag1, long? lag2, decimal? growth, bool? nextEntry, int? quartersUntilEntry, decimal? q4Nd, decimal? q4Wd, decimal? q4Gap, bool? persistentInequality, string definitionHash, DateTime generatedAtUtc)
    {
        FeatureSetVersionId = featureSetVersionId; DatasetVersionId = datasetVersionId; GenericLaunchId = genericLaunchId; DrugId = drugId; StateId = stateId; ObservationQuarterId = observationQuarterId; ApprovalQuarterId = approvalQuarterId; QuarterSinceApproval = quarterSinceApproval; AvailableAsOfQuarterId = availableAsOfQuarterId; ObservedPrescriptionCount = prescriptionCount; ObservedReimbursementAmount = reimbursementAmount; IsPresent = isPresent; IsFirstEntryQuarter = isFirstEntry; IsMissing = isMissing; IsObservedZero = prescriptionCount == 0 && !isMissing && !isSuppressed; IsSuppressed = isSuppressed; Lag1PrescriptionCount = lag1; Lag2PrescriptionCount = lag2; PrescriptionGrowthRate = growth; LabelNextQuarterEntry = nextEntry; LabelQuartersUntilEntry = quartersUntilEntry; LabelFutureQ4NumericDistribution = q4Nd; LabelFutureQ4WeightedDistribution = q4Wd; LabelFutureQ4AccessGap = q4Gap; LabelPersistentInequality = persistentInequality; LaunchCohort = approvalQuarterId.ToString(System.Globalization.CultureInfo.InvariantCulture); FeatureDefinitionHash = definitionHash; GeneratedAtUtc = generatedAtUtc; DataQualityStatus = isMissing || isSuppressed ? DataQualityStatus.Warning : DataQualityStatus.Valid; MissingFeatureCount = new object?[] { lag1, lag2, growth }.Count(x => x is null);
    }
    public long DrugStateQuarterFeatureId { get; private set; }
    public int FeatureSetVersionId { get; private set; }
    public int DatasetVersionId { get; private set; }
    public int GenericLaunchId { get; private set; }
    public int DrugId { get; private set; }
    public int StateId { get; private set; }
    public int ObservationQuarterId { get; private set; }
    public int ApprovalQuarterId { get; private set; }
    public int QuarterSinceApproval { get; private set; }
    public int AvailableAsOfQuarterId { get; private set; }
    public long? ObservedPrescriptionCount { get; private set; }
    public decimal? ObservedReimbursementAmount { get; private set; }
    public bool IsPresent { get; private set; }
    public bool IsFirstEntryQuarter { get; private set; }
    public bool IsObservedZero { get; private set; }
    public bool IsMissing { get; private set; }
    public bool IsSuppressed { get; private set; }
    public int ConsecutiveActiveQuarterCount { get; private set; }
    public long? Lag1PrescriptionCount { get; private set; }
    public long? Lag2PrescriptionCount { get; private set; }
    public decimal? Lag1ReimbursementAmount { get; private set; }
    public decimal? Lag2ReimbursementAmount { get; private set; }
    public decimal? PrescriptionGrowthRate { get; private set; }
    public decimal? ReimbursementGrowthRate { get; private set; }
    public int InitialActiveStateCount { get; private set; }
    public long InitialPrescriptionVolume { get; private set; }
    public decimal? PreviousQuarterNumericDistribution { get; private set; }
    public decimal? PreviousQuarterWeightedDistribution { get; private set; }
    public decimal? PreviousQuarterAccessGap { get; private set; }
    public decimal? NationalActiveStateSharePreviousQuarter { get; private set; }
    public long? NationalPrescriptionCountPreviousQuarter { get; private set; }
    public decimal? NationalReimbursementPreviousQuarter { get; private set; }
    public string LaunchCohort { get; private set; } = null!;
    public int NumberOfObservedQuarters { get; private set; }
    public long StateHistoricalGenericVolume { get; private set; }
    public int StateHistoricalLaunchCount { get; private set; }
    public decimal? StateHistoricalEntryRate { get; private set; }
    public decimal? StateHistoricalMedianEntryDelay { get; private set; }
    public decimal StateHistoricalMarketWeight { get; private set; }
    public decimal? StateVolumePercentile { get; private set; }
    public decimal StateDataCompleteness { get; private set; }
    public decimal? RegionActiveStateShare { get; private set; }
    public decimal? RegionHistoricalEntryRate { get; private set; }
    public decimal? NeighborStateAdoptionShare { get; private set; }
    public decimal? SimilarStateAdoptionShare { get; private set; }
    public decimal? RegionPrescriptionGrowth { get; private set; }
    public decimal? NationalPrescriptionGrowth { get; private set; }
    public bool? LabelNextQuarterEntry { get; private set; }
    public int? LabelQuartersUntilEntry { get; private set; }
    public decimal? LabelFutureQ4NumericDistribution { get; private set; }
    public decimal? LabelFutureQ4WeightedDistribution { get; private set; }
    public decimal? LabelFutureQ4AccessGap { get; private set; }
    public bool? LabelPersistentInequality { get; private set; }
    public DataQualityStatus DataQualityStatus { get; private set; }
    public int MissingFeatureCount { get; private set; }
    public string FeatureDefinitionHash { get; private set; } = null!;
    public DateTime GeneratedAtUtc { get; private set; }
}

public sealed class LaunchQuarterSummary
{
    private LaunchQuarterSummary() { }
    public LaunchQuarterSummary(int featureSetVersionId, int datasetVersionId, int genericLaunchId, int drugId, int observationQuarterId, int quarterSinceApproval, int activeStateCount, int eligibleStateCount, decimal nd, decimal wd, decimal gap, string weightVersion, long prescriptions, decimal reimbursement, decimal? concentration, decimal? topState, decimal? topFive, bool complete, DateTime generatedAtUtc) { FeatureSetVersionId = featureSetVersionId; DatasetVersionId = datasetVersionId; GenericLaunchId = genericLaunchId; DrugId = drugId; ObservationQuarterId = observationQuarterId; QuarterSinceApproval = quarterSinceApproval; ActiveStateCount = activeStateCount; EligibleStateCount = eligibleStateCount; NumericDistribution = nd; WeightedDistribution = wd; AccessGap = gap; MarketWeightVersion = weightVersion; TotalPrescriptionCount = prescriptions; TotalReimbursementAmount = reimbursement; ConcentrationIndex = concentration; TopStateShare = topState; TopFiveStateShare = topFive; IsCompleteQuarter = complete; GeneratedAtUtc = generatedAtUtc; }
    public long LaunchQuarterSummaryId { get; private set; }
    public int FeatureSetVersionId { get; private set; }
    public int DatasetVersionId { get; private set; }
    public int GenericLaunchId { get; private set; }
    public int DrugId { get; private set; }
    public int ObservationQuarterId { get; private set; }
    public int QuarterSinceApproval { get; private set; }
    public int ActiveStateCount { get; private set; }
    public int EligibleStateCount { get; private set; }
    public decimal NumericDistribution { get; private set; }
    public decimal WeightedDistribution { get; private set; }
    public decimal AccessGap { get; private set; }
    public string MarketWeightVersion { get; private set; } = null!;
    public long TotalPrescriptionCount { get; private set; }
    public decimal TotalReimbursementAmount { get; private set; }
    public decimal? ConcentrationIndex { get; private set; }
    public decimal? TopStateShare { get; private set; }
    public decimal? TopFiveStateShare { get; private set; }
    public int RegionalCoverageCount { get; private set; }
    public bool IsCompleteQuarter { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
}

public sealed class StateHistoricalProfile
{
    private StateHistoricalProfile() { }
    public StateHistoricalProfile(int featureSetVersionId, int datasetVersionId, int stateId, int cutoffQuarterId, long volume, int launches, decimal entryRate, decimal? medianDelay, decimal weight, decimal completeness, DateTime generatedAtUtc) { FeatureSetVersionId = featureSetVersionId; DatasetVersionId = datasetVersionId; StateId = stateId; AvailableAsOfQuarterId = cutoffQuarterId; HistoricalGenericVolume = volume; HistoricalLaunchCount = launches; HistoricalEntryRate = entryRate; HistoricalMedianEntryDelay = medianDelay; HistoricalMarketWeight = weight; DataCompleteness = completeness; GeneratedAtUtc = generatedAtUtc; }
    public long StateHistoricalProfileId { get; private set; }
    public int FeatureSetVersionId { get; private set; }
    public int DatasetVersionId { get; private set; }
    public int StateId { get; private set; }
    public int AvailableAsOfQuarterId { get; private set; }
    public long HistoricalGenericVolume { get; private set; }
    public int HistoricalLaunchCount { get; private set; }
    public decimal HistoricalEntryRate { get; private set; }
    public decimal? HistoricalMedianEntryDelay { get; private set; }
    public decimal HistoricalMarketWeight { get; private set; }
    public decimal? VolumePercentile { get; private set; }
    public decimal DataCompleteness { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
}

public sealed class RegionalHistoricalProfile
{
    private RegionalHistoricalProfile() { }
    public RegionalHistoricalProfile(int featureSetVersionId, int datasetVersionId, string region, int cutoffQuarterId, decimal entryRate, decimal activeShare, decimal? growth, int eligibleStates, DateTime generatedAtUtc) { FeatureSetVersionId = featureSetVersionId; DatasetVersionId = datasetVersionId; Region = region; AvailableAsOfQuarterId = cutoffQuarterId; HistoricalEntryRate = entryRate; ActiveStateShare = activeShare; PrescriptionGrowth = growth; EligibleStateCount = eligibleStates; GeneratedAtUtc = generatedAtUtc; }
    public long RegionalHistoricalProfileId { get; private set; }
    public int FeatureSetVersionId { get; private set; }
    public int DatasetVersionId { get; private set; }
    public string Region { get; private set; } = null!;
    public int AvailableAsOfQuarterId { get; private set; }
    public decimal HistoricalEntryRate { get; private set; }
    public decimal ActiveStateShare { get; private set; }
    public decimal? PrescriptionGrowth { get; private set; }
    public int EligibleStateCount { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
}
