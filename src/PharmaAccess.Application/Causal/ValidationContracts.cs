namespace PharmaAccess.Application.Causal;

public enum ValidationOverwritePolicy { Reject, Replace }

public sealed record ExportCausalValidationBundleCommand(
    long CausalStudyId,
    string ValidationRunCode,
    string OutputDirectory,
    bool IncludeNuisancePredictions,
    ValidationOverwritePolicy OverwritePolicy,
    string CorrelationId);

public sealed record ExportCausalValidationBundleResult(
    string ValidationRunCode,
    IReadOnlyList<string> ExportedPaths,
    int RowCount,
    int TreatedCount,
    int ControlCount,
    IReadOnlyDictionary<string, string> Hashes,
    IReadOnlyList<string> Warnings,
    string ReproducibilityHash);
