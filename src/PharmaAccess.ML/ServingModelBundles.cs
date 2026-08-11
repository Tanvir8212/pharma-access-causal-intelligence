using System.Security.Cryptography;
using System.Text.Json;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed record ServingModelCompatibility(string Dataset, int DatasetVersionId, string DatasetHash,
    string FeatureSet, int FeatureSetVersionId, string FeatureSetDefinitionHash, string ModelInputSchemaHash);

public sealed class LocalServingModelBundleRegistry(string root, ServingModelCompatibility compatibility) : IModelArtifactRegistry
{
    private readonly string _root = Path.GetFullPath(root);

    public Task<SelectedModelArtifact?> ResolveAsync(string? modelVersion, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(_root)) return Task.FromResult<SelectedModelArtifact?>(null);
        var bundles = Directory.GetDirectories(_root).Select(x => Path.Combine(x, "manifest.json"))
            .Where(File.Exists).Select(LoadAndVerify).ToArray();
        var selected = string.IsNullOrWhiteSpace(modelVersion)
            ? bundles.SingleOrDefault(x => x.Status == ModelApprovalStatus.Champion)
            : bundles.SingleOrDefault(x => x.Version.Equals(modelVersion, StringComparison.Ordinal));
        return Task.FromResult(selected);
    }

    public SelectedModelArtifact LoadAndVerify(string manifestPath)
    {
        var fullManifest = Path.GetFullPath(manifestPath);
        if (!fullManifest.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Serving manifest is outside the configured model root.");
        var manifest = JsonSerializer.Deserialize<ServingManifest>(File.ReadAllText(fullManifest), JsonOptions) ?? throw new InvalidDataException("Serving manifest is invalid.");
        Require(!manifest.SyntheticDevelopmentData, "Synthetic development bundles are not compatible with this real serving registry.");
        Require(manifest.Task == "NextQuarterStateEntry" && manifest.Trainer == "FastTree", "Task or trainer compatibility failed.");
        Require(manifest.Dataset == compatibility.Dataset && manifest.DatasetVersionId == compatibility.DatasetVersionId && EqualHash(manifest.DatasetHash, compatibility.DatasetHash), "Dataset compatibility failed.");
        Require(manifest.FeatureSet == compatibility.FeatureSet && manifest.FeatureSetVersionId == compatibility.FeatureSetVersionId && EqualHash(manifest.FeatureSetDefinitionHash, compatibility.FeatureSetDefinitionHash), "Feature-set compatibility failed.");
        Require(EqualHash(manifest.ModelInputSchemaHash, compatibility.ModelInputSchemaHash), "Model-input schema compatibility failed.");
        Require(manifest.SelectedThreshold is > 0 and < 1, "Selected threshold is invalid.");
        Require(manifest.ProbabilityOutput == "serialized ML.NET calibrated Probability" && manifest.ServingDecisionRule == "Probability >= 0.08", "Probability or serving-decision contract failed.");
        Require(manifest.SerializedPredictedLabelThreshold == .5, "Serialized PredictedLabel metadata is invalid.");
        Require(manifest.GovernanceStatus is nameof(ModelApprovalStatus.ValidationSelected) or nameof(ModelApprovalStatus.Approved) or nameof(ModelApprovalStatus.Champion), "Governance status is not serving-compatible.");
        var directory = Path.GetDirectoryName(fullManifest)!;
        var verificationPath = Path.Combine(directory, "verification.json"); var calibrationPath = Path.Combine(directory, "threshold-calibration.json");
        Require(File.Exists(verificationPath) && File.Exists(calibrationPath) && File.Exists(Path.Combine(directory, "model-card.md")), "Verification, calibration, or model-card evidence is missing.");
        var verification = JsonSerializer.Deserialize<VerificationMetadata>(File.ReadAllText(verificationPath), JsonOptions) ?? throw new InvalidDataException("Verification metadata is invalid.");
        Require(verification.VerificationStatus == "VerifiedRealModelBinary" && verification.RealTestRowsScored == 12477 && verification.StableKeyMatches == 12477 && verification.ExactSingleProbabilityRoundTripMatches == 12477 && verification.ExternalThresholdDecisionMatches == 12477, "R2B verification evidence failed.");
        Require(EqualHash(verification.R2aSnapshotHash, manifest.R2aSnapshotHash) && EqualHash(verification.R2bVerificationHash, manifest.R2bVerificationHash), "R2A/R2B provenance failed.");
        var calibration = JsonSerializer.Deserialize<CalibrationMetadata>(File.ReadAllText(calibrationPath), JsonOptions) ?? throw new InvalidDataException("Calibration metadata is invalid.");
        Require(calibration.ProbabilityOutput == manifest.ProbabilityOutput && calibration.SerializedCalibration.Method == "Platt" && calibration.SerializedCalibration.A == -.4 && calibration.SerializedCalibration.B == 0 && !calibration.AdditionalCalibrationLayer && calibration.SelectedExternalThreshold == manifest.SelectedThreshold && !calibration.SerializedPredictedLabelMayBeUsedForServingDecision, "Calibration contract failed.");
        var modelPath = Path.Combine(directory, "model.zip");
        Require(File.Exists(modelPath), "Model artifact is missing.");
        using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
        var hash = Convert.ToHexString(SHA256.HashData(stream));
        Require(EqualHash(hash, manifest.ArtifactHash), "Model artifact hash mismatch.");
        Require(stream.Length == manifest.ArtifactSizeBytes, "Model artifact size mismatch.");
        var status = Enum.Parse<ModelApprovalStatus>(manifest.GovernanceStatus, ignoreCase: false);
        return new(manifest.BundleVersion, modelPath, fullManifest, manifest.ArtifactHash, stream.Length,
            manifest.ModelInputSchemaHash, status, manifest.DatasetVersionId, manifest.FeatureSetVersionId,
            manifest.SelectedThreshold, manifest.ProbabilityOutput, calibration.SerializedCalibration.A, calibration.SerializedCalibration.B);
    }

    private static bool EqualHash(string left, string right) => left.Equals(right, StringComparison.OrdinalIgnoreCase);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidDataException(message); }
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed record ServingManifest(bool SyntheticDevelopmentData, string BundleVersion, string Task, string Trainer, string Dataset,
        int DatasetVersionId, string DatasetHash, string FeatureSet, int FeatureSetVersionId,
        string FeatureSetDefinitionHash, string ModelInputSchemaHash, string ArtifactHash, long ArtifactSizeBytes, double SelectedThreshold,
        string ProbabilityOutput, double SerializedPredictedLabelThreshold, string ServingDecisionRule,
        string GovernanceStatus, string R2aSnapshotHash, string R2bVerificationHash);
    private sealed record VerificationMetadata(string VerificationStatus, int RealTestRowsScored, int StableKeyMatches,
        int ExactSingleProbabilityRoundTripMatches, int ExternalThresholdDecisionMatches, string R2aSnapshotHash,
        string R2bVerificationHash);
    private sealed record CalibrationMetadata(string ProbabilityOutput, SerializedCalibration SerializedCalibration,
        bool AdditionalCalibrationLayer, double SelectedExternalThreshold,
        bool SerializedPredictedLabelMayBeUsedForServingDecision);
    private sealed record SerializedCalibration(string Method, double A, double B);
}
