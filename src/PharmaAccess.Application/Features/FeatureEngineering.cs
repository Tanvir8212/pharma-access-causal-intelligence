using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;
using PharmaAccess.Domain.Services;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Application.Features;

public sealed record FeatureObservation(
    int GenericLaunchId, int DrugId, int StateId, string Region,
    CalendarQuarter ApprovalQuarter, int ApprovalQuarterId,
    CalendarQuarter ObservationQuarter, int ObservationQuarterId,
    bool IsEligibleState, long? PrescriptionCount, decimal? ReimbursementAmount,
    bool IsSuppressed, decimal FrozenMarketWeight);

public sealed record FeatureBuildRequest(
    int DatasetVersionId, int FeatureSetVersionId, DatasetVersionStatus DatasetStatus,
    FeatureSetStatus FeatureSetStatus, CalendarQuarter ObservationStartQuarter,
    CalendarQuarter ObservationEndQuarter, IStateEntryPolicy StateEntryPolicy,
    string EligibleStatePolicy, string MarketWeightVersion, CalendarQuarter MarketWeightWindowEnd,
    int Q4Horizon, decimal PersistentInequalityThreshold, int PersistentInequalityConsecutiveQuarters,
    bool DryRun, string CorrelationId, string? RequestedBy = null, int BatchSize = 500, int ErrorLimit = 100);

public sealed record DrugStateQuarterFeatureRow(
    int GenericLaunchId, int DrugId, int StateId, int ObservationQuarterId, int ApprovalQuarterId,
    int QuarterSinceApproval, int AvailableAsOfQuarterId, long? PrescriptionCount, decimal? ReimbursementAmount,
    bool IsPresent, bool IsFirstEntry, bool IsMissing, bool IsSuppressed,
    long? Lag1PrescriptionCount, long? Lag2PrescriptionCount, decimal? PrescriptionGrowthRate,
    bool? LabelNextQuarterEntry, int? LabelQuartersUntilEntry, decimal? LabelFutureQ4NumericDistribution,
    decimal? LabelFutureQ4WeightedDistribution, decimal? LabelFutureQ4AccessGap, bool? LabelPersistentInequality);

public sealed record LaunchQuarterSummaryRow(
    int GenericLaunchId, int DrugId, int ObservationQuarterId, int QuarterSinceApproval,
    int ActiveStateCount, int EligibleStateCount, decimal NumericDistribution,
    decimal WeightedDistribution, decimal AccessGap, string MarketWeightVersion,
    long TotalPrescriptionCount, decimal TotalReimbursementAmount,
    decimal? ConcentrationIndex, decimal? TopStateShare, decimal? TopFiveStateShare, bool IsCompleteQuarter);

public sealed record StateHistoricalProfileRow(int StateId, int AvailableAsOfQuarterId, long HistoricalGenericVolume, int HistoricalLaunchCount, decimal HistoricalEntryRate, decimal? HistoricalMedianEntryDelay, decimal HistoricalMarketWeight, decimal DataCompleteness);
public sealed record RegionalHistoricalProfileRow(string Region, int AvailableAsOfQuarterId, decimal HistoricalEntryRate, decimal ActiveStateShare, decimal? PrescriptionGrowth, int EligibleStateCount);
public sealed record FeatureBuildOutput(IReadOnlyList<DrugStateQuarterFeatureRow> FeatureRows, IReadOnlyList<LaunchQuarterSummaryRow> Summaries, IReadOnlyList<StateHistoricalProfileRow> StateProfiles, IReadOnlyList<RegionalHistoricalProfileRow> RegionalProfiles);
public sealed record FeatureBuildResult(
    int DatasetVersionId, int FeatureSetVersionId, bool DryRun, int LaunchesEvaluated,
    int LaunchesAccepted, int LaunchesRejected, int StateQuarterRowsGenerated,
    int LaunchQuarterSummariesGenerated, int LabelsGenerated, int CensoredLabels,
    IReadOnlyDictionary<string, int> MissingFeatureStatistics, IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors, IReadOnlyList<LeakageFinding> LeakageFindings,
    FeatureValidationStatus ValidationStatus, TimeSpan Duration, string ReproducibilityHash);

public interface IFeatureBuildPersistence
{
    Task PersistAsync(int datasetVersionId, int featureSetVersionId, FeatureBuildOutput output, CancellationToken cancellationToken);
}

