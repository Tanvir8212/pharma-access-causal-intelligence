using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using Xunit;

namespace PharmaAccess.Llm.Tests;

public sealed class GeminiIntegrationTests
{
    [Fact]
    public async Task Optional_live_structured_response()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("PHARMAACCESS_RUN_GEMINI_INTEGRATION"), "true", StringComparison.OrdinalIgnoreCase))
            return;
        var key = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) return;
        await RunLiveAsync(key);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task RunLiveAsync(string key)
    {
        var model = Environment.GetEnvironmentVariable("Gemini__Model") ?? "gemini-flash-latest";
        var client = new GeminiLanguageModelClient(new HttpClient { BaseAddress = new("https://generativelanguage.googleapis.com/") },
            Options.Create(new GeminiOptions { ApiKey = key, Model = model }));
        var output = await client.GenerateAsync("Return JSON only: {\"answer\":\"available\",\"citations\":[],\"numericalClaims\":[],\"causalLanguageClassification\":\"predictive\",\"warnings\":[],\"confidenceSupportStatus\":\"supported\"}");
        Assert.True(StructuredResponseParser.TryParse(output, out _));
    }
}
