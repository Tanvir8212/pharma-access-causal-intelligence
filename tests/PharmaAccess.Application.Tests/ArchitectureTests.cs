using System.Linq;
using Xunit;

namespace PharmaAccess.Application.Tests
{
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Application_depends_on_domain_but_not_outer_layers()
        {
            var names = typeof(Application.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
            Assert.Contains("PharmaAccess.Domain", names);
            Assert.DoesNotContain("PharmaAccess.Infrastructure", names);
            Assert.DoesNotContain("PharmaAccess.Data", names);
        }
    }
}
