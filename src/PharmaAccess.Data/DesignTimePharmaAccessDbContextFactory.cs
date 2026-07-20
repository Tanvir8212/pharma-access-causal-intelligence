using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PharmaAccess.Data;

public sealed class DesignTimePharmaAccessDbContextFactory : IDesignTimeDbContextFactory<PharmaAccessDbContext>
{
    public PharmaAccessDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PharmaAccessDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PharmaAccessDesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new PharmaAccessDbContext(options);
    }
}
