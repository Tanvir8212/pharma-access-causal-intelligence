using PharmaAccess.Domain.Research;
namespace PharmaAccess.Application.Research;

public enum ImportSourceKind { Fda, Medicaid, StateReference, StateAdjacency, RxNormMapping, ManualMapping }
public enum ImportRowDisposition { Accepted, Rejected, Duplicate, ExplicitlyExcluded }
public sealed record ImportSourceRegistration(string RelativePath,string Sha256,long ByteSize,string SchemaVersion,ImportSourceKind Kind,long ExpectedRows,bool Canonical,string Encoding= "utf-8",string Delimiter=",",DateOnly? CoverageStart=null,DateOnly? CoverageEnd=null);
public sealed record RawImportRow(long SourceRowNumber,ImportRowDisposition Disposition,IReadOnlyDictionary<string,string?> Values,string? ReasonCode=null,string? EvidenceJson=null,string? MappingStatus=null);
public sealed record ImportBatch(string CorrelationId,string SourceSha256,ImportSourceKind Kind,int BatchNumber,long StartRow,IReadOnlyList<RawImportRow> Rows,string AggregateHash);
public sealed record ImportInitialization(string VersionCode,long ProtocolId,string CorrelationId,string ManifestHash,IReadOnlyList<ImportSourceRegistration> Sources,string GitCommit);
public sealed record ImportSession(long ImportRunId,int DatasetVersionId,IReadOnlyDictionary<string,int> SourceFileIds);
public sealed record ImportBatchResult(long Accepted,long Rejected,long Duplicate,long ExplicitlyExcluded,long LastCompletedRow);
public sealed record ImportCompletion(long Registered,long Accepted,long Rejected,long Duplicate,long ExplicitlyExcluded,string ReconciliationHash,string ProfileJson);

public interface IResearchImportPersistence
{
    Task<ImportSession> InitializeAsync(ImportInitialization request,CancellationToken cancellationToken);
    Task<ImportBatchResult> PersistBatchAsync(ImportSession session,ImportBatch batch,CancellationToken cancellationToken);
    Task CompleteAsync(ImportSession session,ImportCompletion completion,CancellationToken cancellationToken);
    Task<ImportCheckpoint?> GetCheckpointAsync(long importRunId,string sourceSha256,CancellationToken cancellationToken);
}

public sealed class ResearchImportCoordinator(IResearchImportPersistence persistence)
{
    public async Task<ImportCompletion> ExecuteAsync(ImportInitialization initialization,IAsyncEnumerable<(ImportSourceRegistration Source,IReadOnlyList<RawImportRow> Rows)> batches,int batchSize,CancellationToken cancellationToken)
    {
        if(batchSize<=0)throw new ArgumentOutOfRangeException(nameof(batchSize)); var ordered=initialization with{Sources=initialization.Sources.OrderBy(x=>x.Kind).ThenBy(x=>x.RelativePath,StringComparer.Ordinal).ToArray()};var session=await persistence.InitializeAsync(ordered,cancellationToken);long registered=0,accepted=0,rejected=0,duplicate=0,excluded=0;var batchNumber=0;
        await foreach(var item in batches.WithCancellation(cancellationToken)){if(!item.Source.Canonical)continue;registered+=item.Rows.Count;foreach(var chunk in item.Rows.Chunk(batchSize)){batchNumber++;var hash=ReconciliationHasher.Hash(chunk.Length,chunk.Count(x=>x.Disposition==ImportRowDisposition.Accepted),chunk.Count(x=>x.Disposition==ImportRowDisposition.Rejected),chunk.Count(x=>x.Disposition==ImportRowDisposition.Duplicate),chunk.Count(x=>x.Disposition==ImportRowDisposition.ExplicitlyExcluded));var result=await persistence.PersistBatchAsync(session,new(initialization.CorrelationId,item.Source.Sha256,item.Source.Kind,batchNumber,chunk[0].SourceRowNumber,chunk,hash),cancellationToken);accepted+=result.Accepted;rejected+=result.Rejected;duplicate+=result.Duplicate;excluded+=result.ExplicitlyExcluded;}}
        if(registered!=accepted+rejected+duplicate+excluded)throw new InvalidOperationException("Import reconciliation identity failed.");var reconciliation=ReconciliationHasher.Hash(registered,accepted,rejected,duplicate,excluded);var completion=new ImportCompletion(registered,accepted,rejected,duplicate,excluded,reconciliation,"{}");await persistence.CompleteAsync(session,completion,cancellationToken);return completion;
    }
}
