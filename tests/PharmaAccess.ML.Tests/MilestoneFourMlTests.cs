using Microsoft.ML;
using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.ML.Tests;

public sealed class MilestoneFourMlTests
{
    [Fact] public void Feature_schema_is_ordered_deterministic_and_rejects_labels() { var first = NextQuarterFeatureSchema.Create(); var second = NextQuarterFeatureSchema.Create(); Assert.Equal(first.OrderedFeatures, second.OrderedFeatures); Assert.Equal(first.SchemaHash, second.SchemaHash); Assert.Throws<InvalidOperationException>(() => NextQuarterFeatureSchema.Create(["LabelNextQuarterEntry"])); }

    [Fact]
    public void Dataset_builder_excludes_censoring_and_preserves_launch_groups()
    {
        var rows = Synthetic().Append(Row(999, 1, 1, null)).ToArray(); var split = new TemporalTrainingDatasetBuilder().Build(rows, Split(), 1000);
        Assert.Equal(1, split.CensoredRows); Assert.All(split.Manifest.GroupBy(x => x.GenericLaunchId), group => Assert.Single(group.Select(x => x.Partition).Distinct())); Assert.Equal(split.DatasetHash, new TemporalTrainingDatasetBuilder().Build(rows, Split(), 1000).DatasetHash);
    }

    [Fact] public void Split_rejects_nonchronological_configuration() => Assert.Throws<ArgumentException>(() => new TemporalTrainingDatasetBuilder().Build(Synthetic(), Split() with { TestStartCohort = 5 }, 1000));

    [Fact]
    public void Baselines_and_custom_metrics_are_correct()
    {
        var predictions = new[] { new BinaryPrediction(true, 0, .9f, true), new BinaryPrediction(false, 0, .1f, false), new BinaryPrediction(true, 0, .8f, true), new BinaryPrediction(false, 0, .2f, false) };
        var metrics = BinaryMetricCalculator.Calculate(predictions, .5, .5); Assert.Equal(1, metrics.Precision); Assert.Equal(1, metrics.Recall); Assert.Equal(1, metrics.F1); Assert.Equal(.025, metrics.BrierScore, 5); Assert.Equal(1, metrics.PrAuc);
        Assert.All(BinaryMetricCalculator.PrevalenceBaseline(Synthetic().Take(2), .25, .5), x => Assert.Equal(.25f, x.Probability));
        Assert.Equal(.2f, BinaryMetricCalculator.HistoricalStateRateBaseline([Row(1, 1, 1, false) with { StateHistoricalEntryRate = .2f }], .5, .5).Single().Probability);
    }

