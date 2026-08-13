using PharmaAccess.Application.MachineLearning;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class MilestoneFiveApprovalTests
{
    [Fact]
    public async Task Approval_requires_integrity_schema_and_audit_metadata()
    {
        var repo = new FakeRepository(Model(1)); var service = new ModelApprovalService(repo); var approved = await service.ExecuteAsync(new(1, ApprovalDecision.Approve, "reviewer", "validated synthetic workflow", "Development", false, "card-v1")); Assert.Equal(ModelApprovalStatus.Approved, approved.Status); Assert.Equal("reviewer", approved.ApprovedBy); Assert.NotNull(approved.ApprovedAtUtc);
        repo.Value = Model(1) with { ArtifactIntegrityValid = false }; await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(new(1, ApprovalDecision.Approve, "r", "reason", "Development", false, "card")));
        repo.Value = Model(1) with { FeatureSchemaHash = "bad" }; await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(new(1, ApprovalDecision.Approve, "r", "reason", "Development", false, "card")));
    }

    [Fact]
    public async Task Approval_and_champion_assignment_are_separate_explicit_actions()
    {
        var repo = new FakeRepository(Model(2) with { IsSyntheticDevelopmentOnly = false }) { Champion = Model(1) with { Status = ModelApprovalStatus.Champion } };
        var approval = new ModelApprovalService(repo); var assignment = new ModelChampionAssignmentService(repo);
        await Assert.ThrowsAsync<InvalidOperationException>(() => approval.ExecuteAsync(new(2, ApprovalDecision.Approve, "approver", "verified", "Production", true, "card-v1")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => assignment.ExecuteAsync(new(2, "operator", "assign", "Production", "card-v1")));
        var approved = await approval.ExecuteAsync(new(2, ApprovalDecision.Approve, "approver", "verified", "Production", false, "card-v1")); Assert.Equal(ModelApprovalStatus.Approved, approved.Status); Assert.Null(repo.PreviousChampion);
        var champion = await assignment.ExecuteAsync(new(2, "operator", "explicit assignment", "Production", "card-v1")); Assert.Equal(ModelApprovalStatus.Champion, champion.Status); Assert.Equal(ModelApprovalStatus.Archived, repo.PreviousChampion!.Status);
        repo.Value = Model(3); await approval.ExecuteAsync(new(3, ApprovalDecision.Approve, "approver", "development only", "Development", false, "card")); await Assert.ThrowsAsync<InvalidOperationException>(() => assignment.ExecuteAsync(new(3, "operator", "assign", "Production", "card")));
    }

    [Theory][InlineData("submitter")][InlineData("SUBMITTER")]
    public async Task Submitter_cannot_approve_and_provenance_must_be_complete_and_consistent(string actor)
    {
        var repo=new FakeRepository(Model(1));var service=new ModelApprovalService(repo);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ExecuteAsync(new(1,ApprovalDecision.Approve,actor,"reason","Production",false,"card")));
        repo.Value=Model(1) with{ArtifactSubmitterIdentifier=null};await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ExecuteAsync(new(1,ApprovalDecision.Approve,"reviewer","reason","Production",false,"card")));
        repo.Value=Model(1) with{ExperimentSubmitterIdentifier="other"};await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ExecuteAsync(new(1,ApprovalDecision.Approve,"reviewer","reason","Production",false,"card")));
    }

    private static ModelRegistryRecord Model(long id) => new(id, "NextQuarterStateEntry", "Development", ModelApprovalStatus.ValidationSelected, false, true, "schema", "schema", null, null, null, null,"submitter","submitter");
    private sealed class FakeRepository(ModelRegistryRecord value) : IModelRegistryRepository
    {
        public ModelRegistryRecord Value { get; set; } = value; public ModelRegistryRecord? Champion { get; set; } public ModelRegistryRecord? PreviousChampion { get; private set; }
        public Task<ModelRegistryRecord?> GetAsync(long artifactId, CancellationToken cancellationToken) => Task.FromResult<ModelRegistryRecord?>(Value.ModelArtifactId == artifactId ? Value : null);
        public Task<ModelRegistryRecord?> GetChampionAsync(string task, string environment, CancellationToken cancellationToken) => Task.FromResult(Champion);
        public Task SaveApprovalAsync(ModelRegistryRecord updated, ModelRegistryRecord? previousChampion, CancellationToken cancellationToken) { Value = updated; PreviousChampion = previousChampion; Champion = updated.Status == ModelApprovalStatus.Champion ? updated : Champion; return Task.CompletedTask; }
    }
}
