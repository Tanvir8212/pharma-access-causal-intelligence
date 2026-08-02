using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Data.Entities;
using Xunit;

namespace PharmaAccess.Data.Tests;

public sealed class GovernancePersistenceTests
{
    [Theory]
    [InlineData(typeof(GovernanceDriftReport),"DriftReport","ml")][InlineData(typeof(GovernanceDriftFinding),"DriftFinding","ml")]
    [InlineData(typeof(GovernanceComparison),"ChampionChallengerComparison","ml")][InlineData(typeof(GovernanceChampionState),"ChampionState","ml")]
    [InlineData(typeof(GovernanceDecision),"GovernanceDecision","audit")][InlineData(typeof(GovernanceChampionHistory),"ChampionHistory","audit")]
    [InlineData(typeof(GovernanceAuditRecord),"GovernanceAuditRecord","audit")]
    public void Governance_entities_have_expected_table_and_schema(Type type,string table,string schema){var entity=Model.FindEntityType(type)!;Assert.Equal(table,entity.GetTableName());Assert.Equal(schema,entity.GetSchema());Assert.NotNull(entity.FindPrimaryKey());}

    [Fact]
    public void Governance_relationships_are_restrictive_and_constraints_are_present()
    {
        foreach(var type in new[]{typeof(GovernanceDriftFinding),typeof(GovernanceDecision),typeof(GovernanceChampionHistory),typeof(GovernanceAuditRecord)})Assert.All(Model.FindEntityType(type)!.GetForeignKeys(),x=>Assert.Equal(DeleteBehavior.Restrict,x.DeleteBehavior));
        Assert.True(Model.FindEntityType(typeof(GovernanceComparison))!.FindProperty(nameof(GovernanceComparison.RowVersion))!.IsConcurrencyToken);
        Assert.True(Model.FindEntityType(typeof(GovernanceChampionState))!.FindProperty(nameof(GovernanceChampionState.RowVersion))!.IsConcurrencyToken);
        Assert.True(Model.FindEntityType(typeof(GovernanceDecision))!.FindProperty(nameof(GovernanceDecision.RowVersion))!.IsConcurrencyToken);
        Assert.Contains(Model.FindEntityType(typeof(GovernanceDecision))!.GetIndexes(),x=>x.IsUnique&&x.GetFilter()=="[ComparisonId] IS NOT NULL");
        Assert.Contains(Model.FindEntityType(typeof(GovernanceChampionHistory))!.GetIndexes(),x=>x.IsUnique&&x.GetFilter()=="[IsCurrent] = 1");
    }

    [Fact]
    public void Completed_audit_records_cannot_be_modified()
    {
        var options=new DbContextOptionsBuilder<Data.PharmaAccessDbContext>().UseSqlServer("Server=localhost;Database=MetadataOnly;Trusted_Connection=True;TrustServerCertificate=True").Options;using var db=new Data.PharmaAccessDbContext(options);var audit=(GovernanceAuditRecord)Activator.CreateInstance(typeof(GovernanceAuditRecord),true)!;typeof(GovernanceAuditRecord).GetProperty(nameof(GovernanceAuditRecord.IsCompleted))!.SetValue(audit,true);db.Attach(audit);db.Entry(audit).State=EntityState.Modified;Assert.Throws<InvalidOperationException>(()=>db.SaveChanges());
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IModel Model=>new Data.PharmaAccessDbContext(new DbContextOptionsBuilder<Data.PharmaAccessDbContext>().UseSqlServer("Server=localhost;Database=MetadataOnly;Trusted_Connection=True;TrustServerCertificate=True").Options).Model;
}

public sealed class ArtifactIntegrityTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"pharma-artifact-tests-"+Guid.NewGuid().ToString("N"));
    public ArtifactIntegrityTests()=>Directory.CreateDirectory(root);
    [Fact]public void Valid_artifact_hash_is_accepted(){var path=Write("model.zip","governed model");var hash=Hash(path);var result=Verifier().Verify(new(path,hash));Assert.True(result.IsValid);Assert.Equal("Valid",result.Status);}
    [Fact]public void Invalid_artifact_hash_is_rejected(){var path=Write("model.zip","governed model");Assert.Equal("HashMismatch",Verifier().Verify(new(path,new string('a',64))).Status);}
    [Fact]public void Missing_artifact_is_rejected(){Assert.Equal("MissingArtifact",Verifier().Verify(new(Path.Combine(root,"missing.zip"),new string('a',64))).Status);}
    [Fact]public void Empty_artifact_is_rejected(){var path=Write("empty.zip","");Assert.Equal("EmptyArtifact",Verifier().Verify(new(path,new string('a',64))).Status);}
    [Fact]public void Path_traversal_is_rejected(){Assert.Equal("PathTraversalRejected",Verifier().Verify(new(Path.Combine(root,"folder","..","model.zip"),new string('a',64))).Status);}
    [Fact]public void Artifact_outside_approved_root_is_rejected(){var outside=Path.Combine(Path.GetTempPath(),"outside-"+Guid.NewGuid().ToString("N")+".zip");File.WriteAllText(outside,"x");try{Assert.Equal("OutsideApprovedRoot",Verifier().Verify(new(outside,Hash(outside))).Status);}finally{File.Delete(outside);}}
    [Fact]public void Forbidden_subdirectory_is_rejected(){var path=Write(Path.Combine("docs","model.zip"),"x");Assert.Equal("ForbiddenLocation",Verifier().Verify(new(path,Hash(path))).Status);}
    private Data.ApprovedRootArtifactIntegrityVerifier Verifier()=>new([root]);private string Write(string relative,string text){var path=Path.Combine(root,relative);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,text);return path;}private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}

