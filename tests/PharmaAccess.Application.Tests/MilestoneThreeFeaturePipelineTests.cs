using PharmaAccess.Application.Features;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Features;
using PharmaAccess.Domain.ValueObjects;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class MilestoneThreeFeaturePipelineTests
{
    [Fact]
    public async Task Synthetic_pipeline_is_deterministic_and_dry_run_persists_nothing()
    {
        var persistence = new FakePersistence(); var service = new FeatureBuildService(persistence, new LeakageAuditService()); var request = Request(true);
        var first = await service.BuildAsync(request, Synthetic()); var second = await service.BuildAsync(request, Synthetic());
        Assert.Equal(32, first.StateQuarterRowsGenerated); Assert.Equal(8, first.LaunchQuarterSummariesGenerated); Assert.Equal(first.ReproducibilityHash, second.ReproducibilityHash); Assert.Equal(0, persistence.Calls); Assert.Empty(first.Errors);
    }

    [Fact]
    public async Task Complete_launch_has_q4_labels_and_recent_launch_is_censored()
    {
        var result = await new FeatureBuildService(new FakePersistence(), new LeakageAuditService()).BuildAsync(Request(true), Synthetic());
        var complete = result.CensoredLabels;
        Assert.True(result.LabelsGenerated > 0); Assert.True(complete > 0);
    }

    [Fact]
    public async Task Next_quarter_entry_label_detects_first_entry()
    {
        var service = new FeatureBuildService(new FakePersistence(), new LeakageAuditService());
        var output = await service.BuildAsync(Request(true), Synthetic());
        Assert.True(output.LabelsGenerated > 0);
    }

    [Fact]
    public async Task Non_dry_run_persists_when_audit_passes()
    {
        var persistence = new FakePersistence(); await new FeatureBuildService(persistence, new LeakageAuditService()).BuildAsync(Request(false), Synthetic()); Assert.Equal(1, persistence.Calls); Assert.NotEmpty(persistence.Output!.StateProfiles); Assert.NotEmpty(persistence.Output.RegionalProfiles);
    }

    [Fact]
    public async Task Invalid_dataset_status_is_rejected() => await Assert.ThrowsAsync<InvalidOperationException>(() => new FeatureBuildService(new FakePersistence(), new LeakageAuditService()).BuildAsync(Request(true) with { DatasetStatus = DatasetVersionStatus.Archived }, Synthetic()));

    [Fact]
    public async Task Future_market_weight_window_is_rejected() => await Assert.ThrowsAsync<ArgumentException>(() => new FeatureBuildService(new FakePersistence(), new LeakageAuditService()).BuildAsync(Request(true) with { MarketWeightWindowEnd = new(2025, 1) }, Synthetic()));

    [Fact]
    public void Leakage_audit_blocks_duplicate_keys_and_prohibited_inputs()
    {
        var row = new DrugStateQuarterFeatureRow(1, 1, 1, 1, 1, 0, 1, 0, 0, false, false, false, false, null, null, null, null, null, null, null, null, null);
        var findings = new LeakageAuditService().Audit([row, row], [new("Outcome", FeatureDataType.Double, FeatureCategory.Label, LeakageRisk.Prohibited, true, true, "future", "null")]);
        Assert.Contains(findings, x => x.Code == "DuplicateAnalyticalKey"); Assert.Contains(findings, x => x.Code == "InputIsLabel"); Assert.Contains(findings, x => x.Code == "ProhibitedModelInput");
    }

    [Fact]
    public async Task Cancellation_is_honored()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new FeatureBuildService(new FakePersistence(), new LeakageAuditService()).BuildAsync(Request(true), Synthetic(), cts.Token));
    }

    [Fact]
    public void Eligibility_adjacency_and_similarity_are_policy_driven()
    {
        var decisions = new EligibleStateResolver(0.8m).Resolve([new(1, "PR", true, null, 0.9m), new(2, "CA", false, "Configured exclusion", 1m)]);
        Assert.True(decisions[0].Included); Assert.False(decisions[1].Included);
        Assert.Equal([2, 3], new ConfiguredStateAdjacencyProvider(new Dictionary<int, IReadOnlyCollection<int>> { [1] = [3, 2] }).GetNeighbors(1));
        var similar = new HistoricalStateSimilarityService().FindSimilar(1, [new(1, [1m, 1m]), new(2, [1.1m, 1m]), new(3, [9m, 9m])], 1, 2);
        Assert.Equal([2], similar.StateIds);
    }

    private static FeatureBuildRequest Request(bool dryRun) => new(1, 1, DatasetVersionStatus.Finalized, FeatureSetStatus.Draft, new(2025, 1), new(2026, 1), new AnyPositiveUtilizationPolicy(), "State.IsEligible and observed completeness", "synthetic-2024", new(2024, 4), 4, -5m, 2, dryRun, "synthetic-test");

    private static IReadOnlyCollection<FeatureObservation> Synthetic()
    {
        var rows = new List<FeatureObservation>();
        for (var launch = 1; launch <= 2; launch++)
        for (var q = 0; q < (launch == 1 ? 5 : 3); q++)
        for (var state = 1; state <= 4; state++)
        {
            var prescriptions = state == 4 && q == 2 ? (long?)null : q >= state - 1 ? 10L * state + q : 0;
            rows.Add(new(launch, launch, state, state <= 2 ? "North" : "South", new(2025, 1), 1, new CalendarQuarter(2025, 1).AddQuarters(q), q + 1, state != 4, prescriptions, prescriptions * 2m, state == 3 && q == 1, state));
        }
        return rows;
    }

    private sealed class FakePersistence : IFeatureBuildPersistence
    {
        public int Calls { get; private set; }
        public FeatureBuildOutput? Output { get; private set; }
        public Task PersistAsync(int datasetVersionId, int featureSetVersionId, FeatureBuildOutput output, CancellationToken cancellationToken) { Calls++; Output = output; return Task.CompletedTask; }
    }
}
