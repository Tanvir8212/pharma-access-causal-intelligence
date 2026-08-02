using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace PharmaAccess.Llm;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";
    public string Model { get; set; } = "gemini-flash-latest";
    public string? ApiKey { get; set; }
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/";
}

public enum GeminiFailureKind { InvalidRequest, Forbidden, ModelNotFound, RateLimited, Transient, Unexpected }

public sealed class GeminiHttpException : HttpRequestException
{
    public GeminiHttpException(HttpStatusCode statusCode, string model, GeminiFailureKind failureKind, string responseText)
        : base($"Gemini request failed with HTTP {(int)statusCode} ({statusCode}); model='{model}'; kind={failureKind}; response='{responseText}'.", null, statusCode)
    {
        Model = model;
        FailureKind = failureKind;
        ResponseText = responseText;
    }

    public string Model { get; }
    public GeminiFailureKind FailureKind { get; }
    public string ResponseText { get; }
}

public sealed class GeminiLanguageModelClient : ILanguageModelClient
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    public GeminiLanguageModelClient(HttpClient http, IOptions<GeminiOptions> options) { _http = http; _options = options.Value; }
    public string Provider => "Google Gemini";
    public string Model => NormalizeModel(_options.Model);
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable) throw new InvalidOperationException("Gemini is unavailable: configure Gemini__ApiKey via an environment variable or .NET user secrets.");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{Uri.EscapeDataString(Model)}:generateContent");
        request.Headers.Add("x-goog-api-key", _options.ApiKey);
        request.Content = JsonContent.Create(
            new { contents = new[] { new { parts = new[] { new { text = prompt } } } }, generationConfig = new { responseMimeType = "application/json", temperature = 0 } });
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new GeminiHttpException(response.StatusCode, Model, Classify(response.StatusCode), Sanitize(body, _options.ApiKey!));
        }
        using var json = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        return json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()
               ?? throw new InvalidOperationException("Gemini returned an empty response.");
    }

    private static string NormalizeModel(string configuredModel)
    {
        var model = configuredModel.Trim();
        if (model.StartsWith("models/", StringComparison.OrdinalIgnoreCase)) model = model["models/".Length..];
        if (string.IsNullOrWhiteSpace(model) || model.Contains('/') || model.Contains('\\'))
            throw new InvalidOperationException("Gemini model must be an unqualified model identifier.");
        if (RetiredModels.Contains(model))
            throw new InvalidOperationException($"Gemini model '{model}' is retired and cannot be configured.");
        return model;
    }

    private static readonly HashSet<string> RetiredModels =
        new(["gemini-pro", "gemini-1.5-flash", "gemini-1.5-pro", "gemini-2.0-flash"], StringComparer.OrdinalIgnoreCase);

    private static GeminiFailureKind Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => GeminiFailureKind.InvalidRequest,
        HttpStatusCode.Forbidden => GeminiFailureKind.Forbidden,
        HttpStatusCode.NotFound => GeminiFailureKind.ModelNotFound,
        HttpStatusCode.TooManyRequests => GeminiFailureKind.RateLimited,
        >= HttpStatusCode.InternalServerError => GeminiFailureKind.Transient,
        _ => GeminiFailureKind.Unexpected
    };

    private static string Sanitize(string responseText, string apiKey)
    {
        var sanitized = responseText.Replace(apiKey, "[REDACTED]", StringComparison.Ordinal);
        sanitized = Regex.Replace(sanitized, @"AIza[0-9A-Za-z_-]{20,}", "[REDACTED]");
        sanitized = Regex.Replace(sanitized, @"[\u0000-\u001F\u007F]+", " ").Trim();
        return sanitized.Length <= 1000 ? sanitized : sanitized[..1000] + "…";
    }
}