public sealed class PersistentManagerContractTests
{
    [Fact]public async Task Comparisons_approvals_rejections_rollbacks_and_champion_history_persist_in_test_repository(){var repository=new DeterministicRepository();var manager=new Data.PersistentHumanGovernedModelManager(repository);var comparison=Comparison(Guid.NewGuid(),"champion","challenger");await manager.RegisterComparisonAsync(comparison);Assert.NotNull(await repository.GetComparisonAsync(comparison.Id));var approval=await manager.ApproveAsync(Action(comparison.Id));Assert.Equal(PromotionDecision.Approved,approval.Decision);Assert.Equal("challenger",(await manager.GetStateAsync()).ChampionVersion);var second=Comparison(Guid.NewGuid(),"challenger","candidate-2");await manager.RegisterComparisonAsync(second);Assert.Equal(PromotionDecision.Rejected,(await manager.RejectAsync(Action(second.Id))).Decision);Assert.Equal(PromotionDecision.RolledBack,(await manager.RollbackAsync("reviewer",DateTime.UtcNow,"rollback")).Decision);Assert.Equal("champion",(await manager.GetStateAsync()).ChampionVersion);Assert.Equal(3,repository.Audits.Count);Assert.Equal(["challenger","champion"],repository.History);}
    [Fact]public async Task Duplicate_approval_is_rejected(){var r=new DeterministicRepository();var m=new Data.PersistentHumanGovernedModelManager(r);var c=Comparison(Guid.NewGuid(),"champion","challenger");await m.RegisterComparisonAsync(c);await m.ApproveAsync(Action(c.Id));await Assert.ThrowsAsync<InvalidOperationException>(()=>m.ApproveAsync(Action(c.Id)));}
    [Fact]public async Task Outdated_comparison_is_rejected(){var r=new DeterministicRepository();var m=new Data.PersistentHumanGovernedModelManager(r);var c=Comparison(Guid.NewGuid(),"old","challenger");await m.RegisterComparisonAsync(c);await Assert.ThrowsAsync<InvalidOperationException>(()=>m.ApproveAsync(Action(c.Id)));}
    [Fact]public async Task Concurrency_conflict_is_propagated(){var r=new DeterministicRepository{Conflict=true};var m=new Data.PersistentHumanGovernedModelManager(r);var c=Comparison(Guid.NewGuid(),"champion","challenger");await m.RegisterComparisonAsync(c);await Assert.ThrowsAsync<InvalidOperationException>(()=>m.ApproveAsync(Action(c.Id)));}
    private static PromotionActionRequest Action(Guid id)=>new(id,"reviewer",DateTime.UtcNow,"human decision");private static ChampionChallengerComparison Comparison(Guid id,string champion,string challenger){Func<string,GovernedModelSnapshot> snapshot=v=>new GovernedModelSnapshot(v,new string('a',64),true,"schema","cohort","dataset","repro",true,new Dictionary<string,double>(),new Dictionary<string,double>());return new(id,snapshot(champion),snapshot(challenger),DateTime.UtcNow,[],[],[],true,"human governed");}
    private sealed class DeterministicRepository:IModelGovernanceRepository
    {private readonly Dictionary<Guid,ChampionChallengerComparison> comparisons=[];private readonly HashSet<Guid> completed=[];private string champion="champion";private string? previous;public bool Conflict;public List<PromotionAuditRecord> Audits=[];public List<string> History=[];public Task SaveComparisonAsync(ChampionChallengerComparison c,CancellationToken t=default){comparisons.Add(c.Id,c);return Task.CompletedTask;}public Task<ChampionChallengerComparison?> GetComparisonAsync(Guid id,CancellationToken t=default)=>Task.FromResult(comparisons.GetValueOrDefault(id));public Task<ModelGovernanceState> GetStateAsync(CancellationToken t=default)=>Task.FromResult(new ModelGovernanceState(champion,null,"Persisted",previous!=null,[],[],Audits.ToArray()));public Task<PromotionAuditRecord> ExecuteDecisionAsync(PromotionDecision d,PromotionActionRequest r,CancellationToken t=default){if(Conflict)throw new InvalidOperationException("concurrency conflict");if(!completed.Add(r.ComparisonId))throw new InvalidOperationException("duplicate");var c=comparisons[r.ComparisonId];if(c.Champion.Version!=champion)throw new InvalidOperationException("outdated");var before=champion;if(d==PromotionDecision.Approved){previous=champion;champion=c.Challenger.Version;History.Add(champion);}var a=new PromotionAuditRecord(Guid.NewGuid(),c.Id,d,before,champion,c.Challenger.Version,r.ApproverIdentifier,r.ApprovalTimestampUtc,r.Reason,DateTime.UtcNow);Audits.Add(a);return Task.FromResult(a);}public Task<PromotionAuditRecord> ExecuteRollbackAsync(string a,DateTime at,string reason,CancellationToken t=default){if(previous is null)throw new InvalidOperationException("unapproved");var before=champion;champion=previous;previous=before;History.Add(champion);var x=new PromotionAuditRecord(Guid.NewGuid(),Guid.Empty,PromotionDecision.RolledBack,before,champion,before,a,at,reason,DateTime.UtcNow);Audits.Add(x);return Task.FromResult(x);}}
}
