using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaAccess.Application.Features;
using PharmaAccess.Application.Persistence;

namespace PharmaAccess.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddPharmaAccessData(this IServiceCollection services, string? connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDbContext<PharmaAccessDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("A SQL Server connection string is required when PharmaAccessDbContext is resolved.");
            }

            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(PharmaAccessDbContext).Assembly.FullName));
        });
        services.AddScoped<IDatasetVersionRepository, DatasetVersionRepository>();
        services.AddScoped<IFeatureBuildPersistence, FeatureBuildPersistence>();
        return services;
    }
}
