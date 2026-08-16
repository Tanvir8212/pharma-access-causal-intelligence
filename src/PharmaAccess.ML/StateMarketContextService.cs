using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed class StateMarketContextService(bool development, bool enabled) : IStateMarketContextService
{
    private const string Database = "PharmaAccessCausalIntelligence_ResearchDev";
    private readonly bool _allowed = development && enabled;
    private readonly Lazy<Task<IReadOnlyList<StateMarketContextSource>>> _sourceCache =
        new(ExtractMarketAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    private readonly ConcurrentDictionary<int, Lazy<Task<IReadOnlyList<StateMarketContext>>>> _marketCache = new();

    public async Task<StateMarketContextResponse> GetAsync(StateMarketContextRequest request, CancellationToken cancellationToken = default)
    {
        Guard(request);
        var fullPopulation = await _marketCache.GetOrAdd(request.AsOfQuarter,
            quarter => new(async () => StateMarketContextCalculator.Calculate(quarter, await _sourceCache.Value),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value.WaitAsync(cancellationToken);
        var candidateIds = await ExtractCandidateIdsAsync(request.GenericLaunchId, request.AsOfQuarter, cancellationToken);
        if (candidateIds.Count == 0) throw new InvalidOperationException("InsufficientHistoricalContext");
        var states = fullPopulation.Where(x => candidateIds.Contains(x.StateId)).ToArray();
        if (states.Length != candidateIds.Count) throw new InvalidDataException("Market context state identity mismatch.");
        return new(request.GenericLaunchId, request.AsOfQuarter, fullPopulation.Count, states,
        [
            "Market size is the percentile rank of the latest four-quarter Medicaid prescription volume across the full valid jurisdiction population at the selected as-of quarter.",
            "Growth compares that latest four-quarter window with the preceding four and is ranked across the same full jurisdiction population.",
            "The governed 2021 Q1 historical volume and weight are retained separately as baseline context.",
            "Market context reflects Medicaid utilization, not product-specific demand or total commercial market opportunity."
        ]);
    }

    internal static async Task<IReadOnlyList<StateMarketContextSource>> ExtractMarketAsync()
    {
        var governedSources = await ExtractGovernedSourcesAsync();
        if (governedSources.Length == 0) throw new InvalidDataException("No governed real Medicaid source files are available.");
        var sourceIds = string.Join(',', governedSources.Select(x => x.SourceFileId));
        var minimumRawRecordId = governedSources.Min(x => x.MinimumRawRecordId);
        var maximumRawRecordId = governedSources.Max(x => x.MaximumRawRecordId);
        var sql = $$"""
            SELECT r.SourceFileId,r.StateCodeRaw,r.UtilizationTypeRaw,
                     (r.ParsedYear*10+r.ParsedQuarter) QuarterId,
                     SUM(r.ParsedPrescriptionCount) Volume,
                     SUM(CASE WHEN r.ParsedPrescriptionCount IS NULL THEN 1 ELSE 0 END) MissingCount
            FROM raw.MedicaidStateDrugUtilizationRaw r WITH (INDEX(PK_MedicaidStateDrugUtilizationRaw))
            WHERE r.RawRecordId BETWEEN {{minimumRawRecordId}} AND {{maximumRawRecordId}}
              AND r.SourceFileId IN ({{sourceIds}}) AND r.ParseStatus='Parsed'
              AND r.ParsedYear BETWEEN 2021 AND 2025 AND r.ParsedQuarter BETWEEN 1 AND 4
            GROUP BY r.SourceFileId,r.StateCodeRaw,r.UtilizationTypeRaw,r.ParsedYear,r.ParsedQuarter
            ORDER BY r.SourceFileId,r.StateCodeRaw,r.UtilizationTypeRaw,QuarterId;
            """;
        var raw = (await QueryAsync(sql, CancellationToken.None)).Select(ParseRawAggregate).ToArray();
        var baselines = (await QueryAsync("SELECT s.StateId,RTRIM(s.StateCode),w.PrescriptionNumerator,w.NormalizedWeight FROM research.HistoricalMarketWeight w JOIN core.State s ON s.StateCode=w.StateCode ORDER BY s.StateCode;", CancellationToken.None))
            .Select(ParseBaseline).ToArray();
        if (baselines.Length != 51) throw new InvalidDataException("Authoritative market context jurisdiction count mismatch.");
        var states = baselines.ToDictionary(x => x.StateCode, StringComparer.Ordinal);
        var quarterVolumes = raw
            .Where(x => AllowedUtilizationType(x.UtilizationType) && states.ContainsKey(Normalize(x.StateCode)))
            .GroupBy(x => new { StateCode = Normalize(x.StateCode), x.Quarter })
            .ToDictionary(x => (x.Key.StateCode, x.Key.Quarter), x => AggregateQuarter(x));
        return baselines.Select(x => new StateMarketContextSource(x.StateId, x.StateCode, x.Volume, x.Weight, 1m,
            quarterVolumes.Where(y => y.Key.StateCode == x.StateCode).OrderBy(y => y.Key.Quarter)
                .Select(y => new StateMarketQuarterVolume(y.Key.Quarter, y.Value.Volume,
                    y.Value.MissingCount > 0, y.Value.SourceFileCount)).ToArray())).ToArray();
    }

    private static async Task<GovernedSource[]> ExtractGovernedSourcesAsync()
    {
        const string sql = """
            SELECT sf.SourceFileId,MIN(r.RawRecordId),MAX(r.RawRecordId)
            FROM core.SourceFile sf
            JOIN core.DatasetVersion dv ON dv.DatasetVersionId=sf.DatasetVersionId
            JOIN raw.MedicaidStateDrugUtilizationRaw r WITH (INDEX(IX_MedicaidStateDrugUtilizationRaw_SourceFileId_SourceRowNumber))
              ON r.SourceFileId=sf.SourceFileId
            WHERE sf.SourceType='MedicaidStateDrugUtilization'
              AND dv.VersionCode='real-2021-2025-v1' AND dv.Status='Finalized' AND dv.ValidationStatus='Passed'
              AND EXISTS (SELECT 1 FROM research.ResearchSourceRegistration rs
                          WHERE rs.Sha256=sf.Sha256 AND rs.SourceType='CmsMedicaidStateDrugUtilization'
                            AND rs.IsSynthetic=0 AND rs.ValidationStatus='Validated'
                            AND rs.LocalFileName NOT LIKE '%dictionary%')
            GROUP BY sf.SourceFileId
            ORDER BY sf.SourceFileId;
            """;
        var lines = await QueryAsync(sql, CancellationToken.None);
        return lines.Select(x => x.Split('|')).Select(x => new GovernedSource(
            int.Parse(x[0], CultureInfo.InvariantCulture), long.Parse(x[1], CultureInfo.InvariantCulture),
            long.Parse(x[2], CultureInfo.InvariantCulture))).ToArray();
    }

    private static async Task<HashSet<int>> ExtractCandidateIdsAsync(int launchId, int asOfQuarter, CancellationToken cancellationToken)
    {
        var sql = $"SELECT s.StateId FROM research.AndaStateQuarterPanel p JOIN core.State s ON s.StateCode=p.StateCode WHERE p.AndaLaunchId={launchId} AND p.ObservationQuarter={asOfQuarter} AND p.HasEntered=0 AND p.IsCensored=0 ORDER BY s.StateId;";
        var lines = await QueryAsync(sql, cancellationToken);
        return lines.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToHashSet();
    }

    private static RawAggregate ParseRawAggregate(string line)
    {
        var x = line.Split('|');
        if (x.Length != 6) throw new InvalidDataException("Raw market aggregate width mismatch.");
        return new(int.Parse(x[0], CultureInfo.InvariantCulture), x[1], x[2],
            int.Parse(x[3], CultureInfo.InvariantCulture), x[4] == "NULL" ? null : long.Parse(x[4], CultureInfo.InvariantCulture),
            int.Parse(x[5], CultureInfo.InvariantCulture));
    }

    private static Baseline ParseBaseline(string line)
    {
        var x = line.Split('|');
        if (x.Length != 4) throw new InvalidDataException("Historical market baseline width mismatch.");
        return new(int.Parse(x[0], CultureInfo.InvariantCulture), x[1].Trim(),
            long.Parse(x[2], CultureInfo.InvariantCulture), decimal.Parse(x[3], CultureInfo.InvariantCulture));
    }

    private static QuarterAggregate AggregateQuarter(IEnumerable<RawAggregate> rows)
    {
        long volume = 0; var hasVolume = false; var missing = 0; var sources = new HashSet<int>();
        foreach (var row in rows)
        {
            if (row.Volume.HasValue) { volume = checked(volume + row.Volume.Value); hasVolume = true; }
            missing = checked(missing + row.MissingCount); sources.Add(row.SourceFileId);
        }
        return new(hasVolume ? volume : null, missing, sources.Count);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static bool AllowedUtilizationType(string value) => Normalize(value) is "FFSU" or "MCOU";

    private static async Task<string[]> QueryAsync(string sql, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("sqlcmd") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-S", "lpc:.", "-E", "-d", Database, "-X", "-b", "-W", "-h", "-1", "-s", "|", "-w", "65535", "-Q", "SET NOCOUNT ON; " + sql }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("sqlcmd is unavailable.");
        var rows = new List<string>();
        while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line) if (!string.IsNullOrWhiteSpace(line)) rows.Add(line);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode != 0) throw new InvalidOperationException($"Read-only market context extraction failed: {error.Trim()}");
        return rows.ToArray();
    }

    private void Guard(StateMarketContextRequest request)
    {
        if (!_allowed) throw new InvalidOperationException("Research development market context is disabled.");
        if (request.GenericLaunchId <= 0 || request.AsOfQuarter < 20211 || request.AsOfQuarter > 20253 || request.AsOfQuarter % 10 is < 1 or > 4)
            throw new ArgumentException("Market context launch and as-of quarter are invalid or beyond the research horizon.");
    }

    private sealed record GovernedSource(int SourceFileId, long MinimumRawRecordId, long MaximumRawRecordId);
    private sealed record RawAggregate(int SourceFileId, string StateCode, string UtilizationType, int Quarter, long? Volume, int MissingCount);
    private sealed record Baseline(int StateId, string StateCode, long Volume, decimal Weight);
    private sealed record QuarterAggregate(long? Volume, int MissingCount, int SourceFileCount);
}
