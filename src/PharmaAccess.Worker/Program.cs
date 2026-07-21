using PharmaAccess.Application.Causal;
using PharmaAccess.Causal;

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
            Console.WriteLine("PharmaAccess Worker is ready. No long-running jobs are configured."); return 0;
        }
    }
}
