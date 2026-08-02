using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Data.Entities;

namespace PharmaAccess.Data;

public sealed class EfDriftReportStore(PharmaAccessDbContext db) : IDriftReportStore
{
    public async Task SaveAsync(DriftReport report, CancellationToken cancellationToken = default)
    {
        if (await db.GovernanceDriftReports.AnyAsync(x=>x.DriftReportId==report.Id,cancellationToken)) throw new InvalidOperationException("Drift report already exists.");
        db.GovernanceDriftReports.Add(new GovernanceDriftReport(report)); foreach(var finding in report.Measures) db.GovernanceDriftFindings.Add(new GovernanceDriftFinding(report.Id,finding)); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<DriftReport>> ListAsync(CancellationToken cancellationToken = default) => (await db.GovernanceDriftReports.AsNoTracking().Include(x=>x.Findings).OrderByDescending(x=>x.CreatedAtUtc).ToArrayAsync(cancellationToken)).Select(Map).ToArray();
    public async Task<DriftReport?> GetAsync(Guid id, CancellationToken cancellationToken = default) { var value=await db.GovernanceDriftReports.AsNoTracking().Include(x=>x.Findings).SingleOrDefaultAsync(x=>x.DriftReportId==id,cancellationToken); return value is null?null:Map(value); }
    private static DriftReport Map(GovernanceDriftReport x) => new(x.DriftReportId,x.ChampionVersion,x.EvaluationWindow,x.CreatedAtUtc,x.Severity,x.Findings.OrderBy(f=>f.Name).Select(f=>new DriftMeasure(f.Scope,f.Name,f.Statistic,f.ReferenceValue,f.CurrentValue,f.Change,f.Severity,f.Formula)).ToArray(),JsonSerializer.Deserialize<string[]>(x.SubgroupWarningsJson)??[],x.LabelsAvailable,x.GovernanceNotice);
}

public sealed class EfModelGovernanceRepository(PharmaAccessDbContext db) : IModelGovernanceRepository
{
    public async Task SaveComparisonAsync(ChampionChallengerComparison comparison, CancellationToken cancellationToken = default)
    {
        if(await db.GovernanceComparisons.AnyAsync(x=>x.ComparisonId==comparison.Id,cancellationToken)) throw new InvalidOperationException("Comparison already exists.");
        db.GovernanceComparisons.Add(new GovernanceComparison(comparison)); if(!await db.GovernanceChampionStates.AnyAsync(cancellationToken)) db.GovernanceChampionStates.Add(new GovernanceChampionState(comparison.Champion.Version)); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<ChampionChallengerComparison?> GetComparisonAsync(Guid id,CancellationToken cancellationToken=default) { var x=await db.GovernanceComparisons.AsNoTracking().SingleOrDefaultAsync(v=>v.ComparisonId==id,cancellationToken); return x is null?null:Map(x); }
    public async Task<ModelGovernanceState> GetStateAsync(CancellationToken cancellationToken=default)
    {
        var state=await db.GovernanceChampionStates.AsNoTracking().SingleOrDefaultAsync(cancellationToken); var pending=await db.GovernanceComparisons.AsNoTracking().Where(x=>x.Status=="Pending").OrderByDescending(x=>x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken); var audits=await db.GovernanceDecisions.AsNoTracking().OrderBy(x=>x.RecordedAtUtc).Select(x=>new PromotionAuditRecord(x.GovernanceDecisionId,x.ComparisonId??Guid.Empty,x.Decision,x.ChampionBefore,x.ChampionAfter,x.ChallengerVersion,x.ApproverIdentifier,x.ActionTimestampUtc,x.Reason,x.RecordedAtUtc)).ToArrayAsync(cancellationToken);
        return new(state?.ChampionVersion??"unconfigured",pending?.ChallengerVersion,pending is null?"No pending comparison":pending.PromotionEligible?"Awaiting human approval":"Blocked",state?.PreviousChampionVersion is not null,pending is null?[]:JsonSerializer.Deserialize<MetricDifference[]>(pending.MetricDifferencesJson)??[],pending is null?[]:JsonSerializer.Deserialize<string[]>(pending.SubgroupResultsJson)??[],audits);
    }
    public async Task<PromotionAuditRecord> ExecuteDecisionAsync(PromotionDecision decision,PromotionActionRequest request,CancellationToken cancellationToken=default)
    {
        Validate(request.ApproverIdentifier,request.ApprovalTimestampUtc,request.Reason); await using var transaction=await db.Database.BeginTransactionAsync(cancellationToken); var comparison=await db.GovernanceComparisons.SingleOrDefaultAsync(x=>x.ComparisonId==request.ComparisonId,cancellationToken)??throw new KeyNotFoundException("Comparison was not persisted."); if(comparison.CompletedAtUtc.HasValue||await db.GovernanceDecisions.AnyAsync(x=>x.ComparisonId==request.ComparisonId,cancellationToken)) throw new InvalidOperationException("Comparison decision was already executed."); var state=await db.GovernanceChampionStates.SingleAsync(cancellationToken); if(state.ChampionVersion!=comparison.ChampionVersion) throw new InvalidOperationException("Comparison is outdated because the champion changed."); if(decision==PromotionDecision.Approved&&!comparison.PromotionEligible) throw new InvalidOperationException("Blocked comparison cannot be promoted."); var before=state.ChampionVersion; var after=decision==PromotionDecision.Approved?comparison.ChallengerVersion:before; if(decision==PromotionDecision.Approved) state.Promote(after,request.ApprovalTimestampUtc); comparison.Complete(decision.ToString(),request.ApprovalTimestampUtc); var entity=new GovernanceDecision(comparison.ComparisonId,decision,before,after,comparison.ChallengerVersion,request.ApproverIdentifier.Trim(),request.ApprovalTimestampUtc.ToUniversalTime(),request.Reason.Trim()); db.GovernanceDecisions.Add(entity); if(decision==PromotionDecision.Approved) AddHistory(entity,before,after,request.ApprovalTimestampUtc); db.GovernanceAuditRecords.Add(new GovernanceAuditRecord(entity.GovernanceDecisionId,decision.ToString(),DateTime.UtcNow,JsonSerializer.Serialize(new{comparison.ComparisonId,before,after})));
        try { await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); } catch(DbUpdateConcurrencyException error){ throw new InvalidOperationException("Champion changed concurrently; retry with a fresh comparison.",error); } catch(DbUpdateException error){ throw new InvalidOperationException("Duplicate or conflicting governance decision was rejected.",error); } return Map(entity);
    }
    public async Task<PromotionAuditRecord> ExecuteRollbackAsync(string approverIdentifier,DateTime timestampUtc,string reason,CancellationToken cancellationToken=default)
    {
        Validate(approverIdentifier,timestampUtc,reason); await using var transaction=await db.Database.BeginTransactionAsync(cancellationToken); var state=await db.GovernanceChampionStates.SingleAsync(cancellationToken); var target=state.PreviousChampionVersion??throw new InvalidOperationException("No previously approved champion is available."); if(!await db.GovernanceChampionHistory.AnyAsync(x=>x.ModelVersion==target,cancellationToken)) throw new InvalidOperationException("Rollback target was never approved."); var before=state.ChampionVersion; state.Rollback(timestampUtc); var entity=new GovernanceDecision(Guid.Empty,PromotionDecision.RolledBack,before,state.ChampionVersion,before,approverIdentifier.Trim(),timestampUtc.ToUniversalTime(),reason.Trim()); db.GovernanceDecisions.Add(entity); AddHistory(entity,before,state.ChampionVersion,timestampUtc); db.GovernanceAuditRecords.Add(new GovernanceAuditRecord(entity.GovernanceDecisionId,"RolledBack",DateTime.UtcNow,JsonSerializer.Serialize(new{before,target})));
        try{await db.SaveChangesAsync(cancellationToken);await transaction.CommitAsync(cancellationToken);}catch(DbUpdateConcurrencyException error){throw new InvalidOperationException("Champion changed concurrently; rollback was rejected.",error);} return Map(entity);
    }
    private void AddHistory(GovernanceDecision decision,string before,string after,DateTime at){var history=db.GovernanceChampionHistory.ToArray();foreach(var current in history.Where(x=>x.IsCurrent))current.Supersede(at);if(!history.Any(x=>x.ModelVersion==before))db.GovernanceChampionHistory.Add(new GovernanceChampionHistory(before,null,decision.GovernanceDecisionId,false,at));db.GovernanceChampionHistory.Add(new GovernanceChampionHistory(after,before,decision.GovernanceDecisionId,true,at));}
    private static ChampionChallengerComparison Map(GovernanceComparison x)=>new(x.ComparisonId,JsonSerializer.Deserialize<GovernedModelSnapshot>(x.ChampionJson)!,JsonSerializer.Deserialize<GovernedModelSnapshot>(x.ChallengerJson)!,x.CreatedAtUtc,JsonSerializer.Deserialize<MetricDifference[]>(x.MetricDifferencesJson)??[],JsonSerializer.Deserialize<string[]>(x.SubgroupResultsJson)??[],JsonSerializer.Deserialize<string[]>(x.BlockingReasonsJson)??[],x.PromotionEligible,"Persisted human-governed comparison.");
    private static PromotionAuditRecord Map(GovernanceDecision x)=>new(x.GovernanceDecisionId,x.ComparisonId??Guid.Empty,x.Decision,x.ChampionBefore,x.ChampionAfter,x.ChallengerVersion,x.ApproverIdentifier,x.ActionTimestampUtc,x.Reason,x.RecordedAtUtc);
    private static void Validate(string approver,DateTime timestamp,string reason){if(string.IsNullOrWhiteSpace(approver)||string.IsNullOrWhiteSpace(reason)||timestamp==default||timestamp>DateTime.UtcNow.AddMinutes(5))throw new ArgumentException("Approver identifier, valid timestamp, and reason are required.");}
}

public sealed class PersistentHumanGovernedModelManager(IModelGovernanceRepository repository) : IHumanGovernedModelManager
{
    private ModelGovernanceState _state=new("unconfigured",null,"Loading",false,[],[],[]); public ModelGovernanceState State=>_state;
    public async Task<ModelGovernanceState> GetStateAsync(CancellationToken cancellationToken=default)=>_state=await repository.GetStateAsync(cancellationToken);
    public async Task RegisterComparisonAsync(ChampionChallengerComparison comparison,CancellationToken cancellationToken=default){await repository.SaveComparisonAsync(comparison,cancellationToken);await GetStateAsync(cancellationToken);}
    public async Task<PromotionAuditRecord> ApproveAsync(PromotionActionRequest request,CancellationToken cancellationToken=default){var value=await repository.ExecuteDecisionAsync(PromotionDecision.Approved,request,cancellationToken);await GetStateAsync(cancellationToken);return value;}
    public async Task<PromotionAuditRecord> RejectAsync(PromotionActionRequest request,CancellationToken cancellationToken=default){var value=await repository.ExecuteDecisionAsync(PromotionDecision.Rejected,request,cancellationToken);await GetStateAsync(cancellationToken);return value;}
    public async Task<PromotionAuditRecord> RollbackAsync(string approverIdentifier,DateTime timestampUtc,string reason,CancellationToken cancellationToken=default){var value=await repository.ExecuteRollbackAsync(approverIdentifier,timestampUtc,reason,cancellationToken);await GetStateAsync(cancellationToken);return value;}
}

public sealed class ApprovedRootArtifactIntegrityVerifier : IArtifactIntegrityVerifier
{
    private readonly string[] _roots;
    public ApprovedRootArtifactIntegrityVerifier(IEnumerable<string> approvedRoots){_roots=approvedRoots.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();if(_roots.Length==0)throw new ArgumentException("At least one approved artifact root is required.");}
    public ArtifactVerificationResult Verify(ArtifactVerificationRequest request)
    {
        if(request.ArtifactPath.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Any(x=>x==".."))return new(false,"PathTraversalRejected"); if(!request.ExpectedSha256.All(Uri.IsHexDigit)||request.ExpectedSha256.Length!=64)return new(false,"InvalidExpectedHash");
        string full;try{full=Path.GetFullPath(request.ArtifactPath);}catch{return new(false,"InvalidPath");} var root=_roots.FirstOrDefault(x=>full.StartsWith(x+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));if(root is null)return new(false,"OutsideApprovedRoot"); if(IsForbidden(full,root))return new(false,"ForbiddenLocation");
        try{using var stream=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.SequentialScan);if(stream.Length==0)return new(false,"EmptyArtifact");var hash=Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();return new(hash.Equals(request.ExpectedSha256,StringComparison.OrdinalIgnoreCase),hash.Equals(request.ExpectedSha256,StringComparison.OrdinalIgnoreCase)?"Valid":"HashMismatch",hash);}catch(FileNotFoundException){return new(false,"MissingArtifact");}catch(DirectoryNotFoundException){return new(false,"MissingArtifact");}catch(UnauthorizedAccessException){return new(false,"InaccessibleArtifact");}catch(IOException){return new(false,"InaccessibleArtifact");}
    }
    private static bool IsForbidden(string full,string root){var relative=Path.GetRelativePath(root,full).Replace('\\','/');return relative.StartsWith("data/private/",StringComparison.OrdinalIgnoreCase)||relative.StartsWith("docs/",StringComparison.OrdinalIgnoreCase)||relative.StartsWith("temp/",StringComparison.OrdinalIgnoreCase)||relative.Contains("prompt",StringComparison.OrdinalIgnoreCase);}
}
