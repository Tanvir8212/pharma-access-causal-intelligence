using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PharmaAccess.Llm;

public sealed class TrustedDocumentIngestionService : IDocumentIngestionService
{
    private readonly string _root;
    private static readonly string[] AllowedFiles =
    [
        "README.md",
        "artifacts/final/predictive_metrics.json",
        "artifacts/final/causal_estimates.json",
        "artifacts/final/reproducibility_record.md",
        "artifacts/final/dataset_freeze_report.md",
        "artifacts/final/methodology.md",
        "artifacts/final/limitations.md",
        "artifacts/final/paper_metadata.json"
    ];

    public TrustedDocumentIngestionService(string root) => _root = Path.GetFullPath(root);

    public async Task<IReadOnlyList<ResearchChunk>> IngestAsync(CancellationToken cancellationToken = default)
    {
        var chunks = new List<ResearchChunk>();
        foreach (var relative in AllowedFiles)
        {
            var full = Path.GetFullPath(Path.Combine(_root, relative));
            if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full)) continue;
            var text = await File.ReadAllTextAsync(full, cancellationToken);
            var title = ExtractTitle(text) ?? Path.GetFileNameWithoutExtension(relative);
            chunks.AddRange(DeterministicChunker.Chunk(new ResearchDocument(
                Slug(relative), title, relative.Replace('\\', '/'), text)).Where(x => PromptInjectionGuard.IsSafe(x.Text)));
        }
        return chunks;
    }

    private static string? ExtractTitle(string text) =>
        text.Split('\n').Select(x => x.Trim()).FirstOrDefault(x => x.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim();
    private static string Slug(string value) => Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}

public static class DeterministicChunker
{
    public static IReadOnlyList<ResearchChunk> Chunk(ResearchDocument document, int maxCharacters = 1200)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCharacters, 100);
        var normalized = document.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var sections = Regex.Split(normalized, @"(?m)^(?=#{1,6}\s)")
            .Where(x => !string.IsNullOrWhiteSpace(x));
        var result = new List<ResearchChunk>();
        var index = 0;
        foreach (var sectionText in sections)
        {
            var lines = sectionText.Trim().Split('\n');
            var section = lines[0].StartsWith('#') ? lines[0].TrimStart('#', ' ') : "Document";
            var paragraphs = Regex.Split(sectionText.Trim(), @"\n\s*\n");
            var buffer = new StringBuilder();
            foreach (var paragraph in paragraphs)
            {
                if (buffer.Length > 0 && buffer.Length + paragraph.Length + 2 > maxCharacters)
                {
                    Add(buffer.ToString());
                    buffer.Clear();
                }
                if (buffer.Length > 0) buffer.Append("\n\n");
                buffer.Append(paragraph.Trim());
            }
            if (buffer.Length > 0) Add(buffer.ToString());

            void Add(string text)
            {
                var clean = Regex.Replace(text, @"[ \t]+", " ").Trim();
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clean))).ToLowerInvariant();
                result.Add(new(document.Identifier, document.Title, document.SourcePath, section, index++, clean, hash, document.TrustLevel));
            }
        }
        return result;
    }
}

public sealed class InMemoryRetrievalService : IRetrievalService
{
    private readonly IReadOnlyList<ResearchChunk> _chunks;
    public InMemoryRetrievalService(IReadOnlyList<ResearchChunk> chunks) => _chunks = chunks;

    public Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(string question, int limit = 5, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var terms = Terms(question);
        IReadOnlyList<RetrievedChunk> result = _chunks
            .Where(c => c.TrustLevel == "verified-public")
            .Select(c => new RetrievedChunk(c, Score(terms, c)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Chunk.DocumentIdentifier, StringComparer.Ordinal).ThenBy(x => x.Chunk.ChunkIndex)
            .Take(Math.Clamp(limit, 1, 10)).ToArray();
        return Task.FromResult(result);
    }

    private static double Score(HashSet<string> query, ResearchChunk chunk)
    {
        var body = Terms($"{chunk.Title} {chunk.Section} {chunk.Text}");
        return query.Count == 0 ? 0 : query.Count(body.Contains) / (double)query.Count;
    }
    private static HashSet<string> Terms(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9][a-z0-9.-]*")
            .Select(x => x.Value).Where(x => x.Length > 2 && !StopWords.Contains(x)).ToHashSet(StringComparer.Ordinal);
    private static readonly HashSet<string> StopWords = new(["the", "and", "for", "what", "was", "are", "does", "this", "that", "with", "from"], StringComparer.Ordinal);
}
