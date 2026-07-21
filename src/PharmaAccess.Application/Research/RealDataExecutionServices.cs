using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PharmaAccess.Domain.Research;

namespace PharmaAccess.Application.Research;

public enum RealSourceCategory { FdaFirstGeneric, MedicaidUtilization, StateReference, RegionReference, StateAdjacency, RxNormMapping, ManualMapping }
public sealed record PrivateRootOptions(string Root, bool AllowNetworkPaths = false);
public sealed record SourceDiscoveryRequest(string Root, RealSourceCategory Category, bool Recursive, IReadOnlySet<string> AllowedExtensions, int MaximumFileCount, long MaximumTotalBytes, string CorrelationId);
public sealed record DiscoveredSource(string RelativePath, long ByteSize, string Extension, RealSourceCategory CandidateType, string Sha256, string HashStatus, IReadOnlyList<string> SchemaProfileCandidates);
public sealed record RejectedSource(string DisplayPath, string Reason);
public sealed record SourceDiscoveryResult(string CorrelationId, IReadOnlyList<DiscoveredSource> Files, IReadOnlyList<string> Warnings, IReadOnlyList<RejectedSource> Rejected);
public sealed record SourceAssignment(string RelativePath, RealSourceCategory Category, string Sha256, string SchemaProfileVersion, DateOnly? CoverageStart, DateOnly? CoverageEnd, bool Compressed, string ExtractionPolicy);
public sealed record ResearchSourceManifest(string Version, IReadOnlyList<SourceAssignment> Assignments);

