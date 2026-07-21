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
            Console.WriteLine("PharmaAccess Worker is ready. No long-running jobs are configured."); return 0;
        }
    }
}
