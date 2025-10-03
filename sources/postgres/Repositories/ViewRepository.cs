using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Storage.Postgres.Models;
using AquilaSolutions.LdesServer.Storage.Postgres.Queries;
using Dapper;
using Dapper.Transaction;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Repositories;

public class ViewRepository(ILogger<ViewRepository> logger) : IViewRepository
{
    async Task<View?> IViewRepository.CreateViewAsync(IDbTransaction transaction, Collection collection,
        string name, string definition)
    {
        ArgumentNullException.ThrowIfNull(collection as CollectionRecord);
        var collectionId = (collection as CollectionRecord)!.Cid;
        try
        {
            var viewId = await transaction
                .ExecuteScalarAsync<short?>(Sql.View.Create,
                    new { Cid = collectionId, Name = name, Definition = definition })
                .ConfigureAwait(false);
            if (viewId is null) return null;

            var bucketId = await transaction
                .ExecuteScalarAsync<long?>(Sql.Bucket.Create, new { Vid = viewId, Key = null as string })
                .ConfigureAwait(false);
            if (bucketId is null) return null;

            var pageId = await transaction
                .QuerySingleOrDefaultAsync<long?>(Sql.Page.CreatePage,
                    new { Bid = bucketId, Vid = viewId, Name = name, Root = true })
                .ConfigureAwait(false);
            if (pageId is null) return null;

            return new ViewRecord { Cid = collectionId, Vid = viewId.Value, Name = name, Definition = definition };
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot create default view '{name}' of collection with ID '{collectionId}'");
            throw;
        }
    }

    private static async Task<bool> DeleteViewAsync(IDbTransaction transaction, short viewId)
    {
        // await transaction.ExecuteAsync("SET LOCAL statement_timeout = '30000'").ConfigureAwait(false);
        await transaction.ExecuteAsync(Sql.Page.DeleteRelationsByView, new { Vid = viewId }).ConfigureAwait(false);
        await transaction.ExecuteAsync(Sql.Page.DeleteMembersByView, new { Vid = viewId }).ConfigureAwait(false);
        await transaction.ExecuteAsync(Sql.Page.DeleteByView, new { Vid = viewId }).ConfigureAwait(false);
        await transaction.ExecuteAsync(Sql.Bucket.DeleteMembersByView, new { Vid = viewId }).ConfigureAwait(false);
        await transaction.ExecuteAsync(Sql.Bucket.DeleteByView, new { Vid = viewId }).ConfigureAwait(false);
        await transaction.ExecuteAsync(Sql.View.DeleteBucketizationStatsByView, new { Vid = viewId })
            .ConfigureAwait(false);
        var affected = await transaction.ExecuteAsync(Sql.View.DeleteById, new { Vid = viewId }).ConfigureAwait(false);
        return affected == 1;
    }

    internal static async Task<bool> DeleteCollectionViewsAsync(IDbTransaction transaction, short collectionId)
    {
        var views = await transaction
            .QueryAsync<ViewRecord>(Sql.View.GetByCollectionIdForDeletion, new { Cid = collectionId })
            .ConfigureAwait(false);

        var deleted = true;
        foreach (var viewId in views.Select(x => x.Vid))
        {
            deleted &= await DeleteViewAsync(transaction, viewId);
        }

        return deleted;
    }

    async Task<IEnumerable<View>> IViewRepository.GetCollectionViewsAsync(IDbTransaction transaction,
        Collection collection)
    {
        ArgumentNullException.ThrowIfNull(collection as CollectionRecord);
        var collectionId = (collection as CollectionRecord)!.Cid;
        try
        {
            return await transaction
                .QueryAsync<ViewRecord>(Sql.View.GetByCollectionId, new { Cid = collectionId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve views by collection '{name}'", collection.Name);
            throw;
        }
    }

    async Task<View?> IViewRepository.GetCollectionViewAsync(IDbTransaction transaction, Collection collection,
        string viewName)
    {
        ArgumentNullException.ThrowIfNull(collection as CollectionRecord);
        var collectionId = (collection as CollectionRecord)!.Cid;
        try
        {
            return await transaction
                .QuerySingleOrDefaultAsync<ViewRecord>(Sql.View.GetByCollectionIdAndViewName,
                    new { Cid = collectionId, Name = viewName })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                "Cannot retrieve view by view '{viewName}' for collection {name}", viewName, collection.Name);
            throw;
        }
    }

    async Task<View?> IViewRepository.GetViewByBucketAsync(IDbConnection connection, Bucket bucket)
    {
        ArgumentNullException.ThrowIfNull(bucket as BucketRecord);
        var viewId = (bucket as BucketRecord)!.Vid;
        try
        {
            return await connection
                .QuerySingleOrDefaultAsync<ViewRecord>(Sql.View.GetById, new { Vid = viewId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot find view with ID '{viewId}'");
            return null;
        }
    }

    async Task<View?> IViewRepository.GetViewsReadyForBucketizationAsync(IDbTransaction transaction)
    {
        try
        {
            var stats = await transaction
                .QuerySingleOrDefaultAsync<BucketizationStatistics>(Sql.View.GetReadyForBucketization)
                .ConfigureAwait(false);
            if (stats is null) return null;

            var view = await transaction
                .QuerySingleAsync<ViewRecord>(Sql.View.GetById, new { stats.Vid })
                .ConfigureAwait(false);
            view.Statistics = stats;
            return view;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve views ready for bucketization");
            throw;
        }
    }


    async Task<bool> IViewRepository.UpdateBucketizationStatisticsAsync(IDbTransaction transaction, View view,
        IMemberSet[] memberSets, int bucketizedCount)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);

        var viewId = viewRecord.Vid;
        var txnIds = memberSets.Cast<TransactionId>().Select(x => x.Id);
        var lastTxn = txnIds.Max();

        try
        {
            var affected = await transaction
                .ExecuteAsync(Sql.View.UpdateLastBucketizedAndBucketizedCount,
                    new { Vid = viewId, LastTxn = lastTxn, BucketizedCount = bucketizedCount })
                .ConfigureAwait(false);
            return affected == 1;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                $"Cannot update bucketization statistics (set transaction to {lastTxn} and increment bucketized member count with {bucketizedCount}) for view '{view.Name}' with ID {viewId}");
            return false;
        }
    }


    async Task<bool> IViewRepository.DeleteViewAsync(IDbTransaction transaction, Collection collection, string viewName)
    {
        ArgumentNullException.ThrowIfNull(collection as CollectionRecord);
        var collectionId = (collection as CollectionRecord)!.Cid;
        try
        {
            var view = await transaction
                .QuerySingleOrDefaultAsync<ViewRecord>(Sql.View.GetByCollectionIdAndViewName,
                    new { Cid = collectionId, Name = viewName }).ConfigureAwait(false);
            if (view is null) return false;
            
            var viewId = view.Vid;
            await transaction.QueryAsync<BucketizationStatistics>(Sql.View.GetStatsForDeletion, new { Vid = viewId });
            await transaction.QueryAsync<Bucket>(Sql.Bucket.GetByViewForDeletion, new { Vid = viewId });
            return await DeleteViewAsync(transaction, viewId).ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot delete view {viewName} for collection with id '{collectionId}'");
            throw;
        }
    }
}