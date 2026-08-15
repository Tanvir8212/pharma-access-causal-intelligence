using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Llm;
using PharmaAccess.ML;

namespace PharmaAccess.Api;

public enum ExplorerComponentStatus { Ready, Degraded, Unavailable, Disabled }

public sealed record ExplorerPreflightResult(
    ExplorerComponentStatus Status,
    IReadOnlyDictionary<string, ExplorerComponentStatus> Components);

public interface IExplorerPreflight
{
    Task<ExplorerPreflightResult> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed class ExplorerPreflight : IExplorerPreflight
{
    private const string SnapshotHash = "8B049DC35661502EA0E1236B8275D81EA21F540D7D6F729F4FAC2C6A3A4F336D";
    private const string ComparisonHash = "839015EE3646B692B2DA706AC0C8BB9177AFFE575E905192F98A9D421C9A6FAB";
    private const string ModelHash = "62C607C6462D603D04CDF43662B16EAD773138C916912080B15A7F65C85F9022";
    private readonly bool _allowed;
    private readonly bool _enabled;
    private readonly string _snapshot;
    private readonly string _comparison;
    private readonly string _bundle;
    private readonly ILanguageModelClient _gemini;
    private readonly Lazy<Task<ExplorerPreflightResult>> _check;

    public ExplorerPreflight(bool allowed, bool enabled, string snapshot, string comparison, string bundle, ILanguageModelClient gemini)
    {
        _allowed = allowed;
        _enabled = enabled;
        _snapshot = snapshot;
        _comparison = comparison;
        _bundle = bundle;
        _gemini = gemini;
        _check = new(RunAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<ExplorerPreflightResult> CheckAsync(CancellationToken cancellationToken = default) =>
        _check.Value.WaitAsync(cancellationToken);

    private async Task<ExplorerPreflightResult> RunAsync()
    {
        var components = new Dictionary<string, ExplorerComponentStatus>(StringComparer.Ordinal)
        {
            ["VerifiedModel"] = ExplorerComponentStatus.Disabled,
            ["HistoricalReplay"] = ExplorerComponentStatus.Disabled,
            ["DynamicResearchSource"] = ExplorerComponentStatus.Disabled,
            ["GeminiExplanation"] = _gemini.IsAvailable ? ExplorerComponentStatus.Ready : ExplorerComponentStatus.Degraded
        };
        if (!_enabled) return new(ExplorerComponentStatus.Ready, components);
        if (!_allowed)
        {
            components["VerifiedModel"] = ExplorerComponentStatus.Unavailable;
            components["HistoricalReplay"] = ExplorerComponentStatus.Unavailable;
            components["DynamicResearchSource"] = ExplorerComponentStatus.Unavailable;
            return new(ExplorerComponentStatus.Unavailable, components);
        }

        components["HistoricalReplay"] = VerifyFile(_snapshot, SnapshotHash);
        var comparison = VerifyFile(_comparison, ComparisonHash);
        components["VerifiedModel"] = VerifyBundle();
        components["DynamicResearchSource"] = comparison == ExplorerComponentStatus.Ready
            ? await ProbeResearchDevAsync()
            : ExplorerComponentStatus.Unavailable;
        var status = components.Values.Any(x => x == ExplorerComponentStatus.Unavailable)
            ? ExplorerComponentStatus.Unavailable
            : components.Values.Any(x => x == ExplorerComponentStatus.Degraded)
                ? ExplorerComponentStatus.Degraded
                : ExplorerComponentStatus.Ready;
        return new(status, components);
    }

    private ExplorerComponentStatus VerifyBundle()
    {
        try
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(_bundle));
            if (parent is null) return ExplorerComponentStatus.Unavailable;
            var registry = new LocalServingModelBundleRegistry(parent, new(
                "real-2021-2025-v1", 2, "84B973AFF0545C987B52C525F0CBAB607C293BEFC1D5EBCC87ED6769DECB0C36",
                "real-next-entry-features-v1", 1, "8E849D11A4D25976F808E49436A6576E74311C4CC97016E7C7126C43210AC750",
                "BE2EBF510D5E610DD4F54D4E62BB6F9BDB47EACFB496B6739659C7E6AA6C595E"));
            var artifact = registry.LoadAndVerify(Path.Combine(_bundle, "manifest.json"));
            return artifact.Version == "NextQuarterStateEntry-real-next-quarter-entry-serving-v1" &&
                   artifact.Status == ModelApprovalStatus.ValidationSelected &&
                   !artifact.IsProductionApproved() && artifact.Threshold == .08 &&
                   artifact.Sha256.Equals(ModelHash, StringComparison.OrdinalIgnoreCase)
                ? ExplorerComponentStatus.Ready
                : ExplorerComponentStatus.Unavailable;
        }
        catch { return ExplorerComponentStatus.Unavailable; }
    }

    private static ExplorerComponentStatus VerifyFile(string path, string expectedHash)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
            return Convert.ToHexString(SHA256.HashData(stream)).Equals(expectedHash, StringComparison.OrdinalIgnoreCase)
                ? ExplorerComponentStatus.Ready
                : ExplorerComponentStatus.Unavailable;
        }
        catch { return ExplorerComponentStatus.Unavailable; }
    }

    private static async Task<ExplorerComponentStatus> ProbeResearchDevAsync()
    {
        try
        {
            var start = new ProcessStartInfo("sqlcmd") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in new[] { "-S", "lpc:.", "-E", "-d", "PharmaAccessCausalIntelligence_ResearchDev", "-X", "-b", "-W", "-h", "-1", "-Q", "SET NOCOUNT ON; SELECT COUNT_BIG(*) FROM research.AndaStateQuarterPanel;" }) start.ArgumentList.Add(argument);
            using var process = Process.Start(start);
            if (process is null) return ExplorerComponentStatus.Unavailable;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            return process.ExitCode == 0 && output.Trim() == "174471" ? ExplorerComponentStatus.Ready : ExplorerComponentStatus.Unavailable;
        }
        catch { return ExplorerComponentStatus.Unavailable; }
    }
}

internal sealed class ExplorerPreflightHostedService(IExplorerPreflight preflight, ILogger<ExplorerPreflightHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await preflight.CheckAsync(cancellationToken);
        logger.LogInformation("Explorer preflight completed Status={Status} Components={Components}", result.Status,
            string.Join(',', result.Components.Select(x => $"{x.Key}:{x.Value}")));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal static class ArtifactStatusExtensions
{
    public static bool IsProductionApproved(this SelectedModelArtifact artifact) =>
        artifact.Status is ModelApprovalStatus.Approved or ModelApprovalStatus.Champion;
}
