using System.Text.Json;
using System.Text.RegularExpressions;

namespace PharmaAccess.Llm;

public static class StructuredResponseParser
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    public static bool TryParse(string json, out LlmStructuredResponse? response)
    {
        try
        {
            response = JsonSerializer.Deserialize<LlmStructuredResponse>(json, Options);
            return response is not null && !string.IsNullOrWhiteSpace(response.Answer);
        }
        catch (JsonException) { response = null; return false; }
    }
}

public static partial class PromptInjectionGuard
{
    [GeneratedRegex(@"(?i)(ignore|disregard|override).{0,40}(instruction|prompt|system)|system\s*prompt|developer\s*message|reveal.{0,20}(secret|key)|<\|")]
    private static partial Regex InjectionPattern();
    public static bool IsSafe(string text) => !InjectionPattern().IsMatch(text);
}

public sealed class StrictLlmResponseValidator : ILlmResponseValidator
{
    private static readonly string[] Prohibited = ["proves", "definitely causes", "clinical recommendation", "should prescribe", "should treat"];
    private static readonly string[] RequiredCausalQualifiers = ["observational estimate", "under stated assumptions", "does not establish causation"];
    private static readonly string[] GovernedModelNames = ["lightgbm", "xgboost", "fasttree", "random forest", "logistic regression"];

    public ValidationResult Validate(LlmStructuredResponse response, IReadOnlyList<RetrievedChunk> evidence)
    {
        var errors = new List<string>();
        var ids = evidence.Select(SourceId).ToHashSet(StringComparer.Ordinal);
        foreach (var citation in response.Citations ?? [])
            if (!ids.Contains(citation)) errors.Add($"Citation '{citation}' was not retrieved.");
        if ((response.Citations ?? []).Length == 0) errors.Add("The response has no evidence citation.");

        foreach (var claim in response.NumericalClaims ?? [])
        {
            var source = evidence.FirstOrDefault(x => SourceId(x) == claim.SourceIdentifier);
            if (source is null) errors.Add($"Numerical claim source '{claim.SourceIdentifier}' was not retrieved.");
            else if (!ExactNumberTokens(source.Chunk.Text).Contains(NormalizeNumber(claim.Value)))
                errors.Add($"Numerical claim '{claim.Value}' is not present in its cited source.");
            if (!(response.Citations ?? []).Contains(claim.SourceIdentifier, StringComparer.Ordinal))
                errors.Add($"Numerical claim source '{claim.SourceIdentifier}' is not included in citations.");
        }

        var answerNumbers = ExactNumberTokens(response.Answer);
        var supportedNumbers = evidence.SelectMany(x => ExactNumberTokens(x.Chunk.Text)).ToHashSet(StringComparer.Ordinal);
        foreach (var number in answerNumbers.Where(x => !supportedNumbers.Contains(x)))
            errors.Add($"Answer number '{number}' is unsupported.");

        var lower = response.Answer.ToLowerInvariant();
        var evidenceText = string.Join('\n', evidence.Select(x => x.Chunk.Text)).ToLowerInvariant();
        foreach (var model in GovernedModelNames.Where(lower.Contains).Where(model => !evidenceText.Contains(model, StringComparison.Ordinal)))
            errors.Add($"Model name '{model}' is unsupported.");
        if (Prohibited.Any(lower.Contains)) errors.Add("Causal or clinical language is overstated.");
        var causal = response.CausalLanguageClassification?.Equals("causal", StringComparison.OrdinalIgnoreCase) == true ||
                     lower.Contains("cause", StringComparison.Ordinal);
        if (causal && !RequiredCausalQualifiers.Any(lower.Contains))
            errors.Add("Causal discussion lacks a required methodological qualifier.");
        if (!PromptInjectionGuard.IsSafe(response.Answer)) errors.Add("Response contains prompt-injection language.");
        return new(errors.Count == 0, errors.Count == 0 ? "validated" : "rejected", errors.ToArray());
    }

    public static string SourceId(RetrievedChunk chunk) => $"{chunk.Chunk.DocumentIdentifier}#{chunk.Chunk.ChunkIndex}";
    private static HashSet<string> ExactNumberTokens(string value) =>
        Regex.Matches(value, @"(?<![\w])[-−+]?(?:\d{1,3}(?:,\d{3})+|\d+)(?:\.\d+)?%?(?![\w])")
            .Select(x => NormalizeNumber(x.Value)).ToHashSet(StringComparer.Ordinal);
    private static string NormalizeNumber(string value) => value.Trim().Replace("−", "-", StringComparison.Ordinal).Replace(",", "", StringComparison.Ordinal);
}
