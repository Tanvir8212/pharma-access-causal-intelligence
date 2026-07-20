using PharmaAccess.Domain.Entities;
using PharmaAccess.Domain.ValueObjects;

namespace PharmaAccess.Application.Persistence;

public interface IDatasetVersionRepository
{
    Task<DatasetVersion?> FindByCodeAsync(DatasetVersionCode versionCode, CancellationToken cancellationToken);
    Task AddAsync(DatasetVersion datasetVersion, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
