using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public static class NextQuarterFeatureSchema
{
    public const string Version = "next-entry-features-v1";
    public static readonly string[] OrderedFeatures =
    [
        nameof(ModelInput.QuarterSinceApproval), nameof(ModelInput.NumberOfObservedQuarters),
        nameof(ModelInput.ObservedPrescriptionCount), nameof(ModelInput.ObservedReimbursementAmount),
        nameof(ModelInput.Lag1PrescriptionCount), nameof(ModelInput.Lag2PrescriptionCount),
        nameof(ModelInput.Lag1ReimbursementAmount), nameof(ModelInput.Lag2ReimbursementAmount),
        nameof(ModelInput.PrescriptionGrowthRate), nameof(ModelInput.ReimbursementGrowthRate),
        nameof(ModelInput.InitialActiveStateCount), nameof(ModelInput.InitialPrescriptionVolume),
        nameof(ModelInput.PreviousQuarterNumericDistribution), nameof(ModelInput.PreviousQuarterWeightedDistribution),
        nameof(ModelInput.PreviousQuarterAccessGap), nameof(ModelInput.StateHistoricalGenericVolume),
        nameof(ModelInput.StateHistoricalLaunchCount), nameof(ModelInput.StateHistoricalEntryRate),
        nameof(ModelInput.StateHistoricalMedianEntryDelay), nameof(ModelInput.StateHistoricalMarketWeight),
        nameof(ModelInput.StateVolumePercentile), nameof(ModelInput.StateDataCompleteness),
        nameof(ModelInput.RegionActiveStateShare), nameof(ModelInput.RegionHistoricalEntryRate),
        nameof(ModelInput.NeighborStateAdoptionShare), nameof(ModelInput.SimilarStateAdoptionShare),
        nameof(ModelInput.RegionPrescriptionGrowth), nameof(ModelInput.NationalPrescriptionGrowth),
        nameof(ModelInput.MissingFeatureCount), nameof(ModelInput.IsObservedZero), nameof(ModelInput.IsMissing), nameof(ModelInput.IsSuppressed)
    ];

    private static readonly HashSet<string> Prohibited = new(StringComparer.Ordinal)
    {
        "Label", "LabelNextQuarterEntry", "LabelFutureQ4NumericDistribution", "LabelFutureQ4WeightedDistribution",
        "LabelFutureQ4AccessGap", "LabelPersistentInequality", "DatasetVersionId", "FeatureSetVersionId",
        "FeatureRowId", "GenericLaunchId", "DrugId", "StateId", "ObservationQuarterId", "AvailableAsOfQuarterId"
    };

    public static FeatureSelectionSpecification Create(IEnumerable<string>? features = null)
    {
        var ordered = (features ?? OrderedFeatures).ToArray();
        if (ordered.Length == 0 || ordered.Distinct(StringComparer.Ordinal).Count() != ordered.Length) throw new ArgumentException("Feature order must be nonempty and unique.");
        if (ordered.Any(Prohibited.Contains)) throw new InvalidOperationException("A prohibited identifier or label was selected as an input feature.");
        if (ordered.Any(x => !OrderedFeatures.Contains(x, StringComparer.Ordinal))) throw new InvalidOperationException("Feature is absent from the versioned metadata catalog.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', ordered))));
        return new(Version, ordered, hash);
    }
}

public sealed class TemporalTrainingDatasetBuilder
{
    public TrainingDataset Build(IReadOnlyCollection<NextQuarterTrainingRow> source, TemporalGroupedSplitConfiguration split, int maximumRows)
    {
        if (source.Count > maximumRows || maximumRows <= 0) throw new InvalidOperationException("Configured local training row limit exceeded.");
        ValidateSplit(split);
        var excluded = new Dictionary<string, int>(StringComparer.Ordinal);
        var eligible = new List<NextQuarterTrainingRow>();
        var censored = 0;
        foreach (var row in source.OrderBy(x => x.GenericLaunchId).ThenBy(x => x.StateId).ThenBy(x => x.ObservationQuarterId))
        {
            string? reason = row.LabelNextQuarterEntry is null ? "CensoredLabel" : !row.FeatureSetValidated ? "FeatureSetNotValidated" : row.HasBlockingQualityIssue ? "BlockingQuality" : row.HasBlockingLeakageFinding ? "BlockingLeakage" : !row.IsEligibleState ? "IneligibleState" : row.WasAlreadyPresent ? "AlreadyEntered" : null;
            if (reason is not null) { excluded[reason] = excluded.GetValueOrDefault(reason) + 1; if (reason == "CensoredLabel") censored++; continue; }
            eligible.Add(row);
        }
        var launchPartitions = eligible.GroupBy(x => x.GenericLaunchId).ToDictionary(x => x.Key, group => PartitionFor(group.Select(x => x.LaunchCohortOrdinal).Distinct().Single(), split));
        var manifest = eligible.Select(x => new SplitMembership(x.FeatureRowId, x.GenericLaunchId, launchPartitions[x.GenericLaunchId])).ToArray();
        var train = eligible.Where(x => launchPartitions[x.GenericLaunchId] == MlPartition.Training).ToArray();
        var validation = eligible.Where(x => launchPartitions[x.GenericLaunchId] == MlPartition.Validation).ToArray();
        var test = eligible.Where(x => launchPartitions[x.GenericLaunchId] == MlPartition.Test).ToArray();
        ValidateCounts(train, split, "training"); ValidateCounts(validation, split, "validation"); ValidateCounts(test, split, "test");
        if (manifest.GroupBy(x => x.GenericLaunchId).Any(x => x.Select(y => y.Partition).Distinct().Count() != 1)) throw new InvalidOperationException("Launch group crosses partitions.");
        var schema = NextQuarterFeatureSchema.Create();
        return new(train, validation, test, schema, manifest, excluded, censored, HashRows(eligible), HashManifest(manifest));
    }

    private static void ValidateSplit(TemporalGroupedSplitConfiguration x)
    {
        if (x.GroupingKey != "GenericLaunchId") throw new NotSupportedException("Only GenericLaunchId grouping is supported in the primary split.");
        if (x.TrainingEndCohort >= x.ValidationStartCohort || x.ValidationStartCohort > x.ValidationEndCohort || x.ValidationEndCohort >= x.TestStartCohort || x.TestStartCohort > x.TestEndCohort) throw new ArgumentException("Primary partitions must be chronological and nonoverlapping.");
    }
    private static MlPartition PartitionFor(int cohort, TemporalGroupedSplitConfiguration x) => cohort <= x.TrainingEndCohort ? MlPartition.Training : cohort >= x.ValidationStartCohort && cohort <= x.ValidationEndCohort ? MlPartition.Validation : cohort >= x.TestStartCohort && cohort <= x.TestEndCohort ? MlPartition.Test : throw new InvalidOperationException($"Launch cohort {cohort} is outside the configured split.");
    private static void ValidateCounts(IReadOnlyCollection<NextQuarterTrainingRow> rows, TemporalGroupedSplitConfiguration split, string name) { var positive = rows.Count(x => x.LabelNextQuarterEntry == true); var negative = rows.Count - positive; if (positive < split.MinimumPositiveCount || negative < split.MinimumNegativeCount) throw new InvalidOperationException($"{name} partition has insufficient class counts."); }
    private static string HashRows(IEnumerable<NextQuarterTrainingRow> rows) => Hash(string.Join('\n', rows.Select(x => $"{x.FeatureRowId}|{x.GenericLaunchId}|{x.StateId}|{x.ObservationQuarterId}|{x.LabelNextQuarterEntry}")));
    private static string HashManifest(IEnumerable<SplitMembership> rows) => Hash(string.Join('\n', rows.OrderBy(x => x.FeatureRowId).Select(x => $"{x.FeatureRowId}|{x.GenericLaunchId}|{x.Partition}")));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

public static class BinaryMetricCalculator
{
    public static ClassificationMetrics Calculate(IReadOnlyList<BinaryPrediction> predictions, double threshold, double trainingPrevalence)
    {
        if (predictions.Count == 0) throw new ArgumentException("Predictions are required.");
        var scored = predictions.Select(x => (x.Label, Probability: Math.Clamp((double)x.Probability, 1e-15, 1 - 1e-15), Predicted: x.Probability >= threshold)).ToArray();
        long tp = scored.Count(x => x.Label && x.Predicted), tn = scored.Count(x => !x.Label && !x.Predicted), fp = scored.Count(x => !x.Label && x.Predicted), fn = scored.Count(x => x.Label && !x.Predicted);
        var prevalence = scored.Average(x => x.Label ? 1d : 0d); var predictedRate = scored.Average(x => x.Predicted ? 1d : 0d);
        var precision = Divide(tp, tp + fp); var recall = Divide(tp, tp + fn); var specificity = Divide(tn, tn + fp); var accuracy = Divide(tp + tn, scored.Length); var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
        var logLoss = -scored.Average(x => x.Label ? Math.Log(x.Probability) : Math.Log(1 - x.Probability));
        var baselineLoss = -(prevalence * Math.Log(Math.Clamp(trainingPrevalence, 1e-15, 1 - 1e-15)) + (1 - prevalence) * Math.Log(Math.Clamp(1 - trainingPrevalence, 1e-15, 1 - 1e-15)));
        var brier = scored.Average(x => Math.Pow(x.Probability - (x.Label ? 1 : 0), 2));
        return new(RocAuc(scored), PrAuc(scored), logLoss, baselineLoss == 0 ? 0 : 1 - logLoss / baselineLoss, accuracy, precision, recall, f1, specificity, tp, tn, fp, fn, prevalence, predictedRate, brier, threshold);
    }
    public static IReadOnlyList<BinaryPrediction> PrevalenceBaseline(IEnumerable<NextQuarterTrainingRow> target, double trainingPrevalence, double threshold) => target.Select(x => new BinaryPrediction(x.LabelNextQuarterEntry!.Value, 0, (float)trainingPrevalence, trainingPrevalence >= threshold)).ToArray();
    public static IReadOnlyList<BinaryPrediction> HistoricalStateRateBaseline(IEnumerable<NextQuarterTrainingRow> target, double trainingPrevalence, double threshold) => target.Select(x => { var p = float.IsNaN(x.StateHistoricalEntryRate) ? (float)trainingPrevalence : Math.Clamp(x.StateHistoricalEntryRate, 0, 1); return new BinaryPrediction(x.LabelNextQuarterEntry!.Value, 0, p, p >= threshold); }).ToArray();
    private static double Divide(long n, long d) => d == 0 ? 0 : (double)n / d;
    private static double RocAuc((bool Label, double Probability, bool Predicted)[] values) { var positives = values.Count(x => x.Label); var negatives = values.Length - positives; if (positives == 0 || negatives == 0) return 0.5; var ranked = values.OrderBy(x => x.Probability).Select((x, i) => (x.Label, Rank: i + 1d)).ToArray(); var rankSum = ranked.Where(x => x.Label).Sum(x => x.Rank); return (rankSum - positives * (positives + 1) / 2d) / (positives * negatives); }
    private static double PrAuc((bool Label, double Probability, bool Predicted)[] values) { var positives = values.Count(x => x.Label); if (positives == 0) return 0; var ordered = values.OrderByDescending(x => x.Probability).ToArray(); double area = 0, previousRecall = 0; int tp = 0, fp = 0; foreach (var item in ordered) { if (item.Label) tp++; else fp++; var recall = (double)tp / positives; var precision = (double)tp / (tp + fp); area += (recall - previousRecall) * precision; previousRecall = recall; } return area; }
}

public sealed class ModelInput
{
    public bool Label { get; set; }
    public float QuarterSinceApproval { get; set; } public float NumberOfObservedQuarters { get; set; }
    public float ObservedPrescriptionCount { get; set; } public float ObservedReimbursementAmount { get; set; }
    public float Lag1PrescriptionCount { get; set; } public float Lag2PrescriptionCount { get; set; }
    public float Lag1ReimbursementAmount { get; set; } public float Lag2ReimbursementAmount { get; set; }
    public float PrescriptionGrowthRate { get; set; } public float ReimbursementGrowthRate { get; set; }
    public float InitialActiveStateCount { get; set; } public float InitialPrescriptionVolume { get; set; }
    public float PreviousQuarterNumericDistribution { get; set; } public float PreviousQuarterWeightedDistribution { get; set; } public float PreviousQuarterAccessGap { get; set; }
    public float StateHistoricalGenericVolume { get; set; } public float StateHistoricalLaunchCount { get; set; } public float StateHistoricalEntryRate { get; set; }
    public float StateHistoricalMedianEntryDelay { get; set; } public float StateHistoricalMarketWeight { get; set; } public float StateVolumePercentile { get; set; } public float StateDataCompleteness { get; set; }
    public float RegionActiveStateShare { get; set; } public float RegionHistoricalEntryRate { get; set; } public float NeighborStateAdoptionShare { get; set; } public float SimilarStateAdoptionShare { get; set; }
    public float RegionPrescriptionGrowth { get; set; } public float NationalPrescriptionGrowth { get; set; }
    public float MissingFeatureCount { get; set; } public float IsObservedZero { get; set; } public float IsMissing { get; set; } public float IsSuppressed { get; set; }
}

public sealed class ModelOutput { public bool Label { get; set; } public bool PredictedLabel { get; set; } public float Score { get; set; } [ColumnName("Probability")] public float Probability { get; set; } }
public sealed class ModelOutputWithoutProbability { public bool Label { get; set; } public bool PredictedLabel { get; set; } public float Score { get; set; } }

public sealed record StoredArtifact(string Version, string ModelPath, string ManifestPath, string Sha256, long FileSize, string SchemaHash);

public sealed class FileSystemModelArtifactStore(string root, long maximumArtifactBytes = 100_000_000)
{
    private readonly string _root = Path.GetFullPath(root);
    public StoredArtifact Save(MLContext ml, ITransformer model, DataViewSchema schema, string task, string experiment, int datasetVersion, int featureSetVersion, string trainer, string schemaHash)
    {
        Directory.CreateDirectory(_root); var temp = Path.Combine(_root, $".{Guid.NewGuid():N}.tmp"); ml.Model.Save(model, schema, temp); var info = new FileInfo(temp); if (info.Length > maximumArtifactBytes) { File.Delete(temp); throw new InvalidOperationException("Artifact size limit exceeded."); }
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(temp))); var version = $"{task}-{experiment}-{datasetVersion}-{featureSetVersion}-{trainer}-{hash[..10]}"; var directory = SafeDirectory(version); if (Directory.Exists(directory)) { File.Delete(temp); throw new InvalidOperationException("Immutable model version already exists."); } Directory.CreateDirectory(directory); var modelPath = Path.Combine(directory, "model.zip"); File.Move(temp, modelPath);
        var manifestPath = Path.Combine(directory, "manifest.json"); File.WriteAllText(manifestPath, JsonSerializer.Serialize(new { syntheticDevelopmentData = true, task, experiment, datasetVersion, featureSetVersion, trainer, schemaHash, artifactHash = hash, fileSize = info.Length }, JsonOptions)); return new(version, modelPath, manifestPath, hash, info.Length, schemaHash);
    }
    public (ITransformer Model, DataViewSchema Schema) Load(MLContext ml, StoredArtifact artifact, string expectedSchemaHash) { if (artifact.SchemaHash != expectedSchemaHash) throw new InvalidOperationException("Input feature schema mismatch."); var path = Path.GetFullPath(artifact.ModelPath); if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw new FileNotFoundException("Model artifact is unavailable."); var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))); if (!hash.Equals(artifact.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Model artifact hash validation failed."); var model = ml.Model.Load(path, out var schema); return (model, schema); }
    private string SafeDirectory(string version) { if (version.Any(c => !(char.IsLetterOrDigit(c) || c is '-' or '_'))) throw new InvalidOperationException("Unsafe model version."); var path = Path.GetFullPath(Path.Combine(_root, version)); if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Artifact path escapes the configured root."); return path; }
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}

public sealed class NextQuarterStateEntryTrainingService(FileSystemModelArtifactStore artifacts) : INextQuarterStateEntryTrainingService
{
    public async Task<TrainNextQuarterStateEntryResult> TrainAsync(TrainNextQuarterStateEntryRequest request, IReadOnlyCollection<NextQuarterTrainingRow> rows, CancellationToken cancellationToken = default)
    {
        if (request.Trainers.Count == 0 || request.Trainers.Distinct().Count() != request.Trainers.Count) throw new ArgumentException("At least one unique supported trainer is required.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken); timeout.CancelAfter(request.Timeout ?? TimeSpan.FromMinutes(10)); timeout.Token.ThrowIfCancellationRequested();
        var dataset = new TemporalTrainingDatasetBuilder().Build(rows, request.Split, request.MaximumRows); var prevalence = dataset.Training.Count(x => x.LabelNextQuarterEntry == true) / (double)dataset.Training.Count;
        var ml = new MLContext(seed: request.RandomSeed); var trainData = ml.Data.LoadFromEnumerable(dataset.Training.Select(ToInput)); var validationData = ml.Data.LoadFromEnumerable(dataset.Validation.Select(ToInput)); var testData = ml.Data.LoadFromEnumerable(dataset.Test.Select(ToInput));
        var fitted = new List<(TrainerKind Trainer, ITransformer Model, TrainerEvaluation Evaluation, DataViewSchema Schema)>();
        foreach (var trainer in request.Trainers)
        {
            timeout.Token.ThrowIfCancellationRequested(); var watch = Stopwatch.StartNew(); var pipeline = Pipeline(ml, trainer, request.RandomSeed); var model = pipeline.Fit(trainData); watch.Stop(); var scoreWatch = Stopwatch.StartNew(); var validationPredictions = Predict(ml, model, validationData, request.Threshold); scoreWatch.Stop(); var metrics = BinaryMetricCalculator.Calculate(validationPredictions, request.Threshold, prevalence);
            fitted.Add((trainer, model, new(trainer, metrics, null, watch.ElapsedMilliseconds, scoreWatch.ElapsedMilliseconds, JsonSerializer.Serialize(new { seed = request.RandomSeed, threshold = request.Threshold }), null, ModelApprovalStatus.Candidate, trainer == TrainerKind.LightGbm ? ["LightGBM uses a native runtime; reproducibility can vary slightly across platforms."] : []), trainData.Schema));
        }
        var selected = fitted.OrderByDescending(x => x.Evaluation.ValidationMetrics.PrAuc).ThenBy(x => x.Evaluation.ValidationMetrics.LogLoss).ThenBy(x => x.Evaluation.ValidationMetrics.BrierScore).ThenBy(x => Complexity(x.Trainer)).First();
        var selectedValidationPredictions = Predict(ml, selected.Model, validationData, request.Threshold);
        var threshold = Enumerable.Range(5, 91).Select(x => x / 100d)
            .Select(x => (Threshold: x, Metrics: BinaryMetricCalculator.Calculate(selectedValidationPredictions, x, prevalence)))
            .OrderByDescending(x => x.Metrics.F1).ThenBy(x => Math.Abs(x.Threshold - .5)).First().Threshold;
        var selectedValidationMetrics = BinaryMetricCalculator.Calculate(selectedValidationPredictions, threshold, prevalence);
        var testPredictions = Predict(ml, selected.Model, testData, threshold);
        var testMetrics = BinaryMetricCalculator.Calculate(testPredictions, threshold, prevalence);
        var baselines = new[]
        {
            new BaselineEvaluation("OverallPrevalence", BinaryMetricCalculator.Calculate(BinaryMetricCalculator.PrevalenceBaseline(dataset.Validation, prevalence, threshold), threshold, prevalence), BinaryMetricCalculator.Calculate(BinaryMetricCalculator.PrevalenceBaseline(dataset.Test, prevalence, threshold), threshold, prevalence)),
            new BaselineEvaluation("HistoricalStateEntryRate", BinaryMetricCalculator.Calculate(BinaryMetricCalculator.HistoricalStateRateBaseline(dataset.Validation, prevalence, threshold), threshold, prevalence), BinaryMetricCalculator.Calculate(BinaryMetricCalculator.HistoricalStateRateBaseline(dataset.Test, prevalence, threshold), threshold, prevalence))
        };
        StoredArtifact? artifact = null; string? card = null; string? reproducibility = null;
        if (!request.ValidateOnly)
        {
            artifact = artifacts.Save(ml, selected.Model, trainData.Schema, "NextQuarterStateEntry", request.ExperimentName, request.DatasetVersionId, request.FeatureSetVersionId, selected.Trainer.ToString(), dataset.FeatureSelection.SchemaHash);
            card = Path.Combine(Path.GetDirectoryName(artifact.ModelPath)!, "model-card.md"); File.WriteAllText(card, ModelCard(request, dataset, selected.Trainer, selectedValidationMetrics, testMetrics, artifact));
            reproducibility = Path.Combine(Path.GetDirectoryName(artifact.ModelPath)!, "reproducibility.json"); File.WriteAllText(reproducibility, JsonSerializer.Serialize(new { syntheticDevelopmentData = false, task = "NextQuarterStateEntry", request.DatasetVersionId, request.FeatureSetVersionId, request.FeatureSelectionVersion, dataset.DatasetHash, dataset.SplitManifestHash, featureSchemaHash = dataset.FeatureSelection.SchemaHash, request.RandomSeed, trainer = selected.Trainer.ToString(), selectedThreshold = threshold, packageVersion = "5.0.0", dotnetVersion = Environment.Version.ToString(), artifact.Sha256 }, new JsonSerializerOptions { WriteIndented = true }));
        }
        var candidates = fitted.Select(x => x.Trainer == selected.Trainer ? x.Evaluation with { ValidationMetrics = selectedValidationMetrics, TestMetrics = testMetrics, ArtifactVersion = artifact?.Version, ApprovalStatus = ModelApprovalStatus.ValidationSelected } : x.Evaluation).ToArray();
        await Task.CompletedTask; return new("NextQuarterStateEntry", Guid.NewGuid().ToString("N"), dataset, baselines, candidates, candidates.Single(x => x.Trainer == selected.Trainer), artifact?.ModelPath, card, reproducibility, ["Observational access data; predictions are not clinical advice.", "Model and threshold were selected without locked-test outcomes."], selectedValidationPredictions, testPredictions, threshold);
    }

    private static IEstimator<ITransformer> Pipeline(MLContext ml, TrainerKind trainer, int seed)
    {
        var replacements = NextQuarterFeatureSchema.OrderedFeatures.Select(x => new InputOutputColumnPair($"{x}Imputed", x)).ToArray(); var imputed = replacements.Select(x => x.OutputColumnName).ToArray();
        IEstimator<ITransformer> pipeline = ml.Transforms.ReplaceMissingValues(replacements).Append(ml.Transforms.Concatenate("Features", imputed));
        if (trainer == TrainerKind.LogisticRegression) pipeline = pipeline.Append(ml.Transforms.NormalizeMeanVariance("Features"));
        return trainer switch
        {
            TrainerKind.LogisticRegression => pipeline.Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features")),
            TrainerKind.FastTree => pipeline.Append(ml.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options { LabelColumnName = "Label", FeatureColumnName = "Features", NumberOfTrees = 40, NumberOfLeaves = 16, MinimumExampleCountPerLeaf = 2 })),
            TrainerKind.FastForest => pipeline.Append(ml.BinaryClassification.Trainers.FastForest(new FastForestBinaryTrainer.Options { LabelColumnName = "Label", FeatureColumnName = "Features", NumberOfTrees = 40, NumberOfLeaves = 16, MinimumExampleCountPerLeaf = 2, Seed = seed })),
            TrainerKind.LightGbm => pipeline.Append(ml.BinaryClassification.Trainers.LightGbm(new Microsoft.ML.Trainers.LightGbm.LightGbmBinaryTrainer.Options { LabelColumnName = "Label", FeatureColumnName = "Features", NumberOfIterations = 40, NumberOfLeaves = 16, MinimumExampleCountPerLeaf = 2, Seed = seed })),
            _ => throw new NotSupportedException($"Trainer {trainer} is unsupported.")
        };
    }
    private static IReadOnlyList<BinaryPrediction> Predict(MLContext ml, ITransformer model, IDataView data, double threshold)
    {
        var scored = model.Transform(data);
        if (scored.Schema.Any(column => column.Name == "Probability")) return ml.Data.CreateEnumerable<ModelOutput>(scored, false).Select(x => new BinaryPrediction(x.Label, x.Score, x.Probability, x.Probability >= threshold)).ToArray();
        return ml.Data.CreateEnumerable<ModelOutputWithoutProbability>(scored, false).Select(x => { var probability = (float)(1d / (1d + Math.Exp(-x.Score))); return new BinaryPrediction(x.Label, x.Score, probability, probability >= threshold); }).ToArray();
    }
    private static int Complexity(TrainerKind x) => x switch { TrainerKind.LogisticRegression => 0, TrainerKind.FastTree => 1, TrainerKind.FastForest => 2, _ => 3 };
    private static ModelInput ToInput(NextQuarterTrainingRow x) => new() { Label = x.LabelNextQuarterEntry!.Value, QuarterSinceApproval = x.QuarterSinceApproval, NumberOfObservedQuarters = x.NumberOfObservedQuarters, ObservedPrescriptionCount = x.ObservedPrescriptionCount, ObservedReimbursementAmount = x.ObservedReimbursementAmount, Lag1PrescriptionCount = x.Lag1PrescriptionCount, Lag2PrescriptionCount = x.Lag2PrescriptionCount, Lag1ReimbursementAmount = x.Lag1ReimbursementAmount, Lag2ReimbursementAmount = x.Lag2ReimbursementAmount, PrescriptionGrowthRate = x.PrescriptionGrowthRate, ReimbursementGrowthRate = x.ReimbursementGrowthRate, InitialActiveStateCount = x.InitialActiveStateCount, InitialPrescriptionVolume = x.InitialPrescriptionVolume, PreviousQuarterNumericDistribution = x.PreviousQuarterNumericDistribution, PreviousQuarterWeightedDistribution = x.PreviousQuarterWeightedDistribution, PreviousQuarterAccessGap = x.PreviousQuarterAccessGap, StateHistoricalGenericVolume = x.StateHistoricalGenericVolume, StateHistoricalLaunchCount = x.StateHistoricalLaunchCount, StateHistoricalEntryRate = x.StateHistoricalEntryRate, StateHistoricalMedianEntryDelay = x.StateHistoricalMedianEntryDelay, StateHistoricalMarketWeight = x.StateHistoricalMarketWeight, StateVolumePercentile = x.StateVolumePercentile, StateDataCompleteness = x.StateDataCompleteness, RegionActiveStateShare = x.RegionActiveStateShare, RegionHistoricalEntryRate = x.RegionHistoricalEntryRate, NeighborStateAdoptionShare = x.NeighborStateAdoptionShare, SimilarStateAdoptionShare = x.SimilarStateAdoptionShare, RegionPrescriptionGrowth = x.RegionPrescriptionGrowth, NationalPrescriptionGrowth = x.NationalPrescriptionGrowth, MissingFeatureCount = x.MissingFeatureCount, IsObservedZero = x.IsObservedZero ? 1 : 0, IsMissing = x.IsMissing ? 1 : 0, IsSuppressed = x.IsSuppressed ? 1 : 0 };
    private static string ModelCard(TrainNextQuarterStateEntryRequest r, TrainingDataset d, TrainerKind trainer, ClassificationMetrics validation, ClassificationMetrics test, StoredArtifact artifact) => $"""
# NextQuarterStateEntry model card

**Synthetic development data — not research results**

- Intended use: development prediction of next-quarter first state entry.
- Prohibited use: clinical decisions, causal claims, or claims of complete national access.
- Algorithm: {trainer}; dataset version: {r.DatasetVersionId}; feature set: {r.FeatureSetVersionId}; selection: {r.FeatureSelectionVersion}.
- Training/validation/test groups: {d.Training.Select(x => x.GenericLaunchId).Distinct().Count()}/{d.Validation.Select(x => x.GenericLaunchId).Distinct().Count()}/{d.Test.Select(x => x.GenericLaunchId).Distinct().Count()}.
- Validation PR AUC: {validation.PrAuc.ToString("G6", CultureInfo.InvariantCulture)}; test PR AUC: {test.PrAuc.ToString("G6", CultureInfo.InvariantCulture)}; threshold: {r.Threshold}.
- Artifact SHA-256: {artifact.Sha256}; approval: ValidationSelected (not Approved).
- FDA approval may not equal commercial launch. Medicaid utilization is not complete national access.
- Probabilities are not described as calibrated. Feature importance is not causal evidence. Subgroup evaluation is not yet complete.
""";
}

public sealed class NextStateEntryPredictionService(FileSystemModelArtifactStore store, IModelArtifactRegistry registry, ITrainingFeatureRowResolver rows, int maximumBatchSize = 1000) : INextStateEntryPredictionService
{
    public async Task<NextStateEntryPredictionResponse?> PredictAsync(NextStateEntryPredictionRequest request, CancellationToken cancellationToken = default)
    {
        var artifact = await registry.ResolveAsync(request.ModelVersion, cancellationToken);
        if (artifact is null) throw new FileNotFoundException("Selected model artifact is not registered.");
        if (artifact.Status is not (ModelApprovalStatus.Candidate or ModelApprovalStatus.ValidationSelected or ModelApprovalStatus.Approved)) throw new InvalidOperationException("Model status is not scoreable.");
        if (artifact.FeatureSetVersionId != request.FeatureSetVersionId) throw new InvalidOperationException("Feature-set version is incompatible with the model.");
        var row = await rows.ResolveAsync(request.FeatureRowId, request.FeatureSetVersionId, cancellationToken); if (row is null) return null;
        if (maximumBatchSize < 1) throw new InvalidOperationException("Prediction batch limit is invalid.");
        var ml = new MLContext(); var stored = new StoredArtifact(artifact.Version, artifact.ModelPath, artifact.ManifestPath, artifact.Sha256, artifact.FileSize, artifact.SchemaHash); var loaded = store.Load(ml, stored, NextQuarterFeatureSchema.Create().SchemaHash);
        var input = ml.Data.LoadFromEnumerable(new[] { ToScoringInput(row) }); var scored = loaded.Model.Transform(input); float probability; float score;
        if (scored.Schema.Any(column => column.Name == "Probability")) { var prediction = ml.Data.CreateEnumerable<ModelOutput>(scored, false).Single(); probability = prediction.Probability; score = prediction.Score; }
        else { var prediction = ml.Data.CreateEnumerable<ModelOutputWithoutProbability>(scored, false).Single(); score = prediction.Score; probability = (float)(1d / (1d + Math.Exp(-score))); }
        var warnings = new List<string>(); if (row.IsMissing || row.IsSuppressed || row.MissingFeatureCount > 0) warnings.Add("One or more inputs are missing or suppressed; training-fitted imputation is applied.");
        var uncertainty = new UncertaintyIndicatorService().Assess(probability, artifact.Threshold, 0, (int)row.MissingFeatureCount, 0, 0, 0, null, true);
        return new("NextQuarterStateEntry", probability, null, CalibrationStatus.NotAttempted, score, probability >= artifact.Threshold, artifact.Threshold, uncertainty.Status, uncertainty.Reasons, [], artifact.Version, artifact.Status, artifact.DatasetVersionId, artifact.FeatureSetVersionId, row.ObservationQuarterId, warnings, DateTime.UtcNow);
    }

    private static ModelInput ToScoringInput(NextQuarterTrainingRow x) => new() { Label = x.LabelNextQuarterEntry ?? false, QuarterSinceApproval = x.QuarterSinceApproval, NumberOfObservedQuarters = x.NumberOfObservedQuarters, ObservedPrescriptionCount = x.ObservedPrescriptionCount, ObservedReimbursementAmount = x.ObservedReimbursementAmount, Lag1PrescriptionCount = x.Lag1PrescriptionCount, Lag2PrescriptionCount = x.Lag2PrescriptionCount, Lag1ReimbursementAmount = x.Lag1ReimbursementAmount, Lag2ReimbursementAmount = x.Lag2ReimbursementAmount, PrescriptionGrowthRate = x.PrescriptionGrowthRate, ReimbursementGrowthRate = x.ReimbursementGrowthRate, InitialActiveStateCount = x.InitialActiveStateCount, InitialPrescriptionVolume = x.InitialPrescriptionVolume, PreviousQuarterNumericDistribution = x.PreviousQuarterNumericDistribution, PreviousQuarterWeightedDistribution = x.PreviousQuarterWeightedDistribution, PreviousQuarterAccessGap = x.PreviousQuarterAccessGap, StateHistoricalGenericVolume = x.StateHistoricalGenericVolume, StateHistoricalLaunchCount = x.StateHistoricalLaunchCount, StateHistoricalEntryRate = x.StateHistoricalEntryRate, StateHistoricalMedianEntryDelay = x.StateHistoricalMedianEntryDelay, StateHistoricalMarketWeight = x.StateHistoricalMarketWeight, StateVolumePercentile = x.StateVolumePercentile, StateDataCompleteness = x.StateDataCompleteness, RegionActiveStateShare = x.RegionActiveStateShare, RegionHistoricalEntryRate = x.RegionHistoricalEntryRate, NeighborStateAdoptionShare = x.NeighborStateAdoptionShare, SimilarStateAdoptionShare = x.SimilarStateAdoptionShare, RegionPrescriptionGrowth = x.RegionPrescriptionGrowth, NationalPrescriptionGrowth = x.NationalPrescriptionGrowth, MissingFeatureCount = x.MissingFeatureCount, IsObservedZero = x.IsObservedZero ? 1 : 0, IsMissing = x.IsMissing ? 1 : 0, IsSuppressed = x.IsSuppressed ? 1 : 0 };
}
