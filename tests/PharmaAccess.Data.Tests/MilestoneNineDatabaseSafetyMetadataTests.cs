using Microsoft.EntityFrameworkCore;
using PharmaAccess.Data;
using PharmaAccess.Data.Entities;
using Xunit;

namespace PharmaAccess.Data.Tests;

public sealed class MilestoneNineDatabaseSafetyMetadataTests
{
    [Fact] public void Ownership_marker_is_unique_and_in_research_schema()
    {
        using var context = new PharmaAccessDbContext(new DbContextOptionsBuilder<PharmaAccessDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=MetadataOnly;Trusted_Connection=True").Options);
        var entity = context.Model.FindEntityType(typeof(ResearchDatabaseOwnershipEntity))!;
        Assert.Equal("research", entity.GetSchema()); Assert.Equal("ResearchDatabaseOwnership", entity.GetTableName());
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique && x.Properties.Single().Name == nameof(ResearchDatabaseOwnershipEntity.ProjectId));
    }
}
