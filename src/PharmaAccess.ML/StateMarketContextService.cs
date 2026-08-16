using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed class StateMarketContextService(bool development, bool enabled) : IStateMarketContextService
{
    private const string Database = "PharmaAccessCausalIntelligence_ResearchDev";
    private readonly bool _allowed = development && enabled;
    private readonly ConcurrentDictionary<int, Lazy<Task<IReadOnlyList<StateMarketContext>>>> _marketCache = new();

    public async Task<StateMarketContextResponse> GetAsync(StateMarketContextRequest request, CancellationToken cancellationToken = default)
    {
        Guard(request);
        var fullPopulation = await _marketCache.GetOrAdd(request.AsOfQuarter,
            quarter => new(async () => StateMarketContextCalculator.Calculate(quarter, await ExtractMarketAsync(quarter)),
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

    private async Task<IReadOnlyList<StateMarketContextSource>> ExtractMarketAsync(int asOfQuarter)
    {
        var quarters = StateMarketContextCalculator.PreviousQuarters(asOfQuarter, 8);
        var minimum = quarters[0];
        var governedSourceIds = await ExtractGovernedSourceIdsAsync();
        if (governedSourceIds.Length == 0) throw new InvalidDataException("No governed real Medicaid source files are available.");
        var sourceIds = string.Join(',', governedSourceIds);
        var sql = $$"""
            WITH QuarterVolume AS (
              SELECT s.StateId, (r.ParsedYear*10+r.ParsedQuarter) QuarterId,
                     SUM(r.ParsedPrescriptionCount) Volume,
                     SUM(CASE WHEN r.ParsedPrescriptionCount IS NULL THEN 1 ELSE 0 END) MissingCount,
                     COUNT(DISTINCT r.SourceFileId) SourceFileCount
              FROM raw.MedicaidStateDrugUtilizationRaw r
              JOIN core.State s ON s.StateCode=UPPER(LTRIM(RTRIM(r.StateCodeRaw)))
              WHERE (r.ParsedYear*10+r.ParsedQuarter) BETWEEN {{minimum}} AND {{asOfQuarter}}
                AND r.ParseStatus='Parsed'
                AND UPPER(LTRIM(RTRIM(r.UtilizationTypeRaw))) IN ('FFSU','MCOU')
                AND r.SourceFileId IN ({{sourceIds}})
              GROUP BY s.StateId,(r.ParsedYear*10+r.ParsedQuarter)
            )
            SELECT s.StateId,RTRIM(s.StateCode),w.PrescriptionNumerator,w.NormalizedWeight,
                   q.QuarterId,q.Volume,q.MissingCount,q.SourceFileCount
            FROM research.HistoricalMarketWeight w JOIN core.State s ON s.StateCode=w.StateCode
            LEFT JOIN QuarterVolume q ON q.StateId=s.StateId
            ORDER BY s.StateCode,q.QuarterId;
            """;
        var lines = await QueryAsync(sql, CancellationToken.None);
        var raw = lines.Select(ParseMarket).GroupBy(x => new { x.StateId, x.StateCode, x.Volume, x.Weight })
            .OrderBy(x => x.Key.StateCode, StringComparer.Ordinal).ToArray();
        if (raw.Length != 51) throw new InvalidDataException("Authoritative market context jurisdiction count mismatch.");
        return raw.Select(x => new StateMarketContextSource(x.Key.StateId, x.Key.StateCode, x.Key.Volume, x.Key.Weight,
            1m, x.Where(y => y.Quarter.HasValue)
                .Select(y => new StateMarketQuarterVolume(y.Quarter!.Value, y.QuarterVolume, y.MissingCount > 0, y.SourceFileCount)).ToArray())).ToArray();
    }

    private static async Task<int[]> ExtractGovernedSourceIdsAsync()
    {
        const string sql = """
            SELECT sf.SourceFileId
            FROM core.SourceFile sf
            JOIN core.DatasetVersion dv ON dv.DatasetVersionId=sf.DatasetVersionId
            WHERE sf.SourceType='MedicaidStateDrugUtilization'
              AND dv.VersionCode='real-2021-2025-v1' AND dv.Status='Finalized' AND dv.ValidationStatus='Passed'
              AND EXISTS (SELECT 1 FROM research.ResearchSourceRegistration rs
                          WHERE rs.Sha256=sf.Sha256 AND rs.SourceType='CmsMedicaidStateDrugUtilization'
                            AND rs.IsSynthetic=0 AND rs.ValidationStatus='Validated'
                            AND rs.LocalFileName NOT LIKE '%dictionary%')
            ORDER BY sf.SourceFileId;
            """;
        var lines = await QueryAsync(sql, CancellationToken.None);
        return lines.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
    }

    private static async Task<HashSet<int>> ExtractCandidateIdsAsync(int launchId, int asOfQuarter, CancellationToken cancellationToken)
    {
        var sql = $"SELECT s.StateId FROM research.AndaStateQuarterPanel p JOIN core.State s ON s.StateCode=p.StateCode WHERE p.AndaLaunchId={launchId} AND p.ObservationQuarter={asOfQuarter} AND p.HasEntered=0 AND p.IsCensored=0 ORDER BY s.StateId;";
        var lines = await QueryAsync(sql, cancellationToken);
        return lines.Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToHashSet();
    }

    private static MarketRow ParseMarket(string line)
    {
        var x = line.Split('|');
        if (x.Length != 8) throw new InvalidDataException("Research market projection width mismatch.");
        int I(int i) => int.Parse(x[i], CultureInfo.InvariantCulture);
        long L(int i) => long.Parse(x[i], CultureInfo.InvariantCulture);
        decimal D(int i) => decimal.Parse(x[i], CultureInfo.InvariantCulture);
        var missing = x[4] == "NULL";
        return new(I(0), x[1].Trim(), L(2), D(3), missing ? null : I(4),
            x[5] == "NULL" ? null : L(5), x[6] == "NULL" ? 0 : I(6), x[7] == "NULL" ? 0 : I(7));
    }

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

    private sealed record MarketRow(int StateId, string StateCode, long Volume, decimal Weight, int? Quarter, long? QuarterVolume, int MissingCount, int SourceFileCount);
}
