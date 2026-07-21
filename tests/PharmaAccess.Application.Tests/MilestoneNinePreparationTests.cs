using System.IO.Compression;
using System.Security.Cryptography;
using PharmaAccess.Application.Research;
using Xunit;

namespace PharmaAccess.Application.Tests;

public sealed class MilestoneNinePreparationTests
{
    [Fact] public void Discovery_is_metadata_only_bounded_and_root_relative()
    {
        using var temp = Temp.Create(); File.WriteAllText(Path.Combine(temp.Path, "fda.csv"), "Ingredient,ApprovalDate\nExample,2024-01-01\n");
        var result = new ResearchSourceDiscoveryService().Discover(new(temp.Path, RealSourceCategory.FdaFirstGeneric, false, new HashSet<string> { ".csv" }, 2, 1024, "corr-1"));
        var file = Assert.Single(result.Files); Assert.Equal("fda.csv", file.RelativePath); Assert.Equal(64, file.Sha256.Length); Assert.DoesNotContain(temp.Path, file.RelativePath);
    }

    [Fact] public void Private_policy_rejects_escape_url_and_network_path()
    {
        using var temp = Temp.Create(); var policy = new PrivatePathPolicy();
        Assert.Throws<ArgumentException>(() => policy.ResolveFile(temp.Path, "../outside.csv"));
        Assert.Throws<ArgumentException>(() => policy.ResolveRoot("https://example.test/file.csv"));
        Assert.Throws<ArgumentException>(() => policy.ResolveRoot(@"\\server\share"));
    }

    [Fact] public void Assignment_rejects_duplicates_conflicts_and_changed_hash()
    {
        using var temp = Temp.Create(); var path = Path.Combine(temp.Path, "source.csv"); File.WriteAllText(path, "a\n1\n"); var hash = Hash(path);
        var assignments = new[] { new SourceAssignment("source.csv", RealSourceCategory.StateReference, hash, "v1", null, null, false, "none"), new SourceAssignment("source.csv", RealSourceCategory.RegionReference, hash, "v1", null, null, false, "none") };
        var service = new ResearchSourceManifestValidator(); Assert.Contains(service.Validate(new("1", assignments), temp.Path), x => x.StartsWith("ConflictingAssignment")); File.AppendAllText(path, "2\n"); Assert.Contains(service.Validate(new("1", [assignments[0]]), temp.Path), x => x.StartsWith("ChangedHash"));
    }

