using LdesServer.Core.Models;

namespace LdesServer.Core.Interfaces;

public interface IStatisticsRepository
{
    Task<IEnumerable<CollectionStatistics>> GetCollectionStatisticsAsync(IDbConnection connection,
        CancellationToken cancellationToken);

    Task<IEnumerable<ViewStatistics>> GetViewStatisticsAsync(IDbConnection connection,
        CancellationToken cancellationToken);
}