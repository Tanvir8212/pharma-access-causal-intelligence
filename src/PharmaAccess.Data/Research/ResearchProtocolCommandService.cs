using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Domain.Research;

namespace PharmaAccess.Data.Research;

public sealed record RealProtocolDocument(string ProtocolCode, string ProtocolVersion, string Title, string ResearchQuestion, JsonElement StudyPeriod, string Population, string DrugIdentityPolicy, string StateEligibilityPolicy, string TerritoryPolicy, string EntryDefinition, JsonElement DistributionDefinitions, JsonElement Predictive, JsonElement Causal, JsonElement DataPolicies, JsonElement SensitivityAnalyses, JsonElement LeakagePrevention, JsonElement Exclusions, string ProtocolDeviationPolicy, string HumanApproval, JsonElement ReferenceIds, string[] RemainingReviewQuestions, string? AmendsProtocolVersion = null, string? Status = null, JsonElement? AuthoritativeIdentitySources = null, JsonElement? MappingHierarchy = null, JsonElement? MappingRules = null, JsonElement? Reproducibility = null);

public sealed class ResearchProtocolCommandService(PharmaAccessDbContext db)
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static RealProtocolDocument ReadDocument(string path) => JsonSerializer.Deserialize<RealProtocolDocument>(File.ReadAllText(path), Json) ?? throw new InvalidOperationException("Protocol document is invalid.");

    public async Task<ResearchProtocol> CreateDraftAsync(string path, CancellationToken cancellationToken = default)
    {
        var d = ReadDocument(path); var raw = File.ReadAllText(path); if (raw.Contains("HUMAN_DECISION_REQUIRED", StringComparison.Ordinal)) throw new InvalidOperationException("Protocol contains unresolved human decisions and cannot be created.");
        var existing = await db.ResearchProtocols.SingleOrDefaultAsync(x => x.ProtocolCode == d.ProtocolCode && x.ProtocolVersion == d.ProtocolVersion, cancellationToken); if (existing is not null) throw new InvalidOperationException(existing.DefinitionHash == Build(d).DefinitionHash ? "The identical protocol already exists." : "A conflicting protocol code/version already exists.");
        var protocol = Build(d); db.ResearchProtocols.Add(protocol); await db.SaveChangesAsync(cancellationToken); return protocol;
    }

    public async Task<ResearchProtocol> SubmitAsync(string code, string version, CancellationToken cancellationToken = default)
    {
        var protocol = await db.ResearchProtocols.SingleOrDefaultAsync(x => x.ProtocolCode == code && x.ProtocolVersion == version, cancellationToken) ?? throw new InvalidOperationException("Protocol does not exist."); protocol.SubmitForReview(); db.ResearchProtocolApprovals.Add(new() { ResearchProtocolId = protocol.ResearchProtocolId, Decision = "SubmitForReview", Actor = "human-confirmed-submitter", Reason = "Submitted for independent human review; not approved.", DecidedAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(cancellationToken); return protocol;
    }

    public Task<ResearchProtocol?> GetAsync(string code, string version, CancellationToken cancellationToken = default) => db.ResearchProtocols.AsNoTracking().SingleOrDefaultAsync(x => x.ProtocolCode == code && x.ProtocolVersion == version, cancellationToken);

    private static ResearchProtocol Build(RealProtocolDocument d)
    {
        static string Raw(JsonElement value) => value.GetRawText();
        var ids = d.ReferenceIds; int I(string name) => ids.GetProperty(name).GetInt32();
        return new(d.ProtocolCode, d.ProtocolVersion, d.Title, d.ResearchQuestion, "NextQuarterStateEntry", d.Causal.GetProperty("treatment").GetString() + " -> " + d.Causal.GetProperty("outcome").GetString(), d.Predictive.GetProperty("primaryMetric").GetString()!, string.Join(',', d.Predictive.GetProperty("secondaryMetrics").EnumerateArray().Select(x => x.GetString())), d.Causal.GetProperty("estimand").GetString()!, d.Causal.GetProperty("effectScale").GetString()!, d.Causal.GetProperty("primaryEstimator").GetString()!, string.Join(',', d.Causal.GetProperty("secondaryEstimators").EnumerateArray().Select(x => x.GetString())), d.Population, Raw(d.StudyPeriod), JsonSerializer.Serialize(new { d.AmendsProtocolVersion, d.DrugIdentityPolicy, d.StateEligibilityPolicy, d.TerritoryPolicy, d.EntryDefinition, d.DistributionDefinitions, d.Predictive, d.Causal, d.LeakagePrevention, d.AuthoritativeIdentitySources, d.MappingHierarchy, d.MappingRules, d.Reproducibility }), JsonSerializer.Serialize(new { d.Exclusions, d.DataPolicies, d.ProtocolDeviationPolicy, d.HumanApproval }), "state-eligibility-v1", "any-positive-v1", "historical-market-weight-v1", I("featureSetVersionId"), I("predictiveSplitDefinitionId"), I("dagVersionId"), I("adjustmentSetVersionId"), I("treatmentDefinitionVersionId"), I("outcomeDefinitionVersionId"), d.DataPolicies.GetProperty("missing").GetString()!, d.DataPolicies.GetProperty("censoring").GetString()!, "Prespecified primary analysis; exploratory subgroup multiplicity reported", Raw(d.SensitivityAnalyses), "Prespecified region, approval cohort and historical-volume groups with minimum support", "No outcome-driven change; material deviations require a new version", DateTime.UtcNow);
    }
}
