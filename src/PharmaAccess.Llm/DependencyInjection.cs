using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PharmaAccess.Llm;

public static class DependencyInjection
{
    public static IServiceCollection AddPharmaAccessResearchAssistant(this IServiceCollection services, IConfiguration configuration, string contentRoot)
    {
        services.AddOptions<GeminiOptions>().Bind(configuration.GetSection(GeminiOptions.SectionName));
        services.PostConfigure<GeminiOptions>(o =>
        {
            o.ApiKey ??= Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!Uri.TryCreate(o.Endpoint, UriKind.Absolute, out var endpoint) ||
                endpoint != new Uri("https://generativelanguage.googleapis.com/"))
                throw new InvalidOperationException("Gemini endpoint must be https://generativelanguage.googleapis.com/.");
        });
        services.AddHttpClient<ILanguageModelClient, GeminiLanguageModelClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IDocumentIngestionService>(_ => new TrustedDocumentIngestionService(contentRoot));
        services.AddSingleton<IRetrievalService>(sp =>
        {
            var chunks = sp.GetRequiredService<IDocumentIngestionService>().IngestAsync().GetAwaiter().GetResult();
            return new InMemoryRetrievalService(chunks);
        });
        services.AddSingleton<ILlmResponseValidator, StrictLlmResponseValidator>();
        services.AddScoped<ResearchAssistantService>();
        return services;
    }
}
