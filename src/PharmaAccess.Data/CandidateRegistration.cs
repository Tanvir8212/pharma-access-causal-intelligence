using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.MachineLearning;
using PharmaAccess.Data.Entities;

namespace PharmaAccess.Data;

public sealed record CandidateRegistrationResult(long ArtifactId, long RegistryEntryId, bool Idempotent);

public sealed class EfServingCandidateRegistration(PharmaAccessDbContext db)
{
    private const string Version = "NextQuarterStateEntry-real-next-quarter-entry-serving-v1";
    private const string ArtifactHash = "62C607C6462D603D04CDF43662B16EAD773138C916912080B15A7F65C85F9022";
    private const string DatasetHash = "84B973AFF0545C987B52C525F0CBAB607C293BEFC1D5EBCC87ED6769DECB0C36";
    private const string FeatureHash = "8E849D11A4D25976F808E49436A6576E74311C4CC97016E7C7126C43210AC750";
    private const string SchemaHash = "BE2EBF510D5E610DD4F54D4E62BB6F9BDB47EACFB496B6739659C7E6AA6C595E";
    private const string SplitHash = "1E6909E1CA6F92D8CBA7944CDBC03E765B3042BE8744F16F6ACECDE18A9DE748";
    private const string R2aHash = "8B049DC35661502EA0E1236B8275D81EA21F540D7D6F729F4FAC2C6A3A4F336D";
    private const string R2bHash = "CD0ADF754622B41A3091A8C47519511F4546A09C26F974412E12C3A4E28D2ACE";
    public static void ValidateBundle(string bundlePath) => Verify(bundlePath);

