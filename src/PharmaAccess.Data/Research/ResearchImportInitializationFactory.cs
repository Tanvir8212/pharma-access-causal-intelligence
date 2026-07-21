using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PharmaAccess.Application.Research;
using PharmaAccess.Data.Entities;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.Research;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Data.Research;

public sealed record ValidatedResearchImportInitialization(DatasetVersion Dataset, IReadOnlyList<(ImportSourceRegistration Source, SourceFile File, ResearchSourceRegistration Registration)> Sources, ResearchImportRunEntity Run, ResearchImportAuditEventEntity Audit);

public static class ResearchImportInitializationFactory
{
    public static ValidatedResearchImportInitialization Build(ImportInitialization request, DateTime timestampUtc)
    {
        var sources = request.Sources.OrderBy(x=>x.Kind).ThenBy(x=>x.RelativePath,StringComparer.Ordinal).Select(source=>BuildSource(request.VersionCode,source,timestampUtc)).ToArray();
        var uniqueSourceFiles=sources.Select(x=>x.Source.Sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var dataset = new DatasetVersion(new DatasetVersionCode(request.VersionCode), "real-import-v1", uniqueSourceFiles, timestampUtc, "Real import; non-final until separate validation and approval; all source assignments remain in research registration lineage.", codeCommitHash:request.GitCommit);
        var run=new ResearchImportRunEntity{ResearchProtocolId=request.ProtocolId,CorrelationId=Required(request.CorrelationId,128,"correlationId"),SourceManifestHash=Hash(request.ManifestHash,"manifestHash"),Status="Importing",StartedAtUtc=timestampUtc};
        var audit=new ResearchImportAuditEventEntity{EventType="ImportInitialized",DetailJson=JsonSerializer.Serialize(new{versionCode=request.VersionCode,sourceAssignmentCount=request.Sources.Count,uniqueSourceFileCount=uniqueSourceFiles}),CreatedAtUtc=timestampUtc};
        return new(dataset,sources,run,audit);
    }

    private static (ImportSourceRegistration Source,SourceFile File,ResearchSourceRegistration Registration) BuildSource(string versionCode,ImportSourceRegistration source,DateTime at)
    {
        if(Path.IsPathRooted(source.RelativePath)||source.RelativePath.Contains("..",StringComparison.Ordinal)||source.RelativePath.Contains('\\'))throw new ArgumentException("Research field 'registeredPath' must be a normalized repository-relative forward-slash path.","registeredPath");
        var metadata=Metadata(source.Kind);var code=$"src-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.RelativePath)))[..20]}".ToLowerInvariant();
        var file=new SourceFile(1,SourceType(source.Kind),Path.GetFileName(source.RelativePath),source.Sha256,source.ByteSize,source.SchemaVersion,at,rowCount:source.ExpectedRows,reportingPeriod:Coverage(source),licenseNote:metadata.License);
        var registration=new ResearchSourceRegistration(code,RegistryType(source.Kind),metadata.Authority,metadata.DatasetName,metadata.Description,metadata.OfficialReference,at,source.CoverageStart,source.CoverageEnd,metadata.License,source.SchemaVersion,Path.GetFileName(source.RelativePath),source.RelativePath,source.Sha256,source.ByteSize,source.Encoding,source.Delimiter,false,"PharmaAccess.Worker/guarded-real-import",at);registration.MarkValidated(source.ExpectedRows,[]);return(source,file,registration);
    }
    private static string? Coverage(ImportSourceRegistration s)=>s.CoverageStart is null&&s.CoverageEnd is null?null:$"{s.CoverageStart:yyyy-MM-dd}/{s.CoverageEnd:yyyy-MM-dd}";
    private static string Required(string? v,int max,string field)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException($"Research field '{field}' is required and cannot be blank.",field):v.Trim().Length>max?throw new ArgumentException($"Research field '{field}' exceeds the maximum length of {max} characters.",field):v.Trim();
    private static string Hash(string v,string field){v=Required(v,64,field).ToUpperInvariant();return v.Length==64&&v.All(Uri.IsHexDigit)?v:throw new ArgumentException($"Research field '{field}' must contain exactly 64 hexadecimal characters.",field);}
    private sealed record SourceMetadata(string Authority,string DatasetName,string Description,string OfficialReference,string License);
    private static SourceMetadata Metadata(ImportSourceKind kind)=>kind switch{
        ImportSourceKind.Fda=>new("U.S. Food and Drug Administration","First Generic Drug Approvals","FDA annual first-generic approval reference; approval is a launch proxy.","https://www.fda.gov/drugs/drug-and-biologic-approval-and-ind-activity-reports/first-generic-drug-approvals","U.S. government public data; preserve official provenance and source terms."),
        ImportSourceKind.Medicaid=>new("Centers for Medicare & Medicaid Services","Medicaid State Drug Utilization Data","CMS state-level Medicaid drug utilization and accompanying data dictionary.","https://data.medicaid.gov/dataset/0ad65fe5-3ad3-5d79-a3f9-7893ded7963a","CMS public data; preserve official provenance, suppression semantics, and source terms."),
        ImportSourceKind.StateAdjacency=>new("U.S. Census Bureau","County Adjacency File","Census county adjacency reference used to derive versioned geographic relationships.","https://www.census.gov/geographies/reference-files/2010/geo/county-adjacency.html","U.S. Census Bureau public reference data; preserve official provenance."),
        ImportSourceKind.RxNormMapping=>new("U.S. National Library of Medicine","RxNorm","Versioned RxNorm drug-normalization reference.","https://www.nlm.nih.gov/research/umls/rxnorm/","Use is governed by the authoritative RxNorm/UMLS source terms."),
        ImportSourceKind.ManualMapping=>new("PharmaAccess human review","Versioned manual research mappings","Human-reviewed mapping decisions with repository audit lineage.","repository://docs/research/source-registry","Internal research mapping; changes require versioned human review."),
        _=>new("U.S. Census Bureau","Census Regions and Divisions","Census state, region, and division reference.","https://www2.census.gov/geo/pdfs/maps-data/maps/reference/us_regdiv.pdf","U.S. Census Bureau public reference data; preserve official provenance.")};
    private static SourceType SourceType(ImportSourceKind k)=>k switch{ImportSourceKind.Fda=>Domain.Entities.SourceType.FDAFirstGeneric,ImportSourceKind.Medicaid=>Domain.Entities.SourceType.MedicaidStateDrugUtilization,_=>Domain.Entities.SourceType.StateReference};
    private static ResearchSourceType RegistryType(ImportSourceKind k)=>k switch{ImportSourceKind.Fda=>ResearchSourceType.FdaFirstGenericApproval,ImportSourceKind.Medicaid=>ResearchSourceType.CmsMedicaidStateDrugUtilization,ImportSourceKind.StateAdjacency=>ResearchSourceType.GeographicRegionAdjacency,ImportSourceKind.RxNormMapping=>ResearchSourceType.RxNormDrugNormalization,ImportSourceKind.ManualMapping=>ResearchSourceType.ManuallyCuratedMapping,_=>ResearchSourceType.StateTerritoryReference};
}
