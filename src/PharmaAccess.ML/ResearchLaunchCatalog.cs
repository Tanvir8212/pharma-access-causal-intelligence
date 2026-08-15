using System.Diagnostics;
using System.Globalization;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.ML;

public sealed class ResearchLaunchCatalogService(bool development, bool enabled) : IResearchLaunchCatalogService
{
    private readonly Lazy<Task<IReadOnlyList<HistoricalReplayLaunch>>> _cache = new(LoadAsync, true);

    public Task<IReadOnlyList<HistoricalReplayLaunch>> GetLaunchesAsync(CancellationToken token = default)
    {
        if (!development || !enabled) throw new InvalidOperationException("Research launch catalog is disabled.");
        return _cache.Value.WaitAsync(token);
    }

    private static async Task<IReadOnlyList<HistoricalReplayLaunch>> LoadAsync()
    {
        var processStart = new ProcessStartInfo("sqlcmd") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[]
        {
            "-S", "lpc:.", "-E", "-d", "PharmaAccessCausalIntelligence_ResearchDev", "-X", "-b", "-W", "-h", "-1", "-s", "|", "-w", "65535", "-Q",
            "SET NOCOUNT ON; SELECT al.AndaLaunchId,RTRIM(al.GenericLaunchCode),RTRIM(al.Anda),CONVERT(char(10),al.ApprovalDate,23),al.ApprovalPageYear,al.SequenceNumber,f.DrugsFdaProductId,RTRIM(f.DrugNameRaw),RTRIM(f.ActiveIngredientRaw),RTRIM(f.FormRaw),RTRIM(f.StrengthRaw),RTRIM(f.NormalizedProductNumber) FROM research.AndaLaunch al LEFT JOIN reference.DrugsFdaProduct f ON f.DrugsFdaSnapshotId=4 AND al.Anda NOT LIKE '%[^0-9]%' AND f.NormalizedApplicationNumber NOT LIKE '%[^0-9]%' AND TRY_CONVERT(bigint,al.Anda)=TRY_CONVERT(bigint,f.NormalizedApplicationNumber) WHERE EXISTS (SELECT 1 FROM research.AndaStateQuarterPanel p WHERE p.AndaLaunchId=al.AndaLaunchId) ORDER BY al.AndaLaunchId,f.NormalizedProductNumber,f.DrugsFdaProductId;"
        }) processStart.ArgumentList.Add(argument);

        using var process = Process.Start(processStart) ?? throw new InvalidOperationException("sqlcmd is unavailable.");
        var raw = new List<Row>();
        while (await process.StandardOutput.ReadLineAsync() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split('|');
            if (values.Length != 12) throw new InvalidDataException("Launch catalog projection width mismatch.");
            raw.Add(new(
                int.Parse(values[0], CultureInfo.InvariantCulture), values[1].Trim(), values[2].Trim(),
                DateOnly.ParseExact(values[3], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                int.Parse(values[4], CultureInfo.InvariantCulture), int.Parse(values[5], CultureInfo.InvariantCulture),
                Value(values[6]), Value(values[7]), Value(values[8]), Value(values[9]), Value(values[10])));
        }
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException($"Read-only launch catalog extraction failed: {error.Trim()}");

        var result = raw.GroupBy(x => x.Id).Select(group =>
        {
            var first = group.First();
            string[] Distinct(Func<Row, string?> selector) => group.Select(selector).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
            var names = Distinct(x => x.Name);
            var ingredients = Distinct(x => x.Ingredient);
            var forms = Distinct(x => x.Form);
            var strengths = Distinct(x => x.Strength);
            var primary = names.FirstOrDefault() ?? $"ANDA {first.Anda}";
            return new HistoricalReplayLaunch(first.Id, $"{primary} — ANDA {first.Anda} — {first.Date:yyyy-MM-dd}", first.Code, first.Anda, first.Date, first.Year, first.Sequence)
            {
                DrugNames = names, ActiveIngredients = ingredients, Forms = forms, Strengths = strengths,
                ProductCount = group.Count(x => x.ProductId is not null)
            };
        }).OrderBy(x => x.GenericLaunchId).ToArray();

        if (result.Length != 261 || result.Select(x => x.GenericLaunchId).Distinct().Count() != 261 || result.Any(x => string.IsNullOrWhiteSpace(x.GenericLaunchCode) || string.IsNullOrWhiteSpace(x.Anda)))
            throw new InvalidDataException("Authoritative launch catalog invariants failed.");
        return result;
    }

    private static string? Value(string value) => value.Trim() is "NULL" or "" ? null : value.Trim();
    private sealed record Row(int Id, string Code, string Anda, DateOnly Date, int Year, int Sequence, string? ProductId, string? Name, string? Ingredient, string? Form, string? Strength);
}
