using Microsoft.EntityFrameworkCore;
using PharmaAccess.Application.Persistence;
using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Data;

internal sealed class DatasetVersionRepository(PharmaAccessDbContext context) : IDatasetVersionRepository
{
    public Task<DatasetVersion?> FindByCodeAsync(DatasetVersionCode versionCode, CancellationToken cancellationToken) =>
        context.DatasetVersions.SingleOrDefaultAsync(item => item.VersionCode == versionCode, cancellationToken);

    public async Task AddAsync(DatasetVersion datasetVersion, CancellationToken cancellationToken) =>
        await context.DatasetVersions.AddAsync(datasetVersion, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
