using System.Security.Cryptography;
using System.Text.Json;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.ML;
using Xunit;

namespace PharmaAccess.ML.Tests;

public sealed class ServingModelBundleTests : IDisposable
{
    private const string Version = "NextQuarterStateEntry-real-next-quarter-entry-serving-v1";
    private const string ArtifactHash = "62C607C6462D603D04CDF43662B16EAD773138C916912080B15A7F65C85F9022";
    private const string DatasetHash = "84B973AFF0545C987B52C525F0CBAB607C293BEFC1D5EBCC87ED6769DECB0C36";
    private const string FeatureHash = "8E849D11A4D25976F808E49436A6576E74311C4CC97016E7C7126C43210AC750";
    private const string SchemaHash = "BE2EBF510D5E610DD4F54D4E62BB6F9BDB47EACFB496B6739659C7E6AA6C595E";
    private readonly string _temporaryRoot = Path.Combine(Path.GetTempPath(), "serving-bundle-tests-" + Guid.NewGuid().ToString("N"));
    private static string RepositoryRoot => FindRepositoryRoot(AppContext.BaseDirectory);
    private static string BundleRoot => Path.Combine(RepositoryRoot, "artifacts", "models");
    private static string BundlePath => Path.Combine(BundleRoot, Version);

    [Fact]
    [Trait("Category","LocalResearchIntegration")]
    public async Task Corrected_real_bundle_has_verified_identity_lineage_and_is_not_active()
    {
        var modelPath = Path.Combine(BundlePath, "model.zip");
        Assert.Equal(ArtifactHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(modelPath))));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(BundlePath, "manifest.json")));
        var root = manifest.RootElement;
        Assert.False(root.GetProperty("syntheticDevelopmentData").GetBoolean());
        Assert.Equal("real-2021-2025-v1", root.GetProperty("dataset").GetString());
        Assert.Equal(DatasetHash, root.GetProperty("datasetHash").GetString());
        Assert.Equal(1, root.GetProperty("featureSetVersionId").GetInt32()); Assert.Equal(FeatureHash, root.GetProperty("featureSetDefinitionHash").GetString());
        Assert.Equal(SchemaHash, root.GetProperty("modelInputSchemaHash").GetString()); Assert.Equal(.08, root.GetProperty("selectedThreshold").GetDouble());
        Assert.Equal("ValidationSelected", root.GetProperty("governanceStatus").GetString()); Assert.False(root.GetProperty("approved").GetBoolean()); Assert.False(root.GetProperty("champion").GetBoolean());
        var registry = Registry(BundleRoot);
        var explicitCandidate = await registry.ResolveAsync(Version, CancellationToken.None); Assert.NotNull(explicitCandidate); Assert.Equal(ModelApprovalStatus.ValidationSelected, explicitCandidate.Status); Assert.Equal(.08, explicitCandidate.Threshold); Assert.Equal(-.4, explicitCandidate.PlattA); Assert.Equal(0, explicitCandidate.PlattB);
        Assert.Null(await registry.ResolveAsync(null, CancellationToken.None));
        Assert.True(File.Exists(Path.Combine(BundlePath, "verification.json"))); Assert.DoesNotContain(Directory.GetFiles(BundlePath, "*", SearchOption.AllDirectories), x => x.Contains("final-test-features", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Serving_decision_uses_selected_threshold_not_serialized_predicted_label()
    {
        Assert.True(ServingDecisionPolicy.IsPredicted(.10f, .08));
        Assert.False(ServingDecisionPolicy.IsPredicted(.05f, .08));
        Assert.True(.10f < .5f);
    }

    [Theory]
    [Trait("Category","LocalResearchIntegration")]
    [InlineData("artifactHash", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Model artifact hash mismatch")]
    [InlineData("modelInputSchemaHash", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "schema compatibility")]
    [InlineData("datasetHash", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Dataset compatibility")]
    [InlineData("featureSetDefinitionHash", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Feature-set compatibility")]
    public void Integrity_and_compatibility_mismatches_fail_closed(string property, string replacement, string expectedMessage)
    {
        var copy = CopyBundle(); var manifestPath = Path.Combine(copy, "manifest.json");
        var text = File.ReadAllText(manifestPath); using var document = JsonDocument.Parse(text); var original = document.RootElement.GetProperty(property).GetString()!;
        File.WriteAllText(manifestPath, text.Replace(original, replacement, StringComparison.Ordinal));
        var error = Assert.Throws<InvalidDataException>(() => Registry(_temporaryRoot).LoadAndVerify(manifestPath)); Assert.Contains(expectedMessage, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category","LocalResearchIntegration")]
    public void Existing_synthetic_bundle_is_rejected_by_real_registry()
    {
        var copy = CopyBundle(); var manifestPath = Path.Combine(copy, "manifest.json"); var text = File.ReadAllText(manifestPath).Replace("\"syntheticDevelopmentData\": false", "\"syntheticDevelopmentData\": true", StringComparison.Ordinal); File.WriteAllText(manifestPath, text);
        Assert.Contains("Synthetic development", Assert.Throws<InvalidDataException>(() => Registry(_temporaryRoot).LoadAndVerify(manifestPath)).Message);
    }

    private string CopyBundle() { Directory.CreateDirectory(_temporaryRoot); var target = Path.Combine(_temporaryRoot, Version); Directory.CreateDirectory(target); foreach (var file in Directory.GetFiles(BundlePath)) File.Copy(file, Path.Combine(target, Path.GetFileName(file))); return target; }
    private static LocalServingModelBundleRegistry Registry(string root) => new(root, new("real-2021-2025-v1", 2, DatasetHash, "real-next-entry-features-v1", 1, FeatureHash, SchemaHash));
    private static string FindRepositoryRoot(string start) { var current = new DirectoryInfo(start); while (current is not null && !File.Exists(Path.Combine(current.FullName, "PharmaAccess.sln"))) current = current.Parent; return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found."); }
    public void Dispose() { if (Directory.Exists(_temporaryRoot)) Directory.Delete(_temporaryRoot, true); }
}
