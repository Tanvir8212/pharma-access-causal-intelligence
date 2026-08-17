using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed class StateReadinessContextService(bool development, bool enabled) : IStateReadinessContextService
{
    private const string Database = "PharmaAccessCausalIntelligence_ResearchDev";
    private readonly bool _allowed = development && enabled;
    private readonly ConcurrentDictionary<int, Lazy<Task<IReadOnlyList<StateReadinessHistoricalLaunch>>>> _historyCache = new();
    private readonly ConcurrentDictionary<(int LaunchId, int Quarter), Lazy<Task<IReadOnlyList<StateReadinessCandidate>>>> _candidateCache = new();

    public async Task<StateReadinessContextResponse> GetAsync(StateReadinessContextRequest request, CancellationToken cancellationToken = default)
    {
        Guard(request);
        var history = await _historyCache.GetOrAdd(request.AsOfQuarter,
            quarter => new(() => ExtractHistoryAsync(quarter), LazyThreadSafetyMode.ExecutionAndPublication)).Value.WaitAsync(cancellationToken);
        var candidates = await _candidateCache.GetOrAdd((request.GenericLaunchId, request.AsOfQuarter),
            key => new(() => ExtractCandidatesAsync(key.LaunchId, key.Quarter), LazyThreadSafetyMode.ExecutionAndPublication)).Value.WaitAsync(cancellationToken);
        if (candidates.Count == 0) throw new InvalidOperationException("InsufficientHistoricalContext");
        var states = StateReadinessContextCalculator.Calculate(request.AsOfQuarter, history, candidates);
        return new(request.GenericLaunchId, request.AsOfQuarter, states,
        [
            "State readiness context is descriptive historical context, not a probability, causal estimate, recommendation, or combined score.",
            "Historical participation and entry delay use only observable panel history strictly before the selected quarter.",
            "Nearby adoption is selected-launch-specific and uses the governed lagged neighbor exposure at the selected quarter."
        ]);
    }

    internal static async Task<IReadOnlyList<StateReadinessHistoricalLaunch>> ExtractHistoryAsync(int asOfQuarter)
    {
        return (await QueryAsync(BuildHistorySql(asOfQuarter))).Select(ParseHistory).ToArray();
    }

    internal static string BuildHistorySql(int asOfQuarter) => $$"""
            SELECT RTRIM(p.StateCode),p.AndaLaunchId,
              MAX(CASE WHEN p.IsSuppressed=0 AND (p.IsObservedZero=1 OR p.IsPresent=1) THEN 1 ELSE 0 END) HasObservableHistory,
              MAX(CASE WHEN p.IsSuppressed=0 AND p.IsPresent=1 THEN 1 ELSE 0 END) HasParticipated,
              MIN(CASE WHEN p.IsFirstEntryQuarter=1 AND p.IsSuppressed=0 THEN p.QuarterSinceApproval END) FirstEntryDelay
            FROM research.AndaStateQuarterPanel p
            WHERE p.ObservationQuarter<{{asOfQuarter}}
            GROUP BY p.StateCode,p.AndaLaunchId
            ORDER BY p.StateCode,p.AndaLaunchId OPTION (MAXDOP 1);
            """;

    internal static async Task<IReadOnlyList<StateReadinessCandidate>> ExtractCandidatesAsync(int launchId, int asOfQuarter)
    {
        return (await QueryAsync(BuildCandidateSql(launchId, asOfQuarter))).Select(ParseCandidate).ToArray();
    }

    internal static string BuildCandidateSql(int launchId, int asOfQuarter) => $$"""
            SELECT RTRIM(p.StateCode),p.LaggedNeighborExposure,p.EligiblePeerCount
            FROM research.AndaStateQuarterPanel p
            WHERE p.AndaLaunchId={{launchId}} AND p.ObservationQuarter={{asOfQuarter}}
              AND p.HasEntered=0 AND p.IsCensored=0
            ORDER BY p.StateCode;
            """;

    private static StateReadinessHistoricalLaunch ParseHistory(string line)
    {
        var x = line.Split('|');
        if (x.Length != 5) throw new InvalidDataException("State readiness history width mismatch.");
        var state = ResolveState(x[0]);
        return new(state.StateId, state.StateCode, int.Parse(x[1], CultureInfo.InvariantCulture),
            x[2] == "1", x[3] == "1", x[4] == "NULL" ? null : int.Parse(x[4], CultureInfo.InvariantCulture));
    }

    private static StateReadinessCandidate ParseCandidate(string line)
    {
        var x = line.Split('|');
        if (x.Length != 3) throw new InvalidDataException("State readiness candidate width mismatch.");
        var state = ResolveState(x[0]);
        return new(state.StateId, state.StateCode,
            x[1] == "NULL" ? null : decimal.Parse(x[1], CultureInfo.InvariantCulture),
            int.Parse(x[2], CultureInfo.InvariantCulture));
    }

    private static VerifiedStateMetadata ResolveState(string code) => VerifiedStateReference.All.SingleOrDefault(
        x => x.StateCode.Equals(code.Trim(), StringComparison.Ordinal))
        ?? throw new InvalidDataException("Panel state is absent from the verified jurisdiction reference.");

    private static async Task<string[]> QueryAsync(string sql)
    {
        var start = new ProcessStartInfo("sqlcmd") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-S", "lpc:.", "-E", "-d", Database, "-X", "-b", "-W", "-h", "-1", "-s", "|", "-w", "65535", "-Q", "SET NOCOUNT ON; " + sql }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("sqlcmd is unavailable.");
        var rows = new List<string>();
        while (await process.StandardOutput.ReadLineAsync() is { } line) if (!string.IsNullOrWhiteSpace(line)) rows.Add(line);
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException($"Read-only state readiness extraction failed: {error.Trim()}");
        return rows.ToArray();
    }

    private void Guard(StateReadinessContextRequest request)
    {
        if (!_allowed) throw new InvalidOperationException("Research development state readiness context is disabled.");
        if (request.GenericLaunchId <= 0 || request.AsOfQuarter < 20211 || request.AsOfQuarter > 20253 || request.AsOfQuarter % 10 is < 1 or > 4)
            throw new ArgumentException("State readiness launch and as-of quarter are invalid or beyond the research horizon.");
    }
}
