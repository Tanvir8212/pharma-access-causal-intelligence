using PharmaAccess.Application.Causal;
using PharmaAccess.Causal;
using PharmaAccess.Application.Research;

namespace PharmaAccess.Worker
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "export-m7-synthetic")
            {
                if (args.Length != 3) { Console.Error.WriteLine("Usage: export-m7-synthetic <output-root> <validation-run-code>"); return 2; }
                var result = new CausalValidationBundleExporter().ExportSynthetic(new(1, args[2], args[1], true, ValidationOverwritePolicy.Replace, "m7-cli"));
                Console.WriteLine($"Exported {result.RowCount} synthetic rows to {Path.Combine(args[1], args[2])}"); return 0;
            }
            if (args.Length > 0 && args[0] == "freeze-m8-synthetic")
            {
                if (args.Length != 5) { Console.Error.WriteLine("Usage: freeze-m8-synthetic <output-root> <freeze-code> <python-lock> <dotnet-packages>"); return 2; }
                var result=new SyntheticResearchFreezeWorkflow().Run(args[1],args[2],File.ReadAllText(args[3]),File.ReadAllText(args[4]));
                new ResearchFreezePackageWriter().Verify(result.ArtifactRoot);
                Console.WriteLine($"Synthetic research freeze {result.FreezeCode} is {result.Status}; reproducibility hash {result.ReproducibilityHash}");return 0;
            }
            if (args.Length > 0 && args[0] == "discover-research-sources")
            {
                if (args.Length != 6 || !Enum.TryParse<RealSourceCategory>(args[2], true, out var category)) { Console.Error.WriteLine("Usage: discover-research-sources <private-root> <category> <recursive:true|false> <audit-output-root> <report-code>"); return 2; }
                if (!bool.TryParse(args[3], out var recursive)) { Console.Error.WriteLine("Recursive must be true or false."); return 2; }
                var result = new ResearchSourceDiscoveryService().Discover(new(args[1], category, recursive, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".csv", ".tsv", ".txt", ".json", ".zip", ".gz" }, 1000, 20L * 1024 * 1024 * 1024, Guid.NewGuid().ToString("N")));
                var report = new SanitizedResearchReportWriter().Write(args[4], args[5], result); Console.WriteLine($"Discovered {result.Files.Count} local files; rejected {result.Rejected.Count}. Report: {Path.GetFileName(report)}. No rows were imported."); return 0;
            }
            if (args.Length > 0 && args[0] == "prepare-m9")
            {
                if (args.Length != 3) { Console.Error.WriteLine("Usage: prepare-m9 <audit-output-root> <report-code>"); return 2; }
                var gitSafe = new ResearchGitSafetyService().FindUnsafeTrackedPaths(Directory.GetCurrentDirectory()).Count == 0;
                var readiness = new MilestoneNineReadinessService().Evaluate(false, false, null, false, gitSafe, false);
                var report = new SanitizedResearchReportWriter().Write(args[1], args[2], new { phase = "9A", phase9ACanRun = readiness.Phase9ACanRun, phase9BCanRun = readiness.Phase9BCanRun, readiness.BlockingFindings, warnings = new[] { "No private file contents were inspected.", "No database write was attempted.", "No predictive training or causal estimation was invoked." } });
                Console.WriteLine($"Milestone 9A readiness report written: {Path.GetFileName(report)}. Phase 9B blocked by {readiness.BlockingFindings.Count} findings."); return 3;
            }
            if (args.Length > 0 && args[0] == "discover-m9-real-sources")
            {
                if (args.Length != 4) { Console.Error.WriteLine("Usage: discover-m9-real-sources <private-root> <private-manifest-json> <sanitized-discovery-json>"); return 2; }
                var manifest = new MilestoneNineRealSourceWorkflow().DiscoverAndWriteManifest(args[1], args[2], args[3]); Console.WriteLine($"Assigned {manifest.Assignments.Count} recognized private files. No source rows were persisted."); return 0;
            }
            if (args.Length > 0 && args[0] == "validate-m9-real-sources")
            {
                if (args.Length != 4) { Console.Error.WriteLine("Usage: validate-m9-real-sources <private-root> <private-manifest-json> <sanitized-validation-json>"); return 2; }
                var report = new MilestoneNineRealSourceWorkflow().Validate(args[1], args[2], args[3]); Console.WriteLine($"Dry run inspected {report.TotalRows} rows; accepted {report.AcceptedRows}; rejected {report.RejectedRows}; blockers {report.BlockingFindings.Count}. No database writes occurred."); return report.BlockingFindings.Count == 0 ? 0 : 4;
            }
            Console.WriteLine("PharmaAccess Worker is ready. No long-running jobs are configured."); return 0;
        }
    }
}