public sealed class LeakageAuditService
{
    public IReadOnlyList<LeakageFinding> Audit(IReadOnlyList<DrugStateQuarterFeatureRow> rows, IReadOnlyList<FeatureDefinitionDescriptor>? definitions = null)
    {
        var findings = new List<LeakageFinding>();
        foreach (var row in rows)
        {
            var id = $"{row.GenericLaunchId}:{row.StateId}:{row.ObservationQuarterId}";
            if (row.AvailableAsOfQuarterId > row.ObservationQuarterId) findings.Add(new("FutureAvailableAsOf", LeakageSeverity.Blocking, null, id, "Feature availability is after the observation quarter.", "Recompute using only information available at observation time."));
        }
        foreach (var duplicate in rows.GroupBy(x => new { x.GenericLaunchId, x.StateId, x.ObservationQuarterId }).Where(x => x.Count() > 1))
            findings.Add(new("DuplicateAnalyticalKey", LeakageSeverity.Blocking, null, $"{duplicate.Key.GenericLaunchId}:{duplicate.Key.StateId}:{duplicate.Key.ObservationQuarterId}", "Duplicate drug-state-quarter analytical key.", "Deduplicate at the validated input boundary."));
        if (definitions is not null)
        {
            foreach (var definition in definitions)
            {
                if (definition.IsModelInput && definition.IsLabel) findings.Add(new("InputIsLabel", LeakageSeverity.Blocking, definition.Name, "definition", "Feature is marked as both input and label.", "Choose exactly one role."));
                if (definition.IsModelInput && definition.LeakageRisk == LeakageRisk.Prohibited) findings.Add(new("ProhibitedModelInput", LeakageSeverity.Blocking, definition.Name, "definition", "Prohibited feature is marked as a model input.", "Remove it from model inputs."));
            }
        }
        return findings;
    }
}

public sealed record FeatureDefinitionDescriptor(string Name, FeatureDataType DataType, FeatureCategory Category, LeakageRisk LeakageRisk, bool IsModelInput, bool IsLabel, string AvailableAsOfRule, string MissingValuePolicy);

public sealed class FeatureBuildService(IFeatureBuildPersistence persistence, LeakageAuditService leakageAudit)
{
    public async Task<FeatureBuildResult> BuildAsync(FeatureBuildRequest request, IReadOnlyCollection<FeatureObservation> observations, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        Validate(request);
        cancellationToken.ThrowIfCancellationRequested();
        var warnings = new List<string>();
        var errors = new List<string>();
        var summaries = BuildSummaries(request, observations);
        var rows = BuildRows(request, observations, summaries);
        var stateProfiles = BuildStateProfiles(observations);
        var regionalProfiles = BuildRegionalProfiles(observations);
        var findings = leakageAudit.Audit(rows);
        var blocking = findings.Any(x => x.Severity == LeakageSeverity.Blocking);
        if (blocking) errors.Add("Blocking leakage findings prevent persistence.");
        var output = new FeatureBuildOutput(rows, summaries, stateProfiles, regionalProfiles);
        if (!request.DryRun && !blocking) await persistence.PersistAsync(request.DatasetVersionId, request.FeatureSetVersionId, output, cancellationToken);
        var labels = rows.Count(x => x.LabelNextQuarterEntry.HasValue) + rows.Count(x => x.LabelFutureQ4NumericDistribution.HasValue);
        var censored = rows.Count(x => !x.LabelNextQuarterEntry.HasValue) + rows.Count(x => !x.LabelFutureQ4NumericDistribution.HasValue);
        return new(request.DatasetVersionId, request.FeatureSetVersionId, request.DryRun,
            observations.Select(x => x.GenericLaunchId).Distinct().Count(), summaries.Select(x => x.GenericLaunchId).Distinct().Count(), 0,
            rows.Count, summaries.Count, labels, censored,
            new Dictionary<string, int> { ["PrescriptionCount"] = rows.Count(x => !x.PrescriptionCount.HasValue), ["Lag1PrescriptionCount"] = rows.Count(x => !x.Lag1PrescriptionCount.HasValue) },
            warnings, errors, findings, blocking ? FeatureValidationStatus.Failed : FeatureValidationStatus.Passed,
            DateTime.UtcNow - started, Hash(output));
    }

