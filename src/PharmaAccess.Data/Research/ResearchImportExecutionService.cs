using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.Research;
using PharmaAccess.Domain.Research;

namespace PharmaAccess.Data.Research;

public sealed class ResearchImportExecutionService(PharmaAccessDbContext db)
{
    private static readonly JsonSerializerOptions Json=new(){PropertyNameCaseInsensitive=true};

    public async Task<ImportCompletion> ExecuteAsync(string privateRoot,string manifestPath,string validationPath,string protocolCode,string protocolVersion,string datasetVersion,int batchSize,string correlationId,string gitCommit,bool resume,string? resumeAuditId,CancellationToken ct)
    {
        var initialization=await PrepareAsync(privateRoot,manifestPath,validationPath,protocolCode,protocolVersion,datasetVersion,correlationId,gitCommit,resume,resumeAuditId,ct);var resumeRows=resume?await LoadResumeRowsAsync(correlationId,ct):new Dictionary<string,long>(StringComparer.OrdinalIgnoreCase);
        return await new ResearchImportCoordinator(new EfResearchImportPersistence(db)).ExecuteAsync(initialization,ReadBatches(privateRoot,initialization.Sources.Where(x=>x.Canonical).ToArray(),batchSize,resumeRows,ct),batchSize,ct);
    }

    public async Task<ValidatedResearchImportInitialization> ValidateInitializationAsync(string privateRoot,string manifestPath,string validationPath,string protocolCode,string protocolVersion,string datasetVersion,string correlationId,string gitCommit,CancellationToken ct)
    {
        var initialization=await PrepareAsync(privateRoot,manifestPath,validationPath,protocolCode,protocolVersion,datasetVersion,correlationId,gitCommit,false,null,ct);
        return ResearchImportInitializationFactory.Build(initialization,DateTime.UtcNow);
    }

    public async Task<(long RowsCommitted,string NextSource,long NextRow,long Remaining)> ValidateResumeAsync(string privateRoot,string manifestPath,string validationPath,string protocolCode,string protocolVersion,string datasetVersion,string previousCorrelationId,string resumeAuditId,string gitCommit,CancellationToken ct)
    {
        var initialization=await PrepareAsync(privateRoot,manifestPath,validationPath,protocolCode,protocolVersion,datasetVersion,previousCorrelationId,gitCommit,true,resumeAuditId,ct);return await new EfResearchImportPersistence(db).ValidateResumeAsync(initialization,ct);
    }

