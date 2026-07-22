using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PharmaAccess.Application.Research;
using PharmaAccess.Data.Entities;
using PharmaAccess.Domain.Features;
using PharmaAccess.Domain.Research;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Data.Research;

public sealed class ResearchDatasetFinalizationService(PharmaAccessDbContext db) : IResearchDatasetFinalizationService
{
    public async Task<FinalizeResearchDatasetResult> FinalizeAsync(FinalizeResearchDatasetCommand c,CancellationToken ct=default)
    {
        Validate(c);
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        try
        {
            var ownership=await db.ResearchDatabaseOwnership.AsNoTracking().SingleAsync(ct);
            if(ownership.ProjectId!="PharmaAccessCausalIntelligence"||ownership.RepositoryMarker!="pharma-access-causal-intelligence")throw new InvalidOperationException("Research database ownership verification failed.");
            if(await db.ResearchDataFreezes.AnyAsync(x=>x.FreezeCode=="real-2021-2025-v1-final"&&x.FreezeVersion=="1.0",ct))throw new InvalidOperationException("The real dataset is already frozen; double finalization is refused.");
            var protocols=await db.ResearchProtocols.Where(x=>x.ProtocolCode=="approval-to-access-real"&&new[]{"1.0","1.1","1.2"}.Contains(x.ProtocolVersion)&&x.Status==ResearchProtocolStatus.Approved).ToListAsync(ct);
            if(protocols.Count!=3)throw new InvalidOperationException("All governed real research protocols must be approved.");
            var protocol=protocols.Single(x=>x.ProtocolVersion=="1.2");
            if(!protocol.DefinitionHash.Equals(c.CausalProtocolHash,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Approved protocol hash mismatch.");
            var dataset=await db.DatasetVersions.SingleAsync(x=>x.VersionCode==new DatasetVersionCode(c.DatasetVersion),ct);
            if(dataset.Status!=PharmaAccess.Domain.Entities.DatasetVersionStatus.Validating||dataset.ValidationStatus!=PharmaAccess.Domain.Entities.DatasetValidationStatus.InProgress||dataset.FinalizedAtUtc is not null||dataset.TotalRows!=174471)throw new InvalidOperationException("Dataset lifecycle precondition failed.");
            await VerifyDatabaseGates(ct);
            var feature=await db.FeatureSetVersions.SingleOrDefaultAsync(x=>x.VersionCode=="real-next-entry-features-v1",ct);
            if(feature is null){feature=new FeatureSetVersion("real-next-entry-features-v1",dataset.DatasetVersionId,c.FeatureSchemaHash,c.FinalizedAtUtc,"Governed ANDA-state-quarter analysis panel",c.GitCommitHash,"Final closeout feature lineage.");feature.MarkBuilding();feature.MarkValidating();feature.MarkValidated();feature.FinalizeVersion(c.FinalizedAtUtc);db.FeatureSetVersions.Add(feature);await db.SaveChangesAsync(ct);}
            if(feature.FeatureSetVersionId!=protocol.FeatureSetVersionId)throw new InvalidOperationException("Protocol feature-set lineage mismatch.");
            var freeze=new ResearchDataFreeze("real-2021-2025-v1-final","1.0",protocol.ResearchProtocolId,dataset.DatasetVersionId,feature.FeatureSetVersionId,c.SourceManifestHash,c.PanelHash,c.CohortHash,c.FeatureSchemaHash,c.SplitHash,c.CausalProtocolHash,c.PythonEnvironmentHash,c.DotNetEnvironmentHash,c.GitCommitHash,false,c.FinalizedAtUtc,$"CorrelationId={c.CorrelationId}");
            for(var next=ResearchFreezeStatus.SourceFilesRegistered;next<=ResearchFreezeStatus.ValidationBundleGenerated;next++)freeze.Advance(next);
            freeze.RecordFindings(0,1);freeze.Approve(protocol,c.Actor,c.FinalizedAtUtc,$"{c.HumanDecision} CorrelationId={c.CorrelationId}");
            db.ResearchDataFreezes.Add(freeze);await db.SaveChangesAsync(ct);
            foreach(var a in c.Artifacts)db.ResearchFreezeArtifacts.Add(new ResearchFreezeArtifactEntity{ResearchDataFreezeId=freeze.ResearchDataFreezeId,ArtifactType=a.ArtifactType,RelativePath=a.RelativePath,Sha256=a.Sha256,ByteSize=a.ByteSize,CreatedAtUtc=c.FinalizedAtUtc});
            db.ResearchFreezeFindings.Add(new ResearchFreezeFindingEntity{ResearchDataFreezeId=freeze.ResearchDataFreezeId,Code="PYTHON_DOTNET_ESTIMATOR_DISCREPANCY_ACCEPTED",Severity="Warning",Description=c.HumanDecision,Recommendation="Disclose both estimates without changing the approved estimand.",CreatedAtUtc=c.FinalizedAtUtc});
            dataset.MarkValidated(174471);dataset.FinalizeVersion(c.FinalizedAtUtc);
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return new(dataset.Status.ToString(),dataset.ValidationStatus.ToString(),dataset.TotalRows!.Value,dataset.FinalizedAtUtc!.Value,freeze.ResearchDataFreezeId,c.CorrelationId);
        }catch{await tx.RollbackAsync(CancellationToken.None);db.ChangeTracker.Clear();throw;}
    }

    private async Task VerifyDatabaseGates(CancellationToken ct)
    {
        var cn=db.Database.GetDbConnection();
        await using var cmd=cn.CreateCommand();cmd.Transaction=db.Database.CurrentTransaction!.GetDbTransaction();cmd.CommandTimeout=0;
        cmd.CommandText="""
SELECT CASE WHEN
(SELECT COUNT(*) FROM research.AndaLaunch WHERE EligibilityCategory IN ('A','B'))=261 AND
(SELECT COUNT(*) FROM research.AndaLaunch WHERE EligibilityCategory IN ('A','B') AND Partition='Training')=147 AND
(SELECT COUNT(*) FROM research.AndaLaunch WHERE EligibilityCategory IN ('A','B') AND Partition='Validation')=66 AND
(SELECT COUNT(*) FROM research.AndaLaunch WHERE EligibilityCategory IN ('A','B') AND Partition='LockedTest')=48 AND
(SELECT COUNT(*) FROM research.AndaStateQuarterPanel)=174471 AND
NOT EXISTS(SELECT 1 FROM research.AndaLaunchMappingEvidence GROUP BY MedicaidNormalizedNdc HAVING COUNT(DISTINCT AndaLaunchId)>1) AND
NOT EXISTS(SELECT 1 FROM research.AndaStateQuarterPanel WHERE IsSuppressed=1 AND (PrescriptionCount IS NOT NULL OR ReimbursementAmount IS NOT NULL)) AND
NOT EXISTS(SELECT 1 FROM research.AndaStateQuarterPanel WHERE IsCensored=1 AND LabelNextQuarterEntry IS NOT NULL)
THEN 1 ELSE 0 END
""";
        if(Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))!=1)throw new InvalidOperationException("Database reconciliation gate failed.");
    }
    private static void Validate(FinalizeResearchDatasetCommand c)
    {
        if(c.DatasetVersion!="real-2021-2025-v1"||string.IsNullOrWhiteSpace(c.Actor)||string.IsNullOrWhiteSpace(c.CorrelationId)||c.FinalizedAtUtc.Kind!=DateTimeKind.Utc||c.Artifacts.Count==0)throw new ArgumentException("Finalization command metadata is invalid.");
        foreach(var h in new[]{c.SourceManifestHash,c.PanelHash,c.CohortHash,c.FeatureSchemaHash,c.SplitHash,c.CausalProtocolHash,c.PythonEnvironmentHash,c.DotNetEnvironmentHash}.Concat(c.Artifacts.Select(x=>x.Sha256)))if(h.Length!=64||h.Any(x=>!Uri.IsHexDigit(x)))throw new ArgumentException("Finalization SHA-256 value is invalid.");
        if(!c.HumanDecision.Contains("material",StringComparison.OrdinalIgnoreCase)||!c.HumanDecision.Contains("not a finalization blocker",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Explicit human acceptance of the estimator discrepancy is required.");
        if(c.Artifacts.Select(x=>x.ArtifactType).Distinct(StringComparer.Ordinal).Count()!=c.Artifacts.Count)throw new ArgumentException("Final artifact types must be unique.");
    }
}
