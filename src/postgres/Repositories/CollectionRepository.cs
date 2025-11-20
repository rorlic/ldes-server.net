using System.Data;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models;
using LdesServer.Storage.Postgres.Models;
using LdesServer.Storage.Postgres.Queries;
using Dapper.Transaction;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LdesServer.Storage.Postgres.Repositories;

public class CollectionRepository(ILogger<CollectionRepository> logger) : ICollectionRepository
{
    async Task<Collection?> ICollectionRepository.CreateCollectionAsync(
        IDbTransaction transaction, string name, string definition)
    {
        try
        {
            var cid = await transaction
                .ExecuteScalarAsync<short?>(Sql.Collection.Create,
                    new { Name = name, Definition = definition })
                .ConfigureAwait(false);
            return cid is null
                ? null
                : new CollectionRecord { Cid = cid.Value, Name = name, Definition = definition };
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot add collection '{name}'", name);
            throw;
        }
    }

    private async Task<bool> DeleteCollectionAsync(IDbTransaction transaction, short collectionId)
    {
        try
        {
            var affected = await transaction
                .ExecuteAsync(Sql.Collection.DeleteById, new { Cid = collectionId })
                .ConfigureAwait(false);
            return affected == 1;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot delete collection with id '{collectionId}'");
            throw;
        }
    }

    private Task<bool> TruncateTablesAsync(IDbTransaction transaction)
    {
        try
        {
            return transaction.ExecuteAsync(Sql.Collection.DeleteAll).ContinueWith(_ => true);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot truncate all tables");
            throw;
        }
    }

    async Task<bool> ICollectionRepository.DeleteCollectionAsync(IDbTransaction transaction, string name)
    {
        try
        {
            var collections = (await transaction
                .QueryAsync<CollectionRecord>(Sql.Collection.GetAll).ConfigureAwait(false)).ToArray();

            var collectionId = collections.SingleOrDefault(x => x.Name == name)?.Cid;
            if (collectionId == null) return false;

            var cid = collectionId.Value;
            return collections.Length == 1
                ? await TruncateTablesAsync(transaction).ConfigureAwait(false)
                : await ViewRepository.DeleteCollectionViewsAsync(transaction, cid).ConfigureAwait(false) &&
                  await MemberRepository.DeleteCollectionMembersAsync(transaction, cid).ConfigureAwait(false) &&
                  await DeleteCollectionAsync(transaction, cid).ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot delete collection named '{name}'");
            throw;
        }
    }

    async Task<IEnumerable<Collection>> ICollectionRepository.GetCollectionsAsync(IDbTransaction transaction)
    {
        try
        {
            var collections =
                (await transaction.QueryAsync<CollectionRecord>(Sql.Collection.GetAllDefinitions).ConfigureAwait(false)).ToList();
            return collections;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve collections");
            throw;
        }
    }

    async Task<Collection?> ICollectionRepository.GetCollectionAsync(IDbTransaction transaction,
        string name)
    {
        try
        {
            return await transaction
                .QuerySingleOrDefaultAsync<CollectionRecord?>(Sql.Collection.GetByName, new { Name = name })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve collection '{name}'", name);
            throw;
        }
    }
}