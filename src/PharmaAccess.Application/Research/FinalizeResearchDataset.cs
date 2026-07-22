namespace PharmaAccess.Application.Research;

public sealed record FinalArtifactContract(string ArtifactType,string RelativePath,string Sha256,long ByteSize);

public sealed record FinalizeResearchDatasetCommand(
    string DatasetVersion,string Actor,string CorrelationId,DateTime FinalizedAtUtc,string GitCommitHash,
    string SourceManifestHash,string PanelHash,string CohortHash,string FeatureSchemaHash,string SplitHash,
    string CausalProtocolHash,string PythonEnvironmentHash,string DotNetEnvironmentHash,
    string HumanDecision,IReadOnlyList<FinalArtifactContract> Artifacts);

public sealed record FinalizeResearchDatasetResult(string DatasetStatus,string ValidationStatus,long TotalRows,DateTime FinalizedAtUtc,long FreezeId,string CorrelationId);

public interface IResearchDatasetFinalizationService
{
    Task<FinalizeResearchDatasetResult> FinalizeAsync(FinalizeResearchDatasetCommand command,CancellationToken cancellationToken=default);
}