    public async Task<CandidateRegistrationResult> RegisterAsync(string bundlePath, string submitterIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(submitterIdentifier)) throw new ArgumentException("An authenticated submitter identifier is required.", nameof(submitterIdentifier));
        var candidate = Verify(bundlePath);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var existing = await db.ModelArtifacts.SingleOrDefaultAsync(x => x.ModelVersionCode == Version, cancellationToken);
        if (existing is not null)
        {
            var registry = await db.ModelRegistryEntries.SingleOrDefaultAsync(x => x.ModelArtifactId == existing.ModelArtifactId && x.Environment == "Governance", cancellationToken)
                ?? throw new InvalidOperationException("The existing artifact has no governance registry entry.");
            RequireExisting(existing, registry, candidate);
            await transaction.CommitAsync(cancellationToken);
            return new(existing.ModelArtifactId, registry.ModelRegistryEntryId, true);
        }
        if (await db.ModelArtifacts.AnyAsync(x => x.Sha256 == ArtifactHash, cancellationToken)) throw new InvalidOperationException("The artifact hash is already registered under a different version.");
        await EnsureLineageAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var envelope = JsonSerializer.Serialize(new { submitterIdentifier = submitterIdentifier.Trim(), registeredAtUtc = now, candidate.Manifest, candidate.Verification, candidate.Calibration });
        var experiment = new MlExperiment(Version, 2, 1, envelope, now); db.MlExperiments.Add(experiment); await db.SaveChangesAsync(cancellationToken);
        var run = new ModelTrainingRun(experiment.ExperimentId, JsonSerializer.Serialize(new { selectedThreshold=.08, servingDecisionRule="Probability >= 0.08", probabilityOutput="serialized ML.NET calibrated Probability" }), now); db.ModelTrainingRuns.Add(run); await db.SaveChangesAsync(cancellationToken);
        var artifactNotes = JsonSerializer.Serialize(new { submitterIdentifier=submitterIdentifier.Trim(), dataset="real-2021-2025-v1", datasetVersionId=2, datasetHash=DatasetHash, featureSet="real-next-entry-features-v1", featureSetVersionId=1, featureDefinitionHash=FeatureHash, modelSchemaHash=SchemaHash, splitHash=SplitHash, selectedThreshold=.08, probabilityOutput="serialized ML.NET calibrated Probability", r2aSnapshotHash=R2aHash, r2bVerificationHash=R2bHash, verificationStatus="VerifiedRealModelBinary" });
        var artifact = new ModelArtifact(run.ModelTrainingRunId, Version, candidate.ModelPath, ArtifactHash, 30813, SchemaHash, FeatureHash, artifactNotes, now); db.ModelArtifacts.Add(artifact); await db.SaveChangesAsync(cancellationToken);
        var entry = new ModelRegistryEntry(artifact.ModelArtifactId, now); db.ModelRegistryEntries.Add(entry); await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(artifact.ModelArtifactId, entry.ModelRegistryEntryId, false);
    }

    private async Task EnsureLineageAsync(CancellationToken token)
    {
        var dataset = await db.DatasetVersions.AsNoTracking().SingleOrDefaultAsync(x => x.DatasetVersionId == 2, token);
        if (dataset is null)
        {
            var datasetVersion="real-2021-2025-v1"; var datasetDescription="Verified R2D recovered real dataset lineage."; var featureVersion="real-next-entry-features-v1"; var finalized="Finalized"; var passed="Passed"; var now=DateTime.UtcNow; var notes="SHA256="+DatasetHash;
            await db.Database.ExecuteSqlInterpolatedAsync($@"SET IDENTITY_INSERT [core].[DatasetVersion] ON;
INSERT INTO [core].[DatasetVersion] ([DatasetVersionId],[VersionCode],[Description],[SchemaVersion],[FeatureVersion],[Status],[CreatedAtUtc],[FinalizedAtUtc],[CodeCommitHash],[TotalSourceFiles],[TotalRows],[ValidationStatus],[Notes])
VALUES (2,{datasetVersion},{datasetDescription},{datasetVersion},{featureVersion},{finalized},{now},{now},NULL,0,NULL,{passed},{notes});
SET IDENTITY_INSERT [core].[DatasetVersion] OFF;", token);
        }
        else if (dataset.VersionCode.Value != "real-2021-2025-v1" || dataset.Notes?.Contains(DatasetHash, StringComparison.OrdinalIgnoreCase) != true) throw new InvalidOperationException("Dataset lineage ID 2 conflicts with the verified bundle.");
        var feature = await db.FeatureSetVersions.AsNoTracking().SingleOrDefaultAsync(x => x.FeatureSetVersionId == 1, token);
        if (feature is null)
        {
            var version="real-next-entry-features-v1"; var description="Verified R2D recovered real feature lineage."; var finalized="Finalized"; var passed="Passed"; var notes="Recovered from verified R2A/R2B evidence."; var now=DateTime.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($@"SET IDENTITY_INSERT [feature].[FeatureSetVersion] ON;
INSERT INTO [feature].[FeatureSetVersion] ([FeatureSetVersionId],[VersionCode],[Description],[DatasetVersionId],[Status],[DefinitionHash],[CodeCommitHash],[CreatedAtUtc],[FinalizedAtUtc],[ValidationStatus],[Notes])
VALUES (1,{version},{description},2,{finalized},{FeatureHash},NULL,{now},{now},{passed},{notes});
SET IDENTITY_INSERT [feature].[FeatureSetVersion] OFF;", token);
        }
        else if (feature.DatasetVersionId != 2 || feature.VersionCode != "real-next-entry-features-v1" || !feature.DefinitionHash.Equals(FeatureHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Feature lineage ID 1 conflicts with the verified bundle.");
    }

    private static VerifiedCandidate Verify(string bundlePath)
    {
        var root=Path.GetFullPath(bundlePath); Require(Directory.Exists(root),"Serving bundle does not exist.");
        var manifestPath=Path.Combine(root,"manifest.json"); var verificationPath=Path.Combine(root,"verification.json"); var calibrationPath=Path.Combine(root,"threshold-calibration.json"); var modelPath=Path.Combine(root,"model.zip");
        foreach(var path in new[]{manifestPath,verificationPath,calibrationPath,modelPath,Path.Combine(root,"model-card.md")}) Require(File.Exists(path),$"Required bundle file is missing: {Path.GetFileName(path)}");
        Require(!Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).Any(x=>x.Contains("final-test-features",StringComparison.OrdinalIgnoreCase)||x.Contains("private",StringComparison.OrdinalIgnoreCase)),"Private R2A material is present in the serving bundle.");
        using var manifest=JsonDocument.Parse(File.ReadAllText(manifestPath)); using var verification=JsonDocument.Parse(File.ReadAllText(verificationPath)); using var calibration=JsonDocument.Parse(File.ReadAllText(calibrationPath)); var m=manifest.RootElement; var v=verification.RootElement; var c=calibration.RootElement;
        Require(!m.GetProperty("syntheticDevelopmentData").GetBoolean() && !m.GetProperty("approved").GetBoolean() && !m.GetProperty("champion").GetBoolean(),"Bundle governance or synthetic flags are unsafe.");
        Require(S(m,"bundleVersion")==Version&&S(m,"task")=="NextQuarterStateEntry"&&S(m,"trainer")=="FastTree"&&S(m,"dataset")=="real-2021-2025-v1"&&m.GetProperty("datasetVersionId").GetInt32()==2&&H(m,"datasetHash",DatasetHash),"Bundle identity or dataset lineage mismatch.");
        Require(S(m,"featureSet")=="real-next-entry-features-v1"&&m.GetProperty("featureSetVersionId").GetInt32()==1&&H(m,"featureSetDefinitionHash",FeatureHash)&&H(m,"modelInputSchemaHash",SchemaHash)&&H(m,"splitManifestHash",SplitHash),"Feature, schema, or split lineage mismatch.");
        Require(H(m,"artifactHash",ArtifactHash)&&m.GetProperty("artifactSizeBytes").GetInt64()==30813&&m.GetProperty("selectedThreshold").GetDouble()==.08&&S(m,"servingDecisionRule")=="Probability >= 0.08"&&S(m,"probabilityOutput")=="serialized ML.NET calibrated Probability", "Artifact or serving contract mismatch.");
        Require(S(m,"governanceStatus")==nameof(ModelApprovalStatus.ValidationSelected)&&S(m,"verificationStatus")=="VerifiedRealModelBinary"&&H(m,"r2aSnapshotHash",R2aHash)&&H(m,"r2bVerificationHash",R2bHash),"Verification provenance mismatch.");
        Require(S(v,"verificationStatus")=="VerifiedRealModelBinary"&&v.GetProperty("realTestRowsScored").GetInt32()==12477&&v.GetProperty("stableKeyMatches").GetInt32()==12477&&v.GetProperty("exactSingleProbabilityRoundTripMatches").GetInt32()==12477&&v.GetProperty("externalThresholdDecisionMatches").GetInt32()==12477&&H(v,"r2aSnapshotHash",R2aHash)&&H(v,"r2bVerificationHash",R2bHash)&&!v.GetProperty("privateSnapshotIncluded").GetBoolean(),"R2B verification metadata mismatch.");
        Require(c.GetProperty("selectedExternalThreshold").GetDouble()==.08&&S(c,"servingDecisionRule")=="Probability >= 0.08"&&!c.GetProperty("serializedPredictedLabelMayBeUsedForServingDecision").GetBoolean(),"Threshold calibration mismatch.");
        using var stream=File.OpenRead(modelPath); Require(stream.Length==30813&&Convert.ToHexString(SHA256.HashData(stream))==ArtifactHash,"Model artifact tampering detected.");
        return new(root,modelPath,m.GetRawText(),v.GetRawText(),c.GetRawText());
    }
    private static void RequireExisting(ModelArtifact a, ModelRegistryEntry r, VerifiedCandidate c) { Require(a.Sha256.Equals(ArtifactHash,StringComparison.OrdinalIgnoreCase)&&a.FileSize==30813&&a.InputSchemaHash.Equals(SchemaHash,StringComparison.OrdinalIgnoreCase)&&a.FeatureSchemaHash.Equals(FeatureHash,StringComparison.OrdinalIgnoreCase)&&!a.IsApproved&&a.ApprovalStatus==ModelApprovalStatus.ValidationSelected&&r.Status==ModelApprovalStatus.ValidationSelected&&!r.IsChampion&&!r.IsSynthetic,"Existing registration conflicts with the verified candidate."); }
    private static string S(JsonElement e,string n)=>e.GetProperty(n).GetString()??""; private static bool H(JsonElement e,string n,string x)=>S(e,n).Equals(x,StringComparison.OrdinalIgnoreCase); private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}
    private sealed record VerifiedCandidate(string Root,string ModelPath,string Manifest,string Verification,string Calibration);
}
