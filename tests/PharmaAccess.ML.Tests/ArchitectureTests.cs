using System.Linq;
using Xunit;

namespace PharmaAccess.ML.Tests
{
    public sealed class ArchitectureTests
    {
        [Fact]
        public void Ml_depends_on_application_and_not_data()
        {
            var names = typeof(ML.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
            Assert.Contains("PharmaAccess.Application", names);
            Assert.DoesNotContain("PharmaAccess.Data", names);
        }
    }
}
