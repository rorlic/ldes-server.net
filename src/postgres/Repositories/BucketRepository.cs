using System.Data;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models;
using LdesServer.Storage.Postgres.Models;
using LdesServer.Storage.Postgres.Queries;
using Dapper.Transaction;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LdesServer.Storage.Postgres.Repositories;

public class BucketRepository(ILogger<BucketRepository> logger) : IBucketRepository
{
    async Task<Bucket?> IBucketRepository.CreateBucketAsync(IDbTransaction transaction, View view,
        string? bucketKey, bool isLeafBucket)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);

        var viewId = viewRecord.Vid;
        try
        {
            var bucketId = await transaction
                .ExecuteScalarAsync<long?>(Sql.Bucket.Create, new { Vid = viewId, Key = bucketKey })
                .ConfigureAwait(false);
            return bucketId is null ? null : new BucketRecord { Bid = bucketId.Value, Vid = viewId, Key = bucketKey };
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                $"Cannot create bucket '{bucketKey ?? "(default)"}' for view with ID '{viewId}'");
            return null;
        }
    }

    async Task<IEnumerable<Bucket>> IBucketRepository.GetReadyForPaginationAsync(IDbTransaction transaction, View view, short maxCount)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);

        var viewId = viewRecord.Vid;
        var stats = viewRecord.PaginationStatistics;
        var lastMid = stats!.FirstMid + maxCount; // approx. batch size
        try
        {
            var buckets = await transaction
                .QueryAsync<BucketRecord>(Sql.Bucket.GetReadyForPaginationByView, 
                    new { Vid = viewId, LastMid = lastMid })
                .ConfigureAwait(false);
            return buckets.Select(x => { x.LastMid = lastMid; return x; });
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot retrieve buckets ready for pagination by view");
            throw;
        }
    }

    async Task<Bucket?> IBucketRepository.GetDefaultBucketAsync(IDbTransaction transaction, View view)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);

        var viewId = viewRecord.Vid;
        try
        {
            return await transaction
                .QuerySingleOrDefaultAsync<BucketRecord>(Sql.Bucket.GetByViewAndDefaultKey, new { Vid = viewId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot get default bucket for view with id '{viewId}'");
            return null;
        }
    }

    async Task<Bucket?> IBucketRepository.GetBucketAsync(IDbTransaction transaction, View view, string key)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);

        var viewId = viewRecord.Vid;
        try
        {
            var bucket = await transaction
                .QuerySingleOrDefaultAsync<BucketRecord?>(Sql.Bucket.GetByViewAndKey, new { Vid = viewId, Key = key })
                .ConfigureAwait(false);

            return bucket;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot get bucket for view with id '{viewId}' and key '{key}'");
            return null;
        }
    }

    async Task<int?> IBucketRepository.BucketizeMembersByMemberSetAsync(IDbTransaction transaction, Bucket bucket,
        IMemberSet[] memberSets)
    {
        var bucketRecord = bucket as BucketRecord;
        ArgumentNullException.ThrowIfNull(bucketRecord);

        var bucketId = bucketRecord.Bid;
        var viewId = bucketRecord.Vid;
        var txnIds = memberSets.Cast<TransactionId>().Select(x => x.Id);

        try
        {
            var cmd = Sql.Bucket.CreateByBucketizableMembers.Replace(
                "@TxnIds", string.Join(",", txnIds.Select(x => x.ToString())));
            return await transaction.ExecuteAsync(cmd, new { Vid = viewId, Bid = bucketId }).ConfigureAwait(false);
        }
        catch (NpgsqlException exception)
        {
            logger.LogError(exception, $"Cannot bucketize available members for view with id '{viewId}'");
            return null;
        }
    }

    async Task<int?> IBucketRepository.BucketizeMembersAsync(IDbTransaction transaction, Bucket bucket,
        IEnumerable<Member> members)
    {
        var bucketRecord = bucket as BucketRecord;
        ArgumentNullException.ThrowIfNull(bucketRecord);

        var bucketId = bucketRecord.Bid;
        var viewId = bucketRecord.Vid;
        var pageMemberRecords = members
            .Select(x => new { Bid = bucketId, Vid = viewId, (x as MemberRecord)!.Mid })
            .ToArray();

        try
        {
            return await transaction
                .ExecuteAsync(Sql.Bucket.BucketizeMember, pageMemberRecords)
                .ConfigureAwait(false);
        }
        catch (NpgsqlException exception)
        {
            logger.LogError(exception,
                $"Cannot bucketize members '{string.Join(",", pageMemberRecords.Select(x => x.Mid))}' for view with id '{viewId}'");
            return null;
        }
    }
}