public sealed class PrivatePathPolicy
{
    public string ResolveRoot(string suppliedRoot, bool allowNetworkPaths = false)
    {
        if (string.IsNullOrWhiteSpace(suppliedRoot) || Uri.TryCreate(suppliedRoot, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "ftp") throw new ArgumentException("A local private root is required.");
        if (!allowNetworkPaths && suppliedRoot.StartsWith(@"\\", StringComparison.Ordinal)) throw new ArgumentException("Network paths are disabled.");
        var root = Path.GetFullPath(suppliedRoot); if (!Directory.Exists(root)) throw new DirectoryNotFoundException("Configured private root does not exist.");
        RejectReparsePoint(root); return root;
    }
    public string ResolveFile(string root, string relativePath)
    {
        if (Path.IsPathRooted(relativePath) || Uri.TryCreate(relativePath, UriKind.Absolute, out _)) throw new ArgumentException("Manifest paths must be relative.");
        var canonicalRoot = ResolveRoot(root); var file = Path.GetFullPath(Path.Combine(canonicalRoot, relativePath));
        if (!file.StartsWith(canonicalRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Private-root escape is prohibited.");
        var cursor = new FileInfo(file).Directory; while (cursor is not null && cursor.FullName.StartsWith(canonicalRoot, StringComparison.OrdinalIgnoreCase)) { RejectReparsePoint(cursor.FullName); cursor = cursor.Parent; }
        if (!File.Exists(file)) throw new FileNotFoundException("Assigned private file does not exist.", Path.GetFileName(file)); return file;
    }
    private static void RejectReparsePoint(string path) { if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Symbolic-link or junction paths are prohibited."); }
}

public sealed class ResearchSourceDiscoveryService
{
    private readonly PrivatePathPolicy paths = new();
    public SourceDiscoveryResult Discover(SourceDiscoveryRequest request)
    {
        if (request.MaximumFileCount is <= 0 or > 10000 || request.MaximumTotalBytes <= 0 || string.IsNullOrWhiteSpace(request.CorrelationId)) throw new ArgumentException("Bounded discovery settings and correlation ID are required.");
        var root = paths.ResolveRoot(request.Root); var files = new List<DiscoveredSource>(); var rejected = new List<RejectedSource>(); long total = 0;
        foreach (var path in Directory.EnumerateFiles(root, "*", request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
        {
            var relative = Path.GetRelativePath(root, path); try { var safe = paths.ResolveFile(root, relative); var extension = Path.GetExtension(safe).ToLowerInvariant(); if (!request.AllowedExtensions.Contains(extension)) { rejected.Add(new(relative, "ExtensionNotAllowed")); continue; } var info = new FileInfo(safe); total = checked(total + info.Length); if (files.Count + 1 > request.MaximumFileCount || total > request.MaximumTotalBytes) throw new InvalidOperationException("Discovery limits exceeded."); using var stream = File.OpenRead(safe); var hash = Convert.ToHexString(SHA256.HashData(stream)); files.Add(new(relative.Replace('\\', '/'), info.Length, extension, request.Category, hash, "Calculated", SchemaCandidates(extension, request.Category))); } catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException) { rejected.Add(new(relative, e.GetType().Name)); }
        }
        return new(request.CorrelationId, files, files.Count == 0 ? ["No matching files discovered."] : [], rejected);
    }
    private static string[] SchemaCandidates(string extension, RealSourceCategory category) => [$"{category.ToString().ToLowerInvariant()}-{extension.TrimStart('.')}-v1"];
}

public sealed class ResearchSourceManifestValidator
{
    private readonly PrivatePathPolicy paths = new();
    public IReadOnlyList<string> Validate(ResearchSourceManifest manifest, string root)
    {
        var findings = new List<string>(); var resolved = new Dictionary<string, RealSourceCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in manifest.Assignments)
        {
            string file; try { file = paths.ResolveFile(root, item.RelativePath); } catch (Exception e) { findings.Add($"RejectedPath:{item.RelativePath}:{e.GetType().Name}"); continue; }
            if (resolved.TryGetValue(file, out var existing)) findings.Add(existing == item.Category ? $"DuplicateAssignment:{item.RelativePath}" : $"ConflictingAssignment:{item.RelativePath}"); else resolved[file] = item.Category;
            using var stream = File.OpenRead(file); var actual = Convert.ToHexString(SHA256.HashData(stream)); if (!string.Equals(actual, item.Sha256, StringComparison.OrdinalIgnoreCase)) findings.Add($"ChangedHash:{item.RelativePath}");
            if (string.IsNullOrWhiteSpace(item.SchemaProfileVersion)) findings.Add($"MissingSchemaProfile:{item.RelativePath}");
        }
        return findings;
    }
}

public sealed record ArchiveExtractionLimits(int MaximumFiles, long MaximumExpandedBytes);
public sealed record ExtractedArchiveFile(string RelativePath, long Bytes, string Sha256);
public sealed class SafeArchiveExtractor
{
    private readonly PrivatePathPolicy paths = new();
    public IReadOnlyList<ExtractedArchiveFile> Extract(string privateRoot, string archiveRelativePath, string workingRelativeRoot, ArchiveExtractionLimits limits)
    {
        if (limits.MaximumFiles <= 0 || limits.MaximumExpandedBytes <= 0) throw new ArgumentException("Positive archive limits are required.");
        var archive = paths.ResolveFile(privateRoot, archiveRelativePath); var root = paths.ResolveRoot(privateRoot); var target = Path.GetFullPath(Path.Combine(root, workingRelativeRoot));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || Directory.Exists(target)) throw new InvalidOperationException("Extraction target must be a new private working directory.");
        Directory.CreateDirectory(target); try { return Path.GetExtension(archive).ToLowerInvariant() switch { ".zip" => ExtractZip(archive, target, limits), ".gz" => ExtractGzip(archive, target, limits), _ => throw new NotSupportedException("Only ZIP and GZIP are supported.") }; } catch { Directory.Delete(target, true); throw; }
    }
    private static IReadOnlyList<ExtractedArchiveFile> ExtractZip(string archive, string target, ArchiveExtractionLimits limits)
    {
        using var zip = ZipFile.OpenRead(archive); if (zip.Entries.Count > limits.MaximumFiles) throw new InvalidOperationException("Archive file-count limit exceeded."); var result = new List<ExtractedArchiveFile>(); long total = 0;
        foreach (var entry in zip.Entries.Where(x => !string.IsNullOrEmpty(x.Name))) { if (entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || entry.FullName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Nested archives are prohibited."); total = checked(total + entry.Length); if (total > limits.MaximumExpandedBytes) throw new InvalidOperationException("Archive expanded-size limit exceeded."); var output = Path.GetFullPath(Path.Combine(target, entry.FullName)); if (!output.StartsWith(target + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Archive traversal is prohibited."); Directory.CreateDirectory(Path.GetDirectoryName(output)!); using var input = entry.Open(); using var destination = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None); input.CopyTo(destination); result.Add(Describe(target, output)); }
        return result;
    }
    private static IReadOnlyList<ExtractedArchiveFile> ExtractGzip(string archive, string target, ArchiveExtractionLimits limits)
    {
        var name = Path.GetFileNameWithoutExtension(archive); var output = Path.Combine(target, name); using var input = new GZipStream(File.OpenRead(archive), CompressionMode.Decompress); using var destination = new FileStream(output, FileMode.CreateNew); var buffer = new byte[81920]; long total = 0; int read; while ((read = input.Read(buffer)) > 0) { total += read; if (total > limits.MaximumExpandedBytes) throw new InvalidOperationException("Archive expanded-size limit exceeded."); destination.Write(buffer, 0, read); } destination.Flush(); return [Describe(target, output)];
    }
    private static ExtractedArchiveFile Describe(string root, string file) { using var stream = File.OpenRead(file); return new(Path.GetRelativePath(root, file).Replace('\\', '/'), new FileInfo(file).Length, Convert.ToHexString(SHA256.HashData(stream))); }
}

public sealed record SourceSchemaProfile(string Version, RealSourceCategory Category, char Delimiter, IReadOnlyDictionary<string, string> LogicalToHeader, IReadOnlySet<string> RequiredLogicalFields);
public sealed record SourceDryRunResult(string RelativePath, string Sha256, string Encoding, string Delimiter, IReadOnlyList<string> Headers, IReadOnlyDictionary<string, string> MappedFields, long ParsedRows, long RejectedRows, IReadOnlyList<string> BlockingFindings);
public sealed class RealSourceDryRunValidator
{
    private readonly PrivatePathPolicy paths = new();
    public SourceDryRunResult Validate(string root, SourceAssignment assignment, SourceSchemaProfile profile, int maximumErrors = 100)
    {
        var file = paths.ResolveFile(root, assignment.RelativePath); if (assignment.Compressed) throw new InvalidOperationException("Validate extracted files with preserved archive lineage."); using var hashStream = File.OpenRead(file); var hash = Convert.ToHexString(SHA256.HashData(hashStream)); var blocking = new List<string>(); if (!hash.Equals(assignment.Sha256, StringComparison.OrdinalIgnoreCase)) blocking.Add("ChangedSourceHash");
        using var reader = new StreamReader(file, new UTF8Encoding(false, true), true); var headerLine = reader.ReadLine(); if (headerLine is null) return new(assignment.RelativePath, hash, reader.CurrentEncoding.WebName, profile.Delimiter.ToString(), [], new Dictionary<string, string>(), 0, 0, ["EmptyFile"]); var headers = headerLine.Split(profile.Delimiter).Select(x => x.Trim()).ToArray(); var mapped = profile.LogicalToHeader.Where(x => headers.Contains(x.Value, StringComparer.OrdinalIgnoreCase)).ToDictionary(); foreach (var required in profile.RequiredLogicalFields) if (!mapped.ContainsKey(required)) blocking.Add($"MissingRequiredField:{required}"); long parsed = 0, rejected = 0; string? line; while ((line = reader.ReadLine()) is not null) { if (line.Split(profile.Delimiter).Length != headers.Length) { rejected++; if (rejected > maximumErrors) { blocking.Add("MaximumErrorsExceeded"); break; } } else parsed++; } return new(assignment.RelativePath, hash, reader.CurrentEncoding.WebName, profile.Delimiter.ToString(), headers, mapped, parsed, rejected, blocking);
    }
}

public sealed record NdcQualityReport(long ValidElevenDigit, long SafelyNormalizedTenDigit, long AlreadyNormalized, long AmbiguousTenDigit, long Invalid, long Missing, long DistinctOriginal, long DistinctNormalized, long MappingSuccess, long MappingFailure);
public sealed class NdcQualityService
{
    public NdcQualityReport Inspect(IEnumerable<string?> values)
    {
        long valid = 0, safe = 0, normalized = 0, ambiguous = 0, invalid = 0, missing = 0; var originals = new HashSet<string>(); var outputs = new HashSet<string>();
        foreach (var raw in values) { if (string.IsNullOrWhiteSpace(raw)) { missing++; continue; } var value = raw.Trim(); originals.Add(value); var digits = new string(value.Where(char.IsDigit).ToArray()); if (digits.Length == 11) { valid++; normalized++; outputs.Add(digits); } else if (digits.Length == 10 && value.Contains('-') && TryNormalizeSegmented(value, out var result)) { safe++; outputs.Add(result); } else if (digits.Length == 10) ambiguous++; else invalid++; }
        return new(valid, safe, normalized, ambiguous, invalid, missing, originals.Count, outputs.Count, outputs.Count, originals.Count - outputs.Count);
    }
    private static bool TryNormalizeSegmented(string value, out string result) { result = ""; var p = value.Split('-'); if (p.Length != 3 || p.Any(x => !x.All(char.IsDigit))) return false; result = p.Select((x, i) => (p[0].Length, p[1].Length, p[2].Length) switch { (4, 4, 2) when i == 0 => x.PadLeft(5, '0'), (5, 3, 2) when i == 1 => x.PadLeft(4, '0'), (5, 4, 1) when i == 2 => x.PadLeft(2, '0'), _ => x }).Aggregate(string.Concat); return result.Length == 11; }
}

public sealed record ImportCheckpoint(string RunId, string SourceSha256, long LastCompletedRow, int LastCompletedBatch, string AggregateHash);
public sealed record ImportReconciliation(long RegisteredRows, long RawRows, long AcceptedRows, long RejectedRows, long DuplicateRows, long DatabaseRows, string DeterministicAggregate)
{
    public bool Passes => RegisteredRows == AcceptedRows + RejectedRows + DuplicateRows && RawRows == RegisteredRows && DatabaseRows == AcceptedRows;
}
public sealed class ControlledImportPlanner
{
    public IReadOnlyList<(long Start, long End)> PlanBatches(long rows, int batchSize) { if (rows < 0 || batchSize <= 0) throw new ArgumentOutOfRangeException(); var result = new List<(long, long)>(); for (long start = 1; start <= rows; start += batchSize) result.Add((start, Math.Min(rows, start + batchSize - 1))); return result; }
    public void ValidateResume(ImportCheckpoint checkpoint, string currentHash) { if (!checkpoint.SourceSha256.Equals(currentHash, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Source hash changed after checkpoint; resume is prohibited."); }
    public string Reconcile(ImportReconciliation value) { if (!value.Passes) throw new InvalidOperationException("Import reconciliation mismatch is blocking."); return ReconciliationHasher.Hash(value.RegisteredRows, value.RawRows, value.AcceptedRows, value.RejectedRows, value.DuplicateRows, value.DatabaseRows); }
}

public sealed record ResearchDatabaseProbe(string Provider, string SanitizedServer, string DatabaseName, bool Connected, string? ServerVersion, string? CurrentDatabase, IReadOnlyList<string> AppliedMigrations, IReadOnlyList<string> UserTables, string? OwnershipProjectId, string? OwnershipRepositoryMarker = null);
public sealed record ResearchDatabasePreflight(bool Safe, bool WriteEnabled, IReadOnlyList<string> MissingMigrations, IReadOnlyList<string> UnexpectedMigrations, IReadOnlyList<string> Findings);
public sealed class ResearchDatabaseSafetyService
{
    public const string ProjectId = "PharmaAccessCausalIntelligence";
    public const string RepositoryMarker = "pharma-access-causal-intelligence";
    public const string ApprovedServer = ".";
    public const string ApprovedDatabaseName = "PharmaAccessCausalIntelligence_ResearchDev";
    public ResearchDatabasePreflight Evaluate(ResearchDatabaseProbe probe, IReadOnlyList<string> expectedMigrations, string? writeGate)
    {
        var findings = new List<string>(); if (!probe.Connected) findings.Add("DatabaseConnectionUnavailable"); if (!probe.Provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)) findings.Add("UnexpectedDatabaseProvider"); if (!probe.SanitizedServer.Equals(ApprovedServer, StringComparison.Ordinal)) findings.Add("UnapprovedDatabaseServer"); if (!probe.DatabaseName.Equals(ApprovedDatabaseName, StringComparison.Ordinal)) findings.Add("UnapprovedDatabaseName"); if (!string.Equals(probe.CurrentDatabase, probe.DatabaseName, StringComparison.Ordinal)) findings.Add("CurrentDatabaseMismatch");
        var missing = expectedMigrations.Except(probe.AppliedMigrations, StringComparer.Ordinal).ToArray(); var unexpected = probe.AppliedMigrations.Except(expectedMigrations, StringComparer.Ordinal).ToArray(); if (missing.Length != 0) findings.Add("PendingMigrations"); if (unexpected.Length != 0) findings.Add("UnexpectedMigrationHistory"); if (probe.OwnershipProjectId != ProjectId || probe.OwnershipRepositoryMarker != RepositoryMarker) findings.Add("MissingOrInvalidOwnershipMarker"); var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "__EFMigrationsHistory", "ResearchDatabaseOwnership" }; if (probe.OwnershipProjectId is null && probe.UserTables.Except(allowed).Any()) findings.Add("ForeignTablesBeforeInitialization"); var enabled = writeGate == "YES"; if (!enabled) findings.Add("ResearchDatabaseWriteGateDisabled"); return new(findings.Count == 0, enabled, missing, unexpected, findings);
    }
}

public sealed record MilestoneNineReadiness(bool Phase9ACanRun, bool Phase9BCanRun, IReadOnlyList<string> BlockingFindings, IReadOnlyList<string> Warnings);
public sealed class MilestoneNineReadinessService
{
    public MilestoneNineReadiness Evaluate(bool requiredSourcesAssigned, bool hashesAndSchemasValid, ResearchDatabasePreflight? database, bool protocolApproved, bool gitSafe, bool repositoryClean)
    {
        var blockers = new List<string>(); if (!requiredSourcesAssigned) blockers.Add("RequiredSourceAssignmentsMissing"); if (!hashesAndSchemasValid) blockers.Add("SourceDryRunNotApproved"); if (database is null || !database.Safe) blockers.Add("DedicatedResearchDatabaseNotReady"); if (!protocolApproved) blockers.Add("ApprovedRealResearchProtocolMissing"); if (!gitSafe) blockers.Add("GitDataSafetyFailed"); if (!repositoryClean) blockers.Add("RepositoryDirtyWithoutApprovedException"); return new(true, blockers.Count == 0, blockers, []);
    }
}

public sealed class SanitizedResearchReportWriter
{
    public string Write(string outputRoot, string code, object aggregate)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Any(x => !char.IsLetterOrDigit(x) && x is not '-' and not '_')) throw new ArgumentException("Unsafe report code."); var root = Path.GetFullPath(outputRoot); var file = Path.GetFullPath(Path.Combine(root, $"{code}.json")); if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Report path escape."); Directory.CreateDirectory(root); var json = JsonSerializer.Serialize(aggregate, new JsonSerializerOptions { WriteIndented = true }); if (json.Contains(":\\", StringComparison.Ordinal) || json.Contains("ConnectionString", StringComparison.OrdinalIgnoreCase) || json.Contains("Password", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Sanitized report contains prohibited metadata."); File.WriteAllText(file, json, new UTF8Encoding(false)); return file;
    }
}