    private async Task<ImportInitialization> PrepareAsync(string privateRoot,string manifestPath,string validationPath,string protocolCode,string protocolVersion,string datasetVersion,string correlationId,string gitCommit,bool resume,string? resumeAuditId,CancellationToken ct)
    {
        var protocol=await db.ResearchProtocols.AsNoTracking().SingleOrDefaultAsync(x=>x.ProtocolCode==protocolCode&&x.ProtocolVersion==protocolVersion,ct)??throw new InvalidOperationException("Protocol does not exist.");
        if(protocol.Status!=ResearchProtocolStatus.Approved)throw new InvalidOperationException("Protocol is not approved.");
        if(!resume&&await db.ResearchImportRuns.AsNoTracking().AnyAsync(x=>x.CorrelationId==correlationId,ct))throw new InvalidOperationException("Correlation ID already exists; explicitly request resume.");
        var manifestText=await File.ReadAllTextAsync(manifestPath,ct);
        var manifest=JsonSerializer.Deserialize<ResearchSourceManifest>(manifestText,Json)??throw new InvalidOperationException("Manifest invalid.");
        var validation=JsonSerializer.Deserialize<RealSourceExecutionReport>(await File.ReadAllTextAsync(validationPath,ct),Json)??throw new InvalidOperationException("Validation report invalid.");
        if(validation.BlockingFindings.Count!=0||validation.RejectedRows!=0)throw new InvalidOperationException("Validation report is blocking.");
        var reports=validation.Files.ToDictionary(x=>x.RelativePath,StringComparer.OrdinalIgnoreCase);
        var sources=manifest.Assignments.Select(x=>
        {
            if(!reports.TryGetValue(x.RelativePath,out var report))throw new InvalidOperationException($"Validation report is missing source assignment '{x.RelativePath}'.");
            var local=Path.Combine(privateRoot,x.RelativePath.Replace('/',Path.DirectorySeparatorChar));
            return new ImportSourceRegistration(x.RelativePath,x.Sha256,new FileInfo(local).Length,x.SchemaProfileVersion,Kind(x.Category),report.TotalRows,report.CanonicalForImport,report.Encoding,NormalizeDelimiter(report.Delimiter),x.CoverageStart,x.CoverageEnd);
        }).OrderBy(x=>x.Kind).ThenBy(x=>x.RelativePath,StringComparer.Ordinal).ToArray();
        return new(datasetVersion,protocol.ResearchProtocolId,correlationId,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifestText))),sources,gitCommit,resumeAuditId);
    }

    private async Task<Dictionary<string,long>> LoadResumeRowsAsync(string correlationId,CancellationToken ct){var run=await db.ResearchImportRuns.AsNoTracking().SingleAsync(x=>x.CorrelationId==correlationId,ct);var files=await db.SourceFiles.AsNoTracking().Where(x=>x.DatasetVersionId==run.DatasetVersionId).ToDictionaryAsync(x=>x.SourceFileId,x=>x.Sha256,ct);var checkpoints=await db.ResearchImportCheckpoints.AsNoTracking().Where(x=>x.ResearchImportRunId==run.ResearchImportRunId).ToArrayAsync(ct);return checkpoints.ToDictionary(x=>files[x.SourceFileId],x=>x.LastCompletedRow,StringComparer.OrdinalIgnoreCase);}
    private static async IAsyncEnumerable<(ImportSourceRegistration Source,IReadOnlyList<RawImportRow> Rows)> ReadBatches(string root,IReadOnlyList<ImportSourceRegistration> sources,int batchSize,IReadOnlyDictionary<string,long> resumeRows,[EnumeratorCancellation]CancellationToken ct)
    {
        foreach(var source in sources){var path=Path.Combine(root,source.RelativePath.Replace('/',Path.DirectorySeparatorChar));using var reader=new StreamReader(path,Encoding.UTF8,true,1<<20);using var csv=new CsvRecordReader(reader,source.RelativePath.EndsWith(".txt",StringComparison.OrdinalIgnoreCase)?'|':',');var headers=csv.ReadRecord()??throw new InvalidOperationException("Canonical source has no header.");var indexes=headers.Select((x,i)=>(x,i)).ToDictionary(x=>x.x,x=>x.i,StringComparer.OrdinalIgnoreCase);var buffer=new List<RawImportRow>(batchSize);long record=0;var skipThrough=resumeRows.TryGetValue(source.Sha256,out var checkpointRow)?checkpointRow:0;string[]? values;while((values=csv.ReadRecord())is not null){record++;if(record<=skipThrough)continue;ct.ThrowIfCancellationRequested();var map=headers.Select((h,i)=>(h,Value:i<values.Length?values[i]:null)).ToDictionary(x=>x.h,x=>x.Value,StringComparer.OrdinalIgnoreCase);RawImportRow row;if(source.Kind==ImportSourceKind.Fda&&MilestoneNineRealSourceWorkflow.TryClassifyFdaExclusion(values,indexes,source.RelativePath,record,out var exclusion))row=new(record,ImportRowDisposition.ExplicitlyExcluded,map,exclusion!.RuleCode,JsonSerializer.Serialize(exclusion));else row=new(record,ImportRowDisposition.Accepted,map,MappingStatus:source.Kind==ImportSourceKind.Medicaid?NdcStatus(map.GetValueOrDefault("NDC")):null);buffer.Add(row);if(buffer.Count==batchSize){yield return(source,buffer.ToArray());buffer.Clear();await Task.Yield();}}if(buffer.Count!=0)yield return(source,buffer.ToArray());}
    }
    private static string? NdcStatus(string? value){if(string.IsNullOrWhiteSpace(value))return"Invalid";var digits=new string(value.Where(char.IsDigit).ToArray());if(digits.Length==11)return null;if(digits.Length==10)return value.Contains('-')?null:"Ambiguous";return"Invalid";}
    private static string NormalizeDelimiter(string delimiter)=>delimiter.Equals("entry-specific",StringComparison.OrdinalIgnoreCase)?"N/A":delimiter;
    private static ImportSourceKind Kind(RealSourceCategory category)=>category switch{RealSourceCategory.FdaFirstGeneric=>ImportSourceKind.Fda,RealSourceCategory.MedicaidUtilization=>ImportSourceKind.Medicaid,RealSourceCategory.StateAdjacency=>ImportSourceKind.StateAdjacency,RealSourceCategory.RxNormMapping=>ImportSourceKind.RxNormMapping,RealSourceCategory.ManualMapping=>ImportSourceKind.ManualMapping,_=>ImportSourceKind.StateReference};
}
