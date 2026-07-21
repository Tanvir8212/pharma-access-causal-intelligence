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
    public async Task Champion_requires_approval_and_replaces_previous_transactionally()
    {
        var repo = new FakeRepository(Model(2)) { Champion = Model(1) with { Status = ModelApprovalStatus.Champion } }; var service = new ModelApprovalService(repo); await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(new(2, ApprovalDecision.Reject, "r", "reason", "Development", true, "card")));
        var champion = await service.ExecuteAsync(new(2, ApprovalDecision.Approve, "r", "reason", "Development", true, "card")); Assert.Equal(ModelApprovalStatus.Champion, champion.Status); Assert.Equal(ModelApprovalStatus.Archived, repo.PreviousChampion!.Status);
        repo.Value = Model(3); await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(new(3, ApprovalDecision.Approve, "r", "reason", "Production", true, "card")));
    }

    private static ModelRegistryRecord Model(long id) => new(id, "NextQuarterStateEntry", "Development", ModelApprovalStatus.ValidationSelected, true, true, "schema", "schema", null, null, null, null);
    private sealed class FakeRepository(ModelRegistryRecord value) : IModelRegistryRepository
    {
        public ModelRegistryRecord Value { get; set; } = value; public ModelRegistryRecord? Champion { get; set; } public ModelRegistryRecord? PreviousChampion { get; private set; }
        public Task<ModelRegistryRecord?> GetAsync(long artifactId, CancellationToken cancellationToken) => Task.FromResult<ModelRegistryRecord?>(Value.ModelArtifactId == artifactId ? Value : null);
        public Task<ModelRegistryRecord?> GetChampionAsync(string task, string environment, CancellationToken cancellationToken) => Task.FromResult(Champion);
        public Task SaveApprovalAsync(ModelRegistryRecord updated, ModelRegistryRecord? previousChampion, CancellationToken cancellationToken) { Value = updated; PreviousChampion = previousChampion; Champion = updated.Status == ModelApprovalStatus.Champion ? updated : Champion; return Task.CompletedTask; }
    }
}
