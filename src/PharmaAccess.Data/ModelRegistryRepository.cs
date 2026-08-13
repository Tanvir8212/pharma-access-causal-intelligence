using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Data.Entities;

namespace PharmaAccess.Data;

public sealed class EfModelRegistryRepository(PharmaAccessDbContext db) : IModelRegistryRepository
{
    public async Task<ModelRegistryRecord?> GetAsync(long artifactId,CancellationToken cancellationToken)
    {
        var row=await LoadAsync(artifactId,false,cancellationToken);
        return row is null?null:Map(row);
    }

    public async Task<ModelRegistryRecord?> GetChampionAsync(string task,string environment,CancellationToken cancellationToken)
    {
        var artifactId=await db.ModelRegistryEntries.AsNoTracking().Where(x=>x.TaskName==task&&x.Environment==environment&&x.IsChampion).Select(x=>(long?)x.ModelArtifactId).SingleOrDefaultAsync(cancellationToken);
        return artifactId is null?null:Map((await LoadAsync(artifactId.Value,false,cancellationToken))!);
    }

    public async Task SaveApprovalAsync(ModelRegistryRecord updated,ModelRegistryRecord? previousChampion,CancellationToken cancellationToken)
    {
        if(previousChampion is not null||updated.Status!=ModelApprovalStatus.Approved||string.IsNullOrWhiteSpace(updated.ApprovedBy)||updated.ApprovedAtUtc is null||string.IsNullOrWhiteSpace(updated.ApprovalReason)||string.IsNullOrWhiteSpace(updated.ModelCardVersion)||string.IsNullOrWhiteSpace(updated.Environment))throw new InvalidOperationException("Only a complete non-champion artifact approval can be persisted.");
        await using var transaction=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,cancellationToken);
        var row=await LoadAsync(updated.ModelArtifactId,true,cancellationToken)??throw new KeyNotFoundException("Model artifact is not registered.");
        var current=Map(row); ValidateEligible(current,updated.ApprovedBy);
        if(await db.ModelApprovals.AnyAsync(x=>x.ModelArtifactId==updated.ModelArtifactId,cancellationToken))throw new InvalidOperationException("An approval decision already exists for this artifact.");
        var now=updated.ApprovedAtUtc.Value.ToUniversalTime(); row.Artifact.Approve(); row.Registry.Approve(updated.Environment.Trim(),now);
        db.ModelApprovals.Add(new ModelApprovalEntity(updated.ModelArtifactId,updated.ApprovedBy.Trim(),updated.ApprovalReason.Trim(),updated.Environment.Trim(),updated.ModelCardVersion.Trim(),now));
        try{await db.SaveChangesAsync(cancellationToken);await transaction.CommitAsync(cancellationToken);}catch(DbUpdateException error){throw new InvalidOperationException("Artifact approval conflicted with another decision; no approval was persisted.",error);}
    }

    private async Task<Row?> LoadAsync(long artifactId,bool tracking,CancellationToken token)
    {
        var artifacts=tracking?db.ModelArtifacts:db.ModelArtifacts.AsNoTracking();var artifact=await artifacts.SingleOrDefaultAsync(x=>x.ModelArtifactId==artifactId,token);if(artifact is null)return null;
        var registries=tracking?db.ModelRegistryEntries:db.ModelRegistryEntries.AsNoTracking();var registry=await registries.SingleOrDefaultAsync(x=>x.ModelArtifactId==artifactId,token)??throw new InvalidOperationException("The artifact registry entry is missing.");
        var runs=tracking?db.ModelTrainingRuns:db.ModelTrainingRuns.AsNoTracking();var run=await runs.SingleAsync(x=>x.ModelTrainingRunId==artifact.ModelTrainingRunId,token);
        var experiments=tracking?db.MlExperiments:db.MlExperiments.AsNoTracking();var experiment=await experiments.SingleAsync(x=>x.ExperimentId==run.ExperimentId,token);return new(artifact,registry,experiment);
    }
    private static ModelRegistryRecord Map(Row row)
    {
        var artifactSubmitter=Submitter(row.Artifact.ApprovalNotes);var experimentSubmitter=Submitter(row.Experiment.ConfigurationJson);
        return new(row.Artifact.ModelArtifactId,row.Registry.TaskName,row.Registry.Environment,row.Registry.Status,row.Registry.IsSynthetic,ArtifactValid(row.Artifact),row.Artifact.FeatureSchemaHash,row.Artifact.FeatureSchemaHash,null,null,null,null,artifactSubmitter,experimentSubmitter);
    }
    private static bool ArtifactValid(ModelArtifact artifact){try{using var stream=File.OpenRead(artifact.ArtifactPath);return stream.Length==artifact.FileSize&&Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).Equals(artifact.Sha256,StringComparison.OrdinalIgnoreCase);}catch{return false;}}
    private static string? Submitter(string? json){if(string.IsNullOrWhiteSpace(json))return null;try{using var value=JsonDocument.Parse(json);return value.RootElement.TryGetProperty("submitterIdentifier",out var actor)?actor.GetString():null;}catch(JsonException){return null;}}
    private static void ValidateEligible(ModelRegistryRecord value,string approver){if(value.Status!=ModelApprovalStatus.ValidationSelected)throw new InvalidOperationException("Only a ValidationSelected artifact may be approved.");if(value.IsSyntheticDevelopmentOnly)throw new InvalidOperationException("Synthetic artifacts cannot be approved for real serving.");if(!value.ArtifactIntegrityValid)throw new InvalidOperationException("Artifact integrity validation failed.");if(string.IsNullOrWhiteSpace(value.ArtifactSubmitterIdentifier)||string.IsNullOrWhiteSpace(value.ExperimentSubmitterIdentifier))throw new InvalidOperationException("Submitter provenance is missing.");if(!value.ArtifactSubmitterIdentifier.Equals(value.ExperimentSubmitterIdentifier,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Submitter provenance is contradictory.");if(value.ArtifactSubmitterIdentifier.Equals(approver.Trim(),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("A model submitter cannot approve the same artifact.");}
    private sealed record Row(ModelArtifact Artifact,ModelRegistryEntry Registry,MlExperiment Experiment);
}
