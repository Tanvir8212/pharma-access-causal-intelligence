using System.Linq;
using Xunit;

namespace PharmaAccess.Causal.Tests
{
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Causal_layer_is_separate_from_predictive_ml()
        {
            var names = typeof(Causal.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
            Assert.Contains("PharmaAccess.Application", names);
            Assert.Contains("PharmaAccess.Domain", names);
            Assert.DoesNotContain("PharmaAccess.ML", names);
        }
    }
}
