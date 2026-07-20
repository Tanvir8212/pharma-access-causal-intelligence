namespace PharmaAccess.Data
{
    /// <summary>Identifies the future SQL Server persistence boundary.</summary>
    public sealed class AssemblyMarker
    {
        public System.Type ApplicationBoundary => typeof(Application.AssemblyMarker);

        public System.Type DomainBoundary => typeof(Domain.AssemblyMarker);
    }
}
