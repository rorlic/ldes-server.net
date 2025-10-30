using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Storage.Postgres.Models;
using AquilaSolutions.LdesServer.Storage.Postgres.Queries;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Repositories;

public class StatisticsRepository(ILogger<StatisticsRepository> logger) : IStatisticsRepository
{
    async Task<IEnumerable<CollectionStatistics>> IStatisticsRepository.GetCollectionStatisticsAsync(
        IDbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            var commandDefinition =
                new CommandDefinition(Sql.Collection.GetStatistics, cancellationToken: cancellationToken);
            return await connection
                .QueryAsync<CollectionStatisticsRecord>(commandDefinition)
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve collection statistics");
            throw;
        }
    }

    public async Task<IEnumerable<ViewStatistics>> GetViewStatisticsAsync(IDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var commandDefinition =
                new CommandDefinition(Sql.View.GetStatistics, cancellationToken: cancellationToken);
            return await connection
                .QueryAsync<ViewStatisticsRecord>(commandDefinition)
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve view statistics");
            throw;
        }
    }
}