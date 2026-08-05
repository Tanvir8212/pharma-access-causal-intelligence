using Microsoft.Extensions.Configuration;

namespace PharmaAccess.Api.IntegrationTests;

internal static class IntegrationTestConfiguration
{
    public static void Apply(
        IConfigurationBuilder configuration,
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        configuration.Sources.Clear();
        var values = new Dictionary<string, string?>
        {
            ["Authentication:Mode"] = "DevelopmentHeader",
            ["ConnectionStrings:PharmaAccess"] = string.Empty,
            ["Gemini:ApiKey"] = string.Empty,
            ["Gemini:Endpoint"] = "https://generativelanguage.googleapis.com/",
            ["Gemini:Model"] = "gemini-flash-latest"
        };

        if (overrides is not null)
            foreach (var item in overrides)
                values[item.Key] = item.Value;

        configuration.AddInMemoryCollection(values);
    }
}
