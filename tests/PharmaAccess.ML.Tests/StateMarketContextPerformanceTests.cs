using System.Diagnostics;
using System.Globalization;
using PharmaAccess.Application.MachineLearning;
using Xunit;
using Xunit.Abstractions;

namespace PharmaAccess.ML.Tests;

public sealed class StateMarketContextPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Optimized_aggregate_has_exact_all_jurisdiction_parity_with_approved_reference()
    {
        var optimized = await StateMarketContextService.ExtractMarketAsync();
        var reference = await ReferenceSourcesAsync();

        Assert.Equal(51, optimized.Count);
        Assert.Equal(51, reference.Count);
        foreach (var asOfQuarter in new[] { 20211, 20214, 20244, 20253 })
        {
            var expected = StateMarketContextCalculator.Calculate(asOfQuarter, reference);
            var actual = StateMarketContextCalculator.Calculate(asOfQuarter, optimized);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public async Task Cold_and_warm_application_cache_are_measured()
    {
        var stageWatch = Stopwatch.StartNew();
        var sources = await StateMarketContextService.ExtractMarketAsync();
        stageWatch.Stop();
        var extractionElapsed = stageWatch.Elapsed;
        stageWatch.Restart();
        var calculated = StateMarketContextCalculator.Calculate(20244, sources);
        stageWatch.Stop();
        output.WriteLine($"SqlTransferAndNormalizationMilliseconds={extractionElapsed.TotalMilliseconds:0.###}");
        output.WriteLine($"CalculationMilliseconds={stageWatch.Elapsed.TotalMilliseconds:0.###}");
        Assert.Equal(51, calculated.Count);

        var service = new StateMarketContextService(true, true);
        var watch = Stopwatch.StartNew();
        var cold = await service.GetAsync(new(291, 20244));
        watch.Stop();
        var coldElapsed = watch.Elapsed;
        watch.Restart();
        var warm = await service.GetAsync(new(291, 20244));
        watch.Stop();

        output.WriteLine($"ColdMilliseconds={coldElapsed.TotalMilliseconds:0.###}");
        output.WriteLine($"WarmMilliseconds={watch.Elapsed.TotalMilliseconds:0.###}");
        Assert.Equal(51, cold.ComparisonPopulationCount);
        Assert.Equal(cold.GenericLaunchId, warm.GenericLaunchId);
        Assert.Equal(cold.AsOfQuarter, warm.AsOfQuarter);
        Assert.Equal(cold.ComparisonPopulationCount, warm.ComparisonPopulationCount);
        Assert.Equal(cold.States, warm.States);
        Assert.True(coldElapsed < TimeSpan.FromSeconds(30), $"Cold load took {coldElapsed}.");
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Warm load took {watch.Elapsed}.");
    }

    private static async Task<IReadOnlyList<StateMarketContextSource>> ReferenceSourcesAsync()
    {
        var ids = await QueryAsync("""
            SELECT sf.SourceFileId FROM core.SourceFile sf
            JOIN core.DatasetVersion dv ON dv.DatasetVersionId=sf.DatasetVersionId
            WHERE sf.SourceType='MedicaidStateDrugUtilization'
              AND dv.VersionCode='real-2021-2025-v1' AND dv.Status='Finalized' AND dv.ValidationStatus='Passed'
              AND EXISTS (SELECT 1 FROM research.ResearchSourceRegistration rs WHERE rs.Sha256=sf.Sha256
                AND rs.SourceType='CmsMedicaidStateDrugUtilization' AND rs.IsSynthetic=0
                AND rs.ValidationStatus='Validated' AND rs.LocalFileName NOT LIKE '%dictionary%');
            """);
        var sourceIds = string.Join(',', ids.Select(x => int.Parse(x, CultureInfo.InvariantCulture)));
        var rows = await QueryAsync($$"""
            WITH QuarterVolume AS (
              SELECT s.StateId,(r.ParsedYear*10+r.ParsedQuarter) QuarterId,
                     SUM(r.ParsedPrescriptionCount) Volume,
                     SUM(CASE WHEN r.ParsedPrescriptionCount IS NULL THEN 1 ELSE 0 END) MissingCount,
                     COUNT(DISTINCT r.SourceFileId) SourceFileCount
              FROM raw.MedicaidStateDrugUtilizationRaw r
              JOIN core.State s ON s.StateCode=UPPER(LTRIM(RTRIM(r.StateCodeRaw)))
              WHERE r.ParseStatus='Parsed'
                AND UPPER(LTRIM(RTRIM(r.UtilizationTypeRaw))) IN ('FFSU','MCOU')
                AND r.SourceFileId IN ({{sourceIds}})
              GROUP BY s.StateId,(r.ParsedYear*10+r.ParsedQuarter)
            )
            SELECT s.StateId,RTRIM(s.StateCode),w.PrescriptionNumerator,w.NormalizedWeight,
                   q.QuarterId,q.Volume,q.MissingCount,q.SourceFileCount
            FROM research.HistoricalMarketWeight w JOIN core.State s ON s.StateCode=w.StateCode
            LEFT JOIN QuarterVolume q ON q.StateId=s.StateId
            ORDER BY s.StateCode,q.QuarterId;
            """);
        return rows.Select(ParseReference).GroupBy(x => new { x.StateId, x.StateCode, x.BaselineVolume, x.Weight })
            .Select(x => new StateMarketContextSource(x.Key.StateId, x.Key.StateCode, x.Key.BaselineVolume, x.Key.Weight, 1m,
                x.Where(y => y.Quarter.HasValue).Select(y => new StateMarketQuarterVolume(
                    y.Quarter!.Value, y.Volume, y.MissingCount > 0, y.SourceFileCount)).ToArray()))
            .OrderBy(x => x.StateCode, StringComparer.Ordinal).ToArray();
    }

    private static ReferenceRow ParseReference(string line)
    {
        var x = line.Split('|');
        return new(int.Parse(x[0], CultureInfo.InvariantCulture), x[1].Trim(),
            long.Parse(x[2], CultureInfo.InvariantCulture), decimal.Parse(x[3], CultureInfo.InvariantCulture),
            x[4] == "NULL" ? null : int.Parse(x[4], CultureInfo.InvariantCulture),
            x[5] == "NULL" ? null : long.Parse(x[5], CultureInfo.InvariantCulture),
            x[6] == "NULL" ? 0 : int.Parse(x[6], CultureInfo.InvariantCulture),
            x[7] == "NULL" ? 0 : int.Parse(x[7], CultureInfo.InvariantCulture));
    }

    private static async Task<string[]> QueryAsync(string sql)
    {
        var start = new ProcessStartInfo("sqlcmd") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-S", "lpc:.", "-E", "-d", "PharmaAccessCausalIntelligence_ResearchDev", "-X", "-b", "-W", "-h", "-1", "-s", "|", "-w", "65535", "-Q", "SET NOCOUNT ON; " + sql }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("sqlcmd is unavailable.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException(error.Trim());
        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed record ReferenceRow(int StateId, string StateCode, long BaselineVolume, decimal Weight,
        int? Quarter, long? Volume, int MissingCount, int SourceFileCount);
}
