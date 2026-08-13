using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Data;
using Xunit;

namespace PharmaAccess.Data.Tests;

public sealed class CandidateRegistrationTests
{
    private static string Bundle => Path.Combine(FindRoot(), "artifacts", "models", "NextQuarterStateEntry-real-next-quarter-entry-serving-v1");

    [Fact]
    public async Task Verified_candidate_registration_is_transactional_idempotent_and_never_approves_or_promotes()
    {
        var root=Path.Combine(Environment.GetEnvironmentVariable("TEMP")??Path.GetTempPath(),$"r2d-{Guid.NewGuid():N}"); Directory.CreateDirectory(root);
        var database=$"R2DRegistrationTest_{Guid.NewGuid():N}"; var file=Path.Combine(root,$"{database}.mdf");
        var connection=$"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=True;TrustServerCertificate=True;AttachDbFilename={file}";
        await using var db=new PharmaAccessDbContext(new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.MigrateAsync(); var service=new EfServingCandidateRegistration(db);
            var first=await service.RegisterAsync(Bundle,"test-authenticated-identity"); var second=await service.RegisterAsync(Bundle,"test-authenticated-identity");
            Assert.False(first.Idempotent); Assert.True(second.Idempotent); Assert.Equal(first.ArtifactId,second.ArtifactId); Assert.Equal(first.RegistryEntryId,second.RegistryEntryId);
            var artifact=await db.ModelArtifacts.SingleAsync(); var registry=await db.ModelRegistryEntries.SingleAsync();
            Assert.Equal("62C607C6462D603D04CDF43662B16EAD773138C916912080B15A7F65C85F9022",artifact.Sha256); Assert.Equal("BE2EBF510D5E610DD4F54D4E62BB6F9BDB47EACFB496B6739659C7E6AA6C595E",artifact.InputSchemaHash); Assert.Equal("8E849D11A4D25976F808E49436A6576E74311C4CC97016E7C7126C43210AC750",artifact.FeatureSchemaHash);
            Assert.False(artifact.IsApproved); Assert.Equal(ModelApprovalStatus.ValidationSelected,artifact.ApprovalStatus); Assert.False(registry.IsChampion); Assert.False(registry.IsSynthetic); Assert.Equal(ModelApprovalStatus.ValidationSelected,registry.Status); Assert.Contains("0.08",artifact.ApprovalNotes); Assert.Contains("test-authenticated-identity",artifact.ApprovalNotes);
            Assert.Empty(await db.ModelApprovals.ToArrayAsync()); Assert.Empty(await db.GovernanceChampionStates.ToArrayAsync()); Assert.Empty(await db.GovernanceChampionHistory.ToArrayAsync()); Assert.Equal(2,(await db.DatasetVersions.SingleAsync()).DatasetVersionId); Assert.Equal(1,(await db.FeatureSetVersions.SingleAsync()).FeatureSetVersionId);
            var approval=new ModelApprovalService(new EfModelRegistryRepository(db));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>approval.ExecuteAsync(new(first.ArtifactId,ApprovalDecision.Approve,"different-reviewer",new string('x',2100),"Production",false,"card-v1")));db.ChangeTracker.Clear();
            Assert.False((await db.ModelArtifacts.SingleAsync()).IsApproved);Assert.Empty(await db.ModelApprovals.ToArrayAsync());
            var approved=await approval.ExecuteAsync(new(first.ArtifactId,ApprovalDecision.Approve,"different-reviewer","reviewed verified evidence","Production",false,"card-v1"));Assert.Equal(ModelApprovalStatus.Approved,approved.Status);db.ChangeTracker.Clear();
            Assert.True((await db.ModelArtifacts.SingleAsync()).IsApproved);Assert.Equal(ModelApprovalStatus.Approved,(await db.ModelRegistryEntries.SingleAsync()).Status);Assert.False((await db.ModelRegistryEntries.SingleAsync()).IsChampion);Assert.Single(await db.ModelApprovals.ToArrayAsync());Assert.Empty(await db.GovernanceChampionStates.ToArrayAsync());Assert.Empty(await db.GovernanceChampionHistory.ToArrayAsync());
            await Assert.ThrowsAsync<InvalidOperationException>(()=>approval.ExecuteAsync(new(first.ArtifactId,ApprovalDecision.Approve,"different-reviewer","duplicate","Production",false,"card-v1")));Assert.Single(await db.ModelApprovals.ToArrayAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); Directory.Delete(root,true); }
    }

    [Fact]
    public void Synthetic_or_tampered_bundle_fails_closed()
    {
        var root=Path.Combine(Environment.GetEnvironmentVariable("TEMP")??Path.GetTempPath(),$"r2d-bundle-{Guid.NewGuid():N}"); Copy(Bundle,root);
        try
        {
            var manifest=Path.Combine(root,"manifest.json"); var text=File.ReadAllText(manifest).Replace("\"syntheticDevelopmentData\": false","\"syntheticDevelopmentData\": true"); File.WriteAllText(manifest,text);
            Assert.Throws<InvalidDataException>(()=>EfServingCandidateRegistration.ValidateBundle(root));
            File.Copy(Path.Combine(Bundle,"manifest.json"),manifest,true); File.AppendAllText(Path.Combine(root,"model.zip"),"tamper");
            Assert.Throws<InvalidDataException>(()=>EfServingCandidateRegistration.ValidateBundle(root));
        }
        finally { Directory.Delete(root,true); }
    }

    private static void Copy(string source,string target){Directory.CreateDirectory(target);foreach(var file in Directory.GetFiles(source))File.Copy(file,Path.Combine(target,Path.GetFileName(file)));}
    private static string FindRoot(){var value=AppContext.BaseDirectory;while(value is not null&&!File.Exists(Path.Combine(value,"PharmaAccess.sln")))value=Directory.GetParent(value)?.FullName;return value??throw new DirectoryNotFoundException("Repository root not found.");}
}
