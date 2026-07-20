using System.Linq;
using Xunit;

namespace PharmaAccess.Data.Tests
{
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Data_references_application_and_domain_boundaries()
        {
            var names = typeof(Data.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
            Assert.Contains("PharmaAccess.Application", names);
            Assert.Contains("PharmaAccess.Domain", names);
        }
    }
}