    [Fact]
    public async Task All_required_trainers_run_and_candidate_artifact_round_trips()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("PHARMAACCESS_ML_ARTIFACT_ROOT"); var root = configuredRoot ?? Path.Combine(Path.GetTempPath(), $"pharmaaccess-ml-{Guid.NewGuid():N}");
        try
        {
            var service = new NextQuarterStateEntryTrainingService(new FileSystemModelArtifactStore(root)); var request = new TrainNextQuarterStateEntryRequest("synthetic-v1", 1, 1, NextQuarterFeatureSchema.Version, Split(), [TrainerKind.LogisticRegression, TrainerKind.FastTree, TrainerKind.FastForest, TrainerKind.LightGbm], 17, .5, false, false, "test");
            var result = await service.TrainAsync(request, Synthetic());
            Assert.Equal(2, result.Baselines.Count); Assert.Equal(4, result.Candidates.Count); Assert.NotNull(result.Selected); Assert.Equal(ModelApprovalStatus.ValidationSelected, result.Selected!.ApprovalStatus); Assert.DoesNotContain(result.Candidates, x => x.ApprovalStatus == ModelApprovalStatus.Approved); Assert.True(File.Exists(result.ArtifactPath)); Assert.True(File.Exists(result.ModelCardPath)); Assert.True(File.Exists(result.ReproducibilityManifestPath)); Assert.All(result.Candidates, x => Assert.InRange(x.ValidationMetrics.PrAuc, 0, 1));
            var manifest = Directory.GetFiles(Path.GetDirectoryName(result.ArtifactPath)!, "manifest.json").Single(); var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(result.ArtifactPath!))); var stored = new StoredArtifact(result.Selected.ArtifactVersion!, result.ArtifactPath!, manifest, hash, new FileInfo(result.ArtifactPath!).Length, result.Dataset.FeatureSelection.SchemaHash); var ml = new MLContext(); var loaded = new FileSystemModelArtifactStore(root).Load(ml, stored, result.Dataset.FeatureSelection.SchemaHash); Assert.NotNull(loaded.Model);
            var descriptor = new SelectedModelArtifact(stored.Version, stored.ModelPath, stored.ManifestPath, stored.Sha256, stored.FileSize, stored.SchemaHash, ModelApprovalStatus.ValidationSelected, 1, 1, .5); var predictionService = new NextStateEntryPredictionService(new FileSystemModelArtifactStore(root), new FakeRegistry(descriptor), new FakeResolver(Synthetic()[0])); var scored1 = await predictionService.PredictAsync(new(null, Synthetic()[0].FeatureRowId, 1)); var scored2 = await predictionService.PredictAsync(new(null, Synthetic()[0].FeatureRowId, 1)); Assert.NotNull(scored1); Assert.Equal(stored.Version, scored1.ModelVersion); Assert.Equal(scored1.Probability, scored2!.Probability, .000001f);
            var corruptPath = Path.Combine(Path.GetDirectoryName(result.ArtifactPath!)!, "corrupted-copy.zip"); File.Copy(result.ArtifactPath!, corruptPath); File.AppendAllText(corruptPath, "corruption"); var corrupt = stored with { ModelPath = corruptPath }; Assert.Throws<InvalidDataException>(() => new FileSystemModelArtifactStore(root).Load(ml, corrupt, result.Dataset.FeatureSelection.SchemaHash)); File.Delete(corruptPath);
            if (configuredRoot is not null) File.WriteAllText(Path.Combine(root, "synthetic-training-summary.json"), System.Text.Json.JsonSerializer.Serialize(new { notice = "Synthetic development data — not research results", rows = Synthetic().Length, trainingRows = result.Dataset.Training.Count, validationRows = result.Dataset.Validation.Count, testRows = result.Dataset.Test.Count, selected = result.Selected.Trainer.ToString(), validation = result.Selected.ValidationMetrics, test = result.Selected.TestMetrics, artifact = result.ArtifactPath }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        finally { if (configuredRoot is null && Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static TemporalGroupedSplitConfiguration Split() => new(3, 4, 5, 6, 7);
    private static NextQuarterTrainingRow[] Synthetic() => Enumerable.Range(1, 7).SelectMany(launch => Enumerable.Range(1, 8).Select(state => Row(launch * 100 + state, launch, launch, state % 3 == 0))).ToArray();
    private static NextQuarterTrainingRow Row(long id, int launch, int cohort, bool? label)
    {
        var value = (float)((id % 11) + 1); var missing = id % 13 == 0 ? float.NaN : value;
        return new(id, launch, launch, (int)(id % 8) + 1, cohort * 10, cohort, true, false, false, true, false, label,
            cohort, cohort, 0, 0, missing, value, missing, value, missing, 0, value, value * 2, value, value, 0,
            value * 10, cohort, label == true ? .4f : .15f, missing, value, .5f, .9f, .3f, .2f, .1f, .2f, 0, 0, float.IsNaN(missing) ? 1 : 0, true, float.IsNaN(missing), false);
    }

    private sealed class FakeRegistry(SelectedModelArtifact artifact) : IModelArtifactRegistry { public Task<SelectedModelArtifact?> ResolveAsync(string? modelVersion, CancellationToken cancellationToken) => Task.FromResult<SelectedModelArtifact?>(artifact); }
    private sealed class FakeResolver(NextQuarterTrainingRow row) : ITrainingFeatureRowResolver { public Task<NextQuarterTrainingRow?> ResolveAsync(long featureRowId, int featureSetVersionId, CancellationToken cancellationToken) => Task.FromResult<NextQuarterTrainingRow?>(featureRowId == row.FeatureRowId ? row : null); }
}
