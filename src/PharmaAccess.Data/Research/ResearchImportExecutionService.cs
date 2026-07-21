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
    public async Task<ImportCompletion> ExecuteAsync(string privateRoot,string manifestPath,string validationPath,string protocolCode,string protocolVersion,string datasetVersion,int batchSize,string correlationId,string gitCommit,bool resume,CancellationToken ct)
    {
        var protocol=await db.ResearchProtocols.AsNoTracking().SingleOrDefaultAsync(x=>x.ProtocolCode==protocolCode&&x.ProtocolVersion==protocolVersion,ct)??throw new InvalidOperationException("Protocol does not exist.");if(protocol.Status!=ResearchProtocolStatus.Approved)throw new InvalidOperationException("Protocol is not approved.");var manifestText=await File.ReadAllTextAsync(manifestPath,ct);var manifest=JsonSerializer.Deserialize<ResearchSourceManifest>(manifestText,Json)??throw new InvalidOperationException("Manifest invalid.");var validation=JsonSerializer.Deserialize<RealSourceExecutionReport>(await File.ReadAllTextAsync(validationPath,ct),Json)??throw new InvalidOperationException("Validation report invalid.");if(validation.BlockingFindings.Count!=0||validation.RejectedRows!=0)throw new InvalidOperationException("Validation report is blocking.");
        if (!resume && await db.ResearchImportRuns.AsNoTracking().AnyAsync(x => x.CorrelationId == correlationId, ct)) throw new InvalidOperationException("Correlation ID already exists; explicitly request resume.");
        var reports=validation.Files.Where(x=>x.CanonicalForImport).ToDictionary(x=>x.RelativePath,StringComparer.OrdinalIgnoreCase);var sources=manifest.Assignments.Where(x=>reports.ContainsKey(x.RelativePath)).Select(x=>new ImportSourceRegistration(x.RelativePath,x.Sha256,new FileInfo(Path.Combine(privateRoot,x.RelativePath.Replace('/',Path.DirectorySeparatorChar))).Length,x.SchemaProfileVersion,Kind(x.Category),reports[x.RelativePath].TotalRows,true)).OrderBy(x=>x.Kind).ThenBy(x=>x.RelativePath,StringComparer.Ordinal).ToArray();var initialization=new ImportInitialization(datasetVersion,protocol.ResearchProtocolId,correlationId,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(manifestText))),sources,gitCommit);return await new ResearchImportCoordinator(new EfResearchImportPersistence(db)).ExecuteAsync(initialization,ReadBatches(privateRoot,sources,batchSize,ct),batchSize,ct);
    }

    private static async IAsyncEnumerable<(ImportSourceRegistration Source,IReadOnlyList<RawImportRow> Rows)> ReadBatches(string root,IReadOnlyList<ImportSourceRegistration> sources,int batchSize,[EnumeratorCancellation]CancellationToken ct)
    {
        foreach(var source in sources){var path=Path.Combine(root,source.RelativePath.Replace('/',Path.DirectorySeparatorChar));using var reader=new StreamReader(path,Encoding.UTF8,true,1<<20);using var csv=new CsvRecordReader(reader,source.RelativePath.EndsWith(".txt",StringComparison.OrdinalIgnoreCase)?'|':',');var headers=csv.ReadRecord()??throw new InvalidOperationException("Canonical source has no header.");var indexes=headers.Select((x,i)=>(x,i)).ToDictionary(x=>x.x,x=>x.i,StringComparer.OrdinalIgnoreCase);var buffer=new List<RawImportRow>(batchSize);long record=0;string[]? values;while((values=csv.ReadRecord())is not null){ct.ThrowIfCancellationRequested();record++;var map=headers.Select((h,i)=>(h,Value:i<values.Length?values[i]:null)).ToDictionary(x=>x.h,x=>x.Value,StringComparer.OrdinalIgnoreCase);RawImportRow row;if(source.Kind==ImportSourceKind.Fda&&MilestoneNineRealSourceWorkflow.TryClassifyFdaExclusion(values,indexes,source.RelativePath,record,out var exclusion))row=new(record,ImportRowDisposition.ExplicitlyExcluded,map,exclusion!.RuleCode,JsonSerializer.Serialize(exclusion));else row=new(record,ImportRowDisposition.Accepted,map,MappingStatus:source.Kind==ImportSourceKind.Medicaid?NdcStatus(map.GetValueOrDefault("NDC")):null);buffer.Add(row);if(buffer.Count==batchSize){yield return(source,buffer.ToArray());buffer.Clear();await Task.Yield();}}if(buffer.Count!=0)yield return(source,buffer.ToArray());}
    }
    private static string? NdcStatus(string? value){if(string.IsNullOrWhiteSpace(value))return"Invalid";var digits=new string(value.Where(char.IsDigit).ToArray());if(digits.Length==11)return null;if(digits.Length==10)return value.Contains('-')?null:"Ambiguous";return"Invalid";}
    private static ImportSourceKind Kind(RealSourceCategory category)=>category switch{RealSourceCategory.FdaFirstGeneric=>ImportSourceKind.Fda,RealSourceCategory.MedicaidUtilization=>ImportSourceKind.Medicaid,_=>ImportSourceKind.StateReference};
}