    private static List<LaunchQuarterSummaryRow> BuildSummaries(FeatureBuildRequest request, IReadOnlyCollection<FeatureObservation> observations)
    {
        var result = new List<LaunchQuarterSummaryRow>();
        foreach (var group in observations.Where(x => x.ObservationQuarter >= request.ObservationStartQuarter && x.ObservationQuarter <= request.ObservationEndQuarter).GroupBy(x => new { x.GenericLaunchId, x.DrugId, x.ObservationQuarter, x.ObservationQuarterId, x.ApprovalQuarter }))
        {
            var eligible = group.Where(x => x.IsEligibleState).ToArray();
            if (eligible.Length == 0) continue;
            var entry = eligible.Select(x => (Observation: x, Result: request.StateEntryPolicy.Evaluate(new(x.PrescriptionCount, x.ReimbursementAmount, x.IsSuppressed), false))).ToArray();
            var activeCount = entry.Count(x => x.Result.IsPresent);
            var nd = DistributionMetrics.NumericDistribution(activeCount, eligible.Length);
            var wd = DistributionMetrics.WeightedDistribution(entry.Select(x => new WeightedStateObservation(x.Result.IsPresent, (double)x.Observation.FrozenMarketWeight)));
            var gap = DistributionMetrics.AccessGap(wd, nd);
            var concentration = FeatureCalculations.Concentration(eligible.Select(x => (decimal)(x.PrescriptionCount ?? 0)));
            result.Add(new(group.Key.GenericLaunchId, group.Key.DrugId, group.Key.ObservationQuarterId, group.Key.ApprovalQuarter.DistanceTo(group.Key.ObservationQuarter), activeCount, eligible.Length, (decimal)nd.Value, (decimal)wd.Value, (decimal)gap.Value, request.MarketWeightVersion, eligible.Sum(x => x.PrescriptionCount ?? 0), eligible.Sum(x => x.ReimbursementAmount ?? 0), concentration.ConcentrationIndex, concentration.TopStateShare, concentration.TopFiveStateShare, eligible.All(x => x.PrescriptionCount.HasValue || x.IsSuppressed)));
        }
        return result.OrderBy(x => x.GenericLaunchId).ThenBy(x => x.ObservationQuarterId).ToList();
    }

    private static List<DrugStateQuarterFeatureRow> BuildRows(FeatureBuildRequest request, IReadOnlyCollection<FeatureObservation> observations, IReadOnlyList<LaunchQuarterSummaryRow> summaries)
    {
        var result = new List<DrugStateQuarterFeatureRow>();
        foreach (var stateSeries in observations.GroupBy(x => new { x.GenericLaunchId, x.DrugId, x.StateId }).OrderBy(x => x.Key.GenericLaunchId).ThenBy(x => x.Key.StateId))
        {
            var ordered = stateSeries.OrderBy(x => x.ObservationQuarter).ToArray();
            var previouslyPresent = false;
            for (var i = 0; i < ordered.Length; i++)
            {
                var current = ordered[i];
                var entry = request.StateEntryPolicy.Evaluate(new(current.PrescriptionCount, current.ReimbursementAmount, current.IsSuppressed), previouslyPresent);
                var next = i + 1 < ordered.Length && current.ObservationQuarter.AddQuarters(1) == ordered[i + 1].ObservationQuarter ? ordered[i + 1] : null;
                bool? nextLabel = next is null || previouslyPresent || entry.IsPresent ? null : request.StateEntryPolicy.Evaluate(new(next.PrescriptionCount, next.ReimbursementAmount, next.IsSuppressed), false).IsPresent;
                var futureEntry = ordered.Skip(i + 1).FirstOrDefault(x => request.StateEntryPolicy.Evaluate(new(x.PrescriptionCount, x.ReimbursementAmount, x.IsSuppressed), false).IsPresent);
                int? quartersUntilEntry = futureEntry is null ? null : current.ObservationQuarter.DistanceTo(futureEntry.ObservationQuarter);
                var q4Summary = summaries.FirstOrDefault(x => x.GenericLaunchId == current.GenericLaunchId && x.QuarterSinceApproval == request.Q4Horizon);
                var q4Observed = q4Summary is not null && current.ObservationQuarter.DistanceTo(ordered[^1].ObservationQuarter) >= request.Q4Horizon - current.ApprovalQuarter.DistanceTo(current.ObservationQuarter);
                var lag1 = i > 0 && ordered[i - 1].ObservationQuarter.AddQuarters(1) == current.ObservationQuarter ? ordered[i - 1].PrescriptionCount : null;
                var lag2 = i > 1 && ordered[i - 2].ObservationQuarter.AddQuarters(2) == current.ObservationQuarter ? ordered[i - 2].PrescriptionCount : null;
                var growth = FeatureCalculations.OrdinaryGrowth(current.PrescriptionCount, lag1);
                var launchGaps = summaries.Where(x => x.GenericLaunchId == current.GenericLaunchId && x.QuarterSinceApproval <= request.Q4Horizon).OrderBy(x => x.QuarterSinceApproval).Select(x => x.AccessGap);
                bool? persistent = q4Observed ? FeatureCalculations.PersistentInequality(launchGaps, request.PersistentInequalityThreshold, request.PersistentInequalityConsecutiveQuarters) : null;
                result.Add(new(current.GenericLaunchId, current.DrugId, current.StateId, current.ObservationQuarterId, current.ApprovalQuarterId, current.ApprovalQuarter.DistanceTo(current.ObservationQuarter), current.ObservationQuarterId, current.PrescriptionCount, current.ReimbursementAmount, entry.IsPresent, entry.IsFirstEntry, !current.PrescriptionCount.HasValue && !current.IsSuppressed, current.IsSuppressed, lag1, lag2, growth.Value, nextLabel, quartersUntilEntry, q4Observed ? q4Summary!.NumericDistribution : null, q4Observed ? q4Summary!.WeightedDistribution : null, q4Observed ? q4Summary!.AccessGap : null, persistent));
                previouslyPresent |= entry.IsPresent;
            }
        }
        return result;
    }

