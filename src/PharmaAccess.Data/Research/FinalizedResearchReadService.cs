using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.Research;
using PharmaAccess.Domain.Research;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Data.Research;

public sealed class FinalizedResearchReadService(PharmaAccessDbContext db) : IFinalizedResearchReadService
{
    public async Task<FinalizedDatasetSnapshot> GetAsync(CancellationToken ct=default)
    {
        var d=await db.DatasetVersions.AsNoTracking().SingleAsync(x=>x.VersionCode==new DatasetVersionCode("real-2021-2025-v1"),ct);
        var launches=await db.Database.SqlQueryRaw<PartitionCount>("SELECT Partition AS [Name],COUNT(*) AS [Count] FROM research.AndaLaunch WHERE EligibilityCategory IN ('A','B') GROUP BY Partition").ToListAsync(ct);
        int C(string name)=>launches.Single(x=>x.Name==name).Count;
        return new(d.VersionCode.Value,d.Status.ToString(),d.ValidationStatus.ToString(),d.TotalRows??0,d.FinalizedAtUtc,launches.Sum(x=>x.Count),C("Training"),C("Validation"),C("LockedTest"));
    }
    private sealed class PartitionCount { public string Name { get; set; }=null!; public int Count { get; set; } }
}
