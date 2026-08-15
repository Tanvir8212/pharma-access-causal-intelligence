using System.Globalization;
using System.Text;
using PharmaAccess.Application.MachineLearning;

namespace PharmaAccess.Llm;

internal static class StructuredPredictionExplanation
{
    public const string ProseInstruction = "Return concise readable natural-language prose only. Do not return JSON, Markdown JSON, a code fence, a field list, or reproduce the grounding object. Summarize only the supplied ranking, probabilities, target quarter, threshold status, limitations, and governance. Do not invent reasons, feature attribution, causal explanations, commercial recommendations, confidence intervals, or facts absent from the grounding.";

    public static string SelectOrFallback(string? generated, string mode, string launch, int targetQuarter,
        IReadOnlyList<HistoricalReplayStateResult> states, double threshold, ModelApprovalStatus governance,
        bool productionApproved, bool champion)
    {
        return IsReadableProse(generated)
            ? generated!.Trim()
            : BuildFallback(mode, launch, targetQuarter, states, threshold, governance, productionApproved, champion);
    }

    internal static bool IsReadableProse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal) || text.Contains("```json", StringComparison.OrdinalIgnoreCase)) return false;
        if ((text.StartsWith('{') && text.EndsWith('}')) || (text.StartsWith('[') && text.EndsWith(']'))) return false;
        return true;
    }

    private static string BuildFallback(string mode, string launch, int targetQuarter,
        IReadOnlyList<HistoricalReplayStateResult> states, double threshold, ModelApprovalStatus governance,
        bool productionApproved, bool champion)
    {
        if (states.Count == 0) return "No verified candidate jurisdictions were available to summarize.";
        var ranked = states.OrderBy(x => x.Rank).Take(5).ToArray();
        var first = ranked[0];
        var text = new StringBuilder();
        text.Append("For ").Append(launch).Append(", ").Append(first.StateName)
            .Append(" is the highest-ranked jurisdiction for first observed eligible Medicaid utilization in ")
            .Append(Quarter(targetQuarter)).Append(", with an estimated probability of ").Append(Percent(first.Probability)).Append('.');
        if (ranked.Length > 1)
        {
            text.Append(" The next highest-ranked jurisdictions are ");
            text.Append(string.Join(", ", ranked.Skip(1).Select(x => $"{x.StateName} at {Percent(x.Probability)}")));
            text.Append('.');
        }
        var meeting = states.Count(x => x.AboveSelectedThreshold);
        text.AppendLine().AppendLine();
        text.Append(meeting == 0 ? "None of the evaluated jurisdictions meet" : $"{meeting} of the evaluated jurisdictions meet")
            .Append(" the selected research threshold of ").Append(Percent(threshold)).Append('.');
        text.AppendLine().AppendLine();
        text.Append("These are model-estimated probabilities from ")
            .Append(mode == HistoricalReplayMode.Name ? "a historical research replay" : "a dynamic as-of research prediction")
            .Append(". They are not commercial launch recommendations, do not establish patient consumption, and are not causal findings. The model is ")
            .Append(governance).Append(productionApproved ? " and production approved" : " and not production approved")
            .Append(champion ? "; it is assigned Champion." : "; it is not assigned Champion.");
        return text.ToString();
    }

    private static string Percent(double value) => (value * 100).ToString("0.00", CultureInfo.InvariantCulture) + "%";
    private static string Quarter(int value) => $"{value / 10} Q{value % 10}";
}