    [Fact] public void Zip_traversal_and_expansion_limits_are_blocking()
    {
        using var temp = Temp.Create(); var zipPath = Path.Combine(temp.Path, "bad.zip"); using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create)) { var entry = zip.CreateEntry("../escape.csv"); using var writer = new StreamWriter(entry.Open()); writer.Write("x"); }
        var extractor = new SafeArchiveExtractor(); Assert.Throws<InvalidOperationException>(() => extractor.Extract(temp.Path, "bad.zip", "working/run-1", new(10, 1000)));
        using var other = Temp.Create(); var large = Path.Combine(other.Path, "large.zip"); using (var zip = ZipFile.Open(large, ZipArchiveMode.Create)) { var entry = zip.CreateEntry("x.csv"); using var writer = new StreamWriter(entry.Open()); writer.Write(new string('x', 100)); }
        Assert.Throws<InvalidOperationException>(() => extractor.Extract(other.Path, "large.zip", "working/run-2", new(10, 10)));
    }

    [Fact] public void Source_dry_run_reports_schema_mismatch_without_persistence()
    {
        using var temp = Temp.Create(); var path = Path.Combine(temp.Path, "fda.csv"); File.WriteAllText(path, "Ingredient,Date\nA,2024-01-01\n"); var assignment = new SourceAssignment("fda.csv", RealSourceCategory.FdaFirstGeneric, Hash(path), "fda-v1", null, null, false, "none"); var profile = new SourceSchemaProfile("fda-v1", RealSourceCategory.FdaFirstGeneric, ',', new Dictionary<string, string> { ["ActiveIngredient"] = "Ingredient", ["ApprovalDate"] = "ApprovalDate" }, new HashSet<string> { "ActiveIngredient", "ApprovalDate" });
        var result = new RealSourceDryRunValidator().Validate(temp.Path, assignment, profile); Assert.Contains("MissingRequiredField:ApprovalDate", result.BlockingFindings); Assert.Equal(1, result.ParsedRows);
    }

    [Fact] public void Resume_rejects_changed_hash_and_reconciliation_is_exact()
    {
        var planner = new ControlledImportPlanner(); Assert.Equal(3, planner.PlanBatches(5, 2).Count); var checkpoint = new ImportCheckpoint("run", new string('A', 64), 2, 1, new string('B', 64)); Assert.Throws<InvalidOperationException>(() => planner.ValidateResume(checkpoint, new string('C', 64))); Assert.Throws<InvalidOperationException>(() => planner.Reconcile(new(10, 10, 8, 1, 0, 8, "x"))); Assert.Equal(64, planner.Reconcile(new(10, 10, 8, 1, 1, 8, "x")).Length);
    }

    [Fact] public void Database_preflight_requires_exact_name_ownership_migrations_and_write_gate()
    {
        var service = new ResearchDatabaseSafetyService(); var expected = new[] { "Initial", "AddResearchFreezeFoundation" }; var wrong = service.Evaluate(new("SqlServer", "local", "PreviousGenericLaunch", true, "v", "PreviousGenericLaunch", expected, ["Drug"], null), expected, "YES"); Assert.False(wrong.Safe); Assert.Contains("UnapprovedDatabaseName", wrong.Findings); Assert.Contains("MissingOrInvalidOwnershipMarker", wrong.Findings);
        var gated = service.Evaluate(new("SqlServer", ResearchDatabaseSafetyService.ApprovedServer, ResearchDatabaseSafetyService.ApprovedDatabaseName, true, "v", ResearchDatabaseSafetyService.ApprovedDatabaseName, expected, ["__EFMigrationsHistory", "ResearchDatabaseOwnership"], ResearchDatabaseSafetyService.ProjectId, ResearchDatabaseSafetyService.RepositoryMarker), expected, null); Assert.False(gated.Safe); Assert.False(gated.WriteEnabled); Assert.Contains("ResearchDatabaseWriteGateDisabled", gated.Findings);
        var pending = service.Evaluate(new("SqlServer", ResearchDatabaseSafetyService.ApprovedServer, ResearchDatabaseSafetyService.ApprovedDatabaseName, true, "v", ResearchDatabaseSafetyService.ApprovedDatabaseName, [expected[0]], ["__EFMigrationsHistory", "ResearchDatabaseOwnership"], ResearchDatabaseSafetyService.ProjectId, ResearchDatabaseSafetyService.RepositoryMarker), expected, "YES"); Assert.False(pending.Safe); Assert.Contains("PendingMigrations", pending.Findings);
        var safe = service.Evaluate(new("SqlServer", ResearchDatabaseSafetyService.ApprovedServer, ResearchDatabaseSafetyService.ApprovedDatabaseName, true, "v", ResearchDatabaseSafetyService.ApprovedDatabaseName, expected, ["__EFMigrationsHistory", "ResearchDatabaseOwnership"], ResearchDatabaseSafetyService.ProjectId, ResearchDatabaseSafetyService.RepositoryMarker), expected, "YES"); Assert.True(safe.Safe);
    }

    [Fact] public void Ndc_ambiguity_is_never_blindly_padded()
    {
        var report = new NdcQualityService().Inspect(["12345-6789-0", "1234567890", "12345678901", null, "bad"]); Assert.Equal(1, report.SafelyNormalizedTenDigit); Assert.Equal(1, report.AmbiguousTenDigit); Assert.Equal(1, report.ValidElevenDigit); Assert.Equal(1, report.Missing); Assert.Equal(1, report.Invalid);
    }

    [Fact]
    public void Fda_continuation_fragment_is_explicitly_excluded_and_archival_copy_is_not_canonical()
    {
        using var temp = Temp.Create(); Directory.CreateDirectory(Path.Combine(temp.Path, "fda", "converted")); Directory.CreateDirectory(Path.Combine(temp.Path, "fda", "original"));
        const string csv = "ANDA Number,Generic Name,ANDA Applicant,Brand Name,ANDA Approval Date,ANDA Indication,SourceYear\n218112,\"Dexmedetomidine Injection USP,\",Somerset,Dexmedetomidine,9/24/2024,Sedation,2024\n,400 mcg/4 mL and 1000 mcg/10 mL Multiple-Dose Vials,,,,,2024\n";
        var canonical = Path.Combine(temp.Path, "fda", "converted", "fda_first_generic_2024.csv"); var archival = Path.Combine(temp.Path, "fda", "original", "fda_first_generic_2024.csv"); File.WriteAllText(canonical, csv); File.WriteAllText(archival, csv);
        var assignments = new[] { Assignment(temp.Path, "fda/converted/fda_first_generic_2024.csv", "fda-first-generic-csv-v1"), Assignment(temp.Path, "fda/original/fda_first_generic_2024.csv", "fda-first-generic-original-csv-v1") }; var manifestPath = Path.Combine(temp.Path, "manifest.json"); File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(new ResearchSourceManifest("v1", assignments)));
        var report = new MilestoneNineRealSourceWorkflow().Validate(temp.Path, manifestPath, Path.Combine(temp.Path, "report.json"));
        Assert.Empty(report.BlockingFindings); Assert.Equal(1, report.ExplicitlyExcludedRows); Assert.Equal(2, report.TotalRows); Assert.Equal(1, report.AcceptedRows); Assert.Single(report.CanonicalFdaSources); Assert.Single(report.ProvenanceOnlyFdaSources); Assert.Equal("FdaConvertedContinuationFragmentV1", Assert.Single(report.Files.Single(x => x.CanonicalForImport).Exclusions).RuleCode);
    }

    [Fact]
    public void Legitimate_shaped_fda_record_with_missing_identity_remains_blocking()
    {
        using var temp = Temp.Create(); Directory.CreateDirectory(Path.Combine(temp.Path, "fda", "converted")); var relative = "fda/converted/fda_first_generic_2024.csv"; var path = Path.Combine(temp.Path, relative.Replace('/', Path.DirectorySeparatorChar)); File.WriteAllText(path, "ANDA Number,Generic Name,ANDA Applicant,Brand Name,ANDA Approval Date,ANDA Indication,SourceYear\n,Example Drug,Applicant,Brand,,Indication,2024\n"); var manifestPath = Path.Combine(temp.Path, "manifest.json"); File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(new ResearchSourceManifest("v1", [Assignment(temp.Path, relative, "fda-first-generic-csv-v1")])));
        var report = new MilestoneNineRealSourceWorkflow().Validate(temp.Path, manifestPath, Path.Combine(temp.Path, "report.json")); Assert.Equal(1, report.RejectedRows); Assert.Contains(report.BlockingFindings, x => x.EndsWith("RejectedRowsPresent")); Assert.Empty(report.Files.Single().Exclusions);
    }

    [Fact]
    public void Suppressed_medicaid_blanks_are_accepted_and_ambiguous_ndc_is_unresolved()
    {
        using var temp = Temp.Create(); Directory.CreateDirectory(Path.Combine(temp.Path, "medicaid", "original")); var relative = "medicaid/original/medicaid_2024.csv"; var path = Path.Combine(temp.Path, relative.Replace('/', Path.DirectorySeparatorChar)); File.WriteAllText(path, "Utilization Type,State,NDC,Package Size,Year,Quarter,Suppression Used,Product Name,Number of Prescriptions,Total Amount Reimbursed\nFFSU,CA,1234567890,1,2024,1,true,Example,,\n"); var manifestPath = Path.Combine(temp.Path, "manifest.json"); File.WriteAllText(manifestPath, System.Text.Json.JsonSerializer.Serialize(new ResearchSourceManifest("v1", [Assignment(temp.Path, relative, "medicaid-data-csv-v1", RealSourceCategory.MedicaidUtilization)])));
        var report = new MilestoneNineRealSourceWorkflow().Validate(temp.Path, manifestPath, Path.Combine(temp.Path, "report.json")); Assert.Equal(1, report.AcceptedRows); Assert.Equal(0, report.RejectedRows); Assert.Equal(1, report.Files.Single().NdcQuality!.AmbiguousTenDigit);
    }

    [Fact] public void Missing_prerequisites_stop_9b_and_sanitized_reports_reject_paths()
    {
        var readiness = new MilestoneNineReadinessService().Evaluate(false, false, null, false, false, false); Assert.True(readiness.Phase9ACanRun); Assert.False(readiness.Phase9BCanRun); Assert.Contains("RequiredSourceAssignmentsMissing", readiness.BlockingFindings);
        using var temp = Temp.Create(); Assert.Throws<InvalidOperationException>(() => new SanitizedResearchReportWriter().Write(temp.Path, "bad", new { path = @"C:\private\file.csv" }));
    }

    private static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    private static SourceAssignment Assignment(string root, string relative, string profile, RealSourceCategory category = RealSourceCategory.FdaFirstGeneric) => new(relative, category, Hash(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))), profile, null, null, false, "none");
    private sealed class Temp : IDisposable { private Temp(string path) => Path = path; public string Path { get; } public static Temp Create() { var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pa-m9-{Guid.NewGuid():N}"); Directory.CreateDirectory(path); return new(path); } public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); } }
}
