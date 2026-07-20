using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.Features;
using PharmaAccess.Domain.Features;

namespace PharmaAccess.Data;

public sealed class FeatureBuildPersistence(PharmaAccessDbContext context) : IFeatureBuildPersistence
{
    public async Task PersistAsync(int datasetVersionId, int featureSetVersionId, FeatureBuildOutput output, CancellationToken cancellationToken)
    {
        var featureSet = await context.FeatureSetVersions.SingleOrDefaultAsync(x => x.FeatureSetVersionId == featureSetVersionId, cancellationToken) ?? throw new InvalidOperationException("Feature-set version does not exist.");
        if (featureSet.DatasetVersionId != datasetVersionId || featureSet.Status is FeatureSetStatus.Finalized or FeatureSetStatus.Archived or FeatureSetStatus.Rejected) throw new InvalidOperationException("Feature-set lineage is incompatible or immutable.");
        var at = DateTime.UtcNow;
        context.DrugStateQuarterFeatures.AddRange(output.FeatureRows.Select(x => new DrugStateQuarterFeature(featureSetVersionId, datasetVersionId, x.GenericLaunchId, x.DrugId, x.StateId, x.ObservationQuarterId, x.ApprovalQuarterId, x.QuarterSinceApproval, x.AvailableAsOfQuarterId, x.PrescriptionCount, x.ReimbursementAmount, x.IsPresent, x.IsFirstEntry, x.IsMissing, x.IsSuppressed, x.Lag1PrescriptionCount, x.Lag2PrescriptionCount, x.PrescriptionGrowthRate, x.LabelNextQuarterEntry, x.LabelQuartersUntilEntry, x.LabelFutureQ4NumericDistribution, x.LabelFutureQ4WeightedDistribution, x.LabelFutureQ4AccessGap, x.LabelPersistentInequality, featureSet.DefinitionHash, at)));
        context.LaunchQuarterSummaries.AddRange(output.Summaries.Select(x => new LaunchQuarterSummary(featureSetVersionId, datasetVersionId, x.GenericLaunchId, x.DrugId, x.ObservationQuarterId, x.QuarterSinceApproval, x.ActiveStateCount, x.EligibleStateCount, x.NumericDistribution, x.WeightedDistribution, x.AccessGap, x.MarketWeightVersion, x.TotalPrescriptionCount, x.TotalReimbursementAmount, x.ConcentrationIndex, x.TopStateShare, x.TopFiveStateShare, x.IsCompleteQuarter, at)));
        context.StateHistoricalProfiles.AddRange(output.StateProfiles.Select(x => new StateHistoricalProfile(featureSetVersionId, datasetVersionId, x.StateId, x.AvailableAsOfQuarterId, x.HistoricalGenericVolume, x.HistoricalLaunchCount, x.HistoricalEntryRate, x.HistoricalMedianEntryDelay, x.HistoricalMarketWeight, x.DataCompleteness, at)));
        context.RegionalHistoricalProfiles.AddRange(output.RegionalProfiles.Select(x => new RegionalHistoricalProfile(featureSetVersionId, datasetVersionId, x.Region, x.AvailableAsOfQuarterId, x.HistoricalEntryRate, x.ActiveStateShare, x.PrescriptionGrowth, x.EligibleStateCount, at)));
        await context.SaveChangesAsync(cancellationToken);
    }
}
