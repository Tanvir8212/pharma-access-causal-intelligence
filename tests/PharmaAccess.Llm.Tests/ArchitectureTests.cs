using System.Linq;
using Xunit;

namespace PharmaAccess.Llm.Tests
{
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Llm_layer_does_not_leak_into_domain()
        {
            var llmReferences = typeof(Llm.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
            var domainReferences = typeof(Domain.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
            Assert.Contains("PharmaAccess.Application", llmReferences);
            Assert.DoesNotContain("PharmaAccess.Llm", domainReferences);
        }
    }
}
