using PharmaAccess.Application.Causal;
using PharmaAccess.Causal;
using PharmaAccess.Application.Research;
using Microsoft.EntityFrameworkCore;
using PharmaAccess.Data;
using PharmaAccess.Data.Research;
using System.Text.Json;

namespace PharmaAccess.Worker
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if(args.Length>0&&args[0]=="finalize-research-dataset")
            {
                if(args.Length!=2){Console.Error.WriteLine("Usage: finalize-research-dataset <command-json>");return 2;}
                var connection=Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");if(string.IsNullOrWhiteSpace(connection))return 2;
                var command=JsonSerializer.Deserialize<FinalizeResearchDatasetCommand>(File.ReadAllText(args[1]),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidOperationException("Finalization command is invalid.");
                var options=new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options;using var db=new PharmaAccessDbContext(options);
                var result=new ResearchDatasetFinalizationService(db).FinalizeAsync(command).GetAwaiter().GetResult();Console.WriteLine(JsonSerializer.Serialize(result));return 0;
            }
            if (args.Length > 0 && args[0] == "run-real-final-analysis")
            {
                if (args.Length != 2) { Console.Error.WriteLine("Usage: run-real-final-analysis <artifact-root>"); return 2; }
                var connection=Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");if(string.IsNullOrWhiteSpace(connection))return 2;
                var result=new RealFinalAnalysisWorkflow().RunAsync(connection,args[1]).GetAwaiter().GetResult();
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{predictive=result.Predictive.Selected?.TestMetrics,threshold=result.Predictive.SelectedThreshold,causal=result.Causal.Estimates,blockers=result.Causal.BlockingFindings}));return result.Causal.BlockingFindings.Count==0?0:5;
            }
            if (args.Length > 0 && args[0] == "run-real-final-causal")
            {
                if(args.Length!=2)return 2;var connection=Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");if(string.IsNullOrWhiteSpace(connection))return 2;
                var result=new RealFinalAnalysisWorkflow().RunCausalOnlyAsync(connection,args[1]).GetAwaiter().GetResult();Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));return result.BlockingFindings.Count==0?0:5;
            }
            if(args.Length>0&&args[0]=="export-real-causal-validation")
            {
                if(args.Length!=2)return 2;var connection=Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");if(string.IsNullOrWhiteSpace(connection))return 2;var count=new RealFinalAnalysisWorkflow().ExportCausalValidationRowsAsync(connection,args[1]).GetAwaiter().GetResult();Console.WriteLine($"Exported {count} governed causal validation rows.");return 0;
            }
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
            if (args.Length > 0 && args[0] is "create-real-protocol" or "submit-real-protocol" or "show-real-protocol")
            {
                var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess"); if (string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("The process-scoped PharmaAccess connection is required."); return 2; }
                var options = new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options; using var db = new PharmaAccessDbContext(options); var service = new ResearchProtocolCommandService(db);
                if (args[0] == "create-real-protocol") { if (args.Length != 2) return 2; var p = service.CreateDraftAsync(args[1]).GetAwaiter().GetResult(); Console.WriteLine($"Created protocol {p.ProtocolCode}/{p.ProtocolVersion} as {p.Status}; it was not submitted or approved."); return 0; }
                if (args.Length != 3) return 2;
                if (args[0] == "submit-real-protocol") { var p = service.SubmitAsync(args[1], args[2]).GetAwaiter().GetResult(); Console.WriteLine($"Submitted protocol {p.ProtocolCode}/{p.ProtocolVersion} as {p.Status}; it was not approved."); return 0; }
                var found = service.GetAsync(args[1], args[2]).GetAwaiter().GetResult(); if (found is null) { Console.Error.WriteLine("Protocol does not exist."); return 4; } Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(found, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })); return 0;
            }
            if (args.Length > 0 && args[0] == "execute-real-import")
            {
                if (args.Length != 12 || !int.TryParse(args[7], out var batchSize) || !bool.TryParse(args[10], out var resume))
                {
                    Console.Error.WriteLine("Usage: execute-real-import <private-root> <manifest> <validation> <protocol-code> <protocol-version> <dataset-version> <batch-size> <correlation-id> <git-commit> <resume:true|false> <resume-audit-id-or-dash>");
                    return 2;
                }
                var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");
                if (string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("The process-scoped PharmaAccess connection is required."); return 2; }
                var options = new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options;
                using var db = new PharmaAccessDbContext(options);
                var completion = new ResearchImportExecutionService(db).ExecuteAsync(args[1], args[2], args[3], args[4], args[5], args[6], batchSize, args[8], args[9], resume, args[11]=="-"?null:args[11], CancellationToken.None).GetAwaiter().GetResult();
                Console.WriteLine($"Import reconciled {completion.Registered} rows and stopped with DatasetVersion non-final. Reconciliation: {completion.ReconciliationHash}");
                return 0;
            }
            if (args.Length > 0 && args[0] == "validate-real-import-initialization")
            {
                if (args.Length != 10) { Console.Error.WriteLine("Usage: validate-real-import-initialization <private-root> <manifest> <validation> <protocol-code> <protocol-version> <dataset-version> <correlation-id> <git-commit> <registered-by>"); return 2; }
                var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");
                if (string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("The process-scoped PharmaAccess connection is required."); return 2; }
                var options = new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options;
                using var db = new PharmaAccessDbContext(options);
                var plan = new ResearchImportExecutionService(db).ValidateInitializationAsync(args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], CancellationToken.None).GetAwaiter().GetResult();
                if (args[9] != "PharmaAccess.Worker/guarded-real-import") { Console.Error.WriteLine("Registered-by identity is not authoritative."); return 2; }
                Console.WriteLine($"Validated initialization payload: VersionCode={plan.Dataset.VersionCode}; sources={plan.Sources.Count}; correlation={plan.Run.CorrelationId}; no records persisted.");
                return 0;
            }
            if (args.Length > 0 && args[0] == "validate-real-import-resume")
            {
                if (args.Length != 11) { Console.Error.WriteLine("Usage: validate-real-import-resume <private-root> <manifest> <validation> <protocol-code> <protocol-version> <dataset-version> <previous-correlation-id> <resume-audit-id> <git-commit> <batch-size>"); return 2; }
                var connection = Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess"); if (string.IsNullOrWhiteSpace(connection)) return 2;
                var options = new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options; using var db = new PharmaAccessDbContext(options);
                var summary = new ResearchImportExecutionService(db).ValidateResumeAsync(args[1],args[2],args[3],args[4],args[5],args[6],args[7],args[8],args[9],CancellationToken.None).GetAwaiter().GetResult();
                Console.WriteLine($"Resume validated: committed={summary.RowsCommitted}; nextSource={summary.NextSource}; nextRow={summary.NextRow}; remaining={summary.Remaining}; previousCorrelation={args[7]}; resumeAudit={args[8]}; batchSize={args[10]}; no records persisted."); return 0;
            }
            if (args.Length > 0 && args[0] == "benchmark-bulk-import")
            {
                var connection=Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");if(string.IsNullOrWhiteSpace(connection))return 2;var results=BulkImportBenchmark.RunAsync(connection,1_000_000,CancellationToken.None).GetAwaiter().GetResult();foreach(var result in results)Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));return results.All(x=>x.Reconciled)?0:5;
            }
            if (args.Length > 0 && args[0] == "import-fda-references")
            {
                if(args.Length!=3){Console.Error.WriteLine("Usage: import-fda-references <repository-root> <snapshot-code>");return 2;}
                var connection=Environment.GetEnvironmentVariable("ConnectionStrings__PharmaAccess");if(string.IsNullOrWhiteSpace(connection))return 2;
                var options=new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer(connection).Options;using var db=new PharmaAccessDbContext(options);
                var result=new FdaReferenceImportService(db).ImportAsync(args[1],args[2],CancellationToken.None).GetAwaiter().GetResult();Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result));return 0;
            }
            Console.WriteLine("PharmaAccess Worker is ready. No long-running jobs are configured."); return 0;
        }
    }
}
