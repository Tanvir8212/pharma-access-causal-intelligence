using System.Linq;
using Xunit;

namespace PharmaAccess.Domain.Tests
{
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Domain_has_no_framework_or_outer_layer_dependencies()
        {
            var references = typeof(Domain.AssemblyMarker).Assembly.GetReferencedAssemblies();
            var forbiddenPrefixes = new[] { "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Microsoft.ML", "PharmaAccess." };
            Assert.DoesNotContain(references, reference => reference.Name is not null && forbiddenPrefixes.Any(prefix => reference.Name.StartsWith(prefix, System.StringComparison.Ordinal)));
        }
    }
}