    private static List<StateHistoricalProfileRow> BuildStateProfiles(IReadOnlyCollection<FeatureObservation> observations) => observations
        .Select(x => new { x.StateId, x.ObservationQuarter, x.ObservationQuarterId })
        .Distinct()
        .Select(cutoff =>
        {
            var history = observations.Where(x => x.StateId == cutoff.StateId && x.ObservationQuarter < cutoff.ObservationQuarter).ToArray();
            var launches = history.Select(x => x.GenericLaunchId).Distinct().Count();
            var entered = history.Where(x => x.PrescriptionCount > 0 && !x.IsSuppressed).Select(x => x.GenericLaunchId).Distinct().Count();
            var observed = history.Count(x => x.PrescriptionCount.HasValue && !x.IsSuppressed);
            return new StateHistoricalProfileRow(cutoff.StateId, cutoff.ObservationQuarterId, history.Sum(x => x.PrescriptionCount ?? 0), launches, launches == 0 ? 0 : (decimal)entered / launches, null, history.Select(x => x.FrozenMarketWeight).DefaultIfEmpty(0).First(), history.Length == 0 ? 0 : (decimal)observed / history.Length);
        }).OrderBy(x => x.StateId).ThenBy(x => x.AvailableAsOfQuarterId).ToList();

    private static List<RegionalHistoricalProfileRow> BuildRegionalProfiles(IReadOnlyCollection<FeatureObservation> observations) => observations
        .Select(x => new { x.Region, x.ObservationQuarter, x.ObservationQuarterId }).Distinct()
        .Select(cutoff =>
        {
            var history = observations.Where(x => x.Region == cutoff.Region && x.ObservationQuarter < cutoff.ObservationQuarter && x.IsEligibleState).ToArray();
            var observed = history.Where(x => x.PrescriptionCount.HasValue && !x.IsSuppressed).ToArray();
            var active = observed.Count(x => x.PrescriptionCount > 0);
            return new RegionalHistoricalProfileRow(cutoff.Region, cutoff.ObservationQuarterId, observed.Length == 0 ? 0 : (decimal)active / observed.Length, observed.Length == 0 ? 0 : (decimal)active / observed.Length, null, history.Select(x => x.StateId).Distinct().Count());
        }).OrderBy(x => x.Region).ThenBy(x => x.AvailableAsOfQuarterId).ToList();

    private static void Validate(FeatureBuildRequest request)
    {
        if (request.DatasetVersionId <= 0 || request.FeatureSetVersionId <= 0) throw new ArgumentOutOfRangeException(nameof(request));
        if (request.DatasetStatus is DatasetVersionStatus.Rejected or DatasetVersionStatus.Archived or DatasetVersionStatus.Draft or DatasetVersionStatus.Validating) throw new InvalidOperationException("Dataset version must be validated or finalized and not rejected or archived.");
        if (request.FeatureSetStatus is FeatureSetStatus.Finalized or FeatureSetStatus.Archived or FeatureSetStatus.Rejected) throw new InvalidOperationException("Feature set is immutable or rejected.");
        if (request.ObservationEndQuarter < request.ObservationStartQuarter || request.MarketWeightWindowEnd >= request.ObservationStartQuarter) throw new ArgumentException("Observation and frozen-weight windows are not leakage-safe.");
        if (request.BatchSize <= 0 || request.ErrorLimit <= 0 || request.Q4Horizon < 0) throw new ArgumentOutOfRangeException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.CorrelationId) || string.IsNullOrWhiteSpace(request.MarketWeightVersion)) throw new ArgumentException("Correlation and market-weight version are required.");
    }

    private static string Hash(FeatureBuildOutput output)
    {
        var canonical = string.Join('\n', output.FeatureRows.OrderBy(x => x.GenericLaunchId).ThenBy(x => x.StateId).ThenBy(x => x.ObservationQuarterId).Select(x => string.Join('|', x.GenericLaunchId, x.StateId, x.ObservationQuarterId, x.PrescriptionCount?.ToString(CultureInfo.InvariantCulture) ?? "null", x.IsPresent, x.LabelNextQuarterEntry?.ToString() ?? "null")));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
