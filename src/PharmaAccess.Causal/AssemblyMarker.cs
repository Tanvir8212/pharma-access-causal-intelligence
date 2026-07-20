namespace PharmaAccess.Causal
{
    /// <summary>Identifies the future causal inference boundary.</summary>
    public sealed class AssemblyMarker
    {
        public System.Type ApplicationBoundary => typeof(Application.AssemblyMarker);

        public System.Type DomainBoundary => typeof(Domain.AssemblyMarker);
    }
}
