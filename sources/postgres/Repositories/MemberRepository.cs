using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Storage.Postgres.Models;
using AquilaSolutions.LdesServer.Storage.Postgres.Queries;
using Dapper.Transaction;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AquilaSolutions.LdesServer.Storage.Postgres.Repositories;

public class MemberRepository(ILogger<MemberRepository> logger) : IMemberRepository
{
    internal static Task<bool> DeleteCollectionMembersAsync(IDbTransaction transaction, short collectionId)
    {
        return transaction
            .ExecuteAsync(Sql.Member.DeleteByCollection, new { Cid = collectionId })
            .ContinueWith(_ => true);
    }

    async Task<IEnumerable<string>> IMemberRepository.StoreMembersAsync(IDbTransaction transaction,
        Collection collection, IEnumerable<Member> members)
    {
        var collectionRecord = collection as CollectionRecord;
        ArgumentNullException.ThrowIfNull(collectionRecord);

        var collectionId = collectionRecord.Cid;
        try
        {
            var inserted = 0;
            var low = (long?)null;
            var high = (long?)null;
            var ids = new List<string>();
            foreach (var m in members)
            {
                var mid = await transaction
                    .ExecuteScalarAsync<long?>(Sql.Member.Create, new
                    {
                        Cid = collectionId, m.CreatedAt, m.MemberId, m.EntityId, m.EntityModel
                    })
                    .ConfigureAwait(false);

                if (mid is null)
                {
                    // TODO: check if we need to provide a 'strict' option which fails txn if duplicate detected 
                    logger.LogWarning("A member with id '{Member_Id}' was ignored as it already exists.", m.MemberId);
                }
                else
                {
                    ids.Add(m.MemberId);
                    inserted++;
                    low ??= mid; // first member id is the lowest 
                    high = mid; // always override previous high as member ids value increases
                }
            }

            if (low is not null && high is not null)
            {
                await transaction
                    .ExecuteAsync(Sql.Member.InsertMemberTxnRange, new { Cid = collectionId, Low = low, High = high })
                    .ConfigureAwait(false);
            }

            if (inserted > 0)
            {
                var affected = await transaction
                    .ExecuteAsync(Sql.Collection.UpdateIngestedMemberCount,
                        new { Affected = inserted, Cid = collectionId })
                    .ConfigureAwait(false);
                if (affected == 0)
                {
                    throw new InvalidOperationException("Should not happen");
                }
            }

            return ids;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, "Cannot store members for collection '{Name}'", collection.Name);
            throw;
        }
    }

    async Task<IEnumerable<IMemberSet>> IMemberRepository.GetBucketizableMemberSetsAsync(IDbTransaction transaction,
        View view, short maxMemberCount)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);
        var collectionId = viewRecord.Cid;
        var lastTxn = viewRecord.BucketizationStatistics!.LastTxn;
        
        try
        {
            var transactionsToProcess = await transaction
                .QueryAsync<long>(Sql.Member.GetHighestBatchTransactionForBucketization,
                    new { Cid = collectionId, LastTxn = lastTxn, Count = maxMemberCount })
                .ConfigureAwait(false);
            return transactionsToProcess.Select(x => new TransactionId(x)).ToArray();
        }
        catch (NpgsqlException exception)
        {
            logger.LogError(exception, $"Cannot retrieve member transactions to process for view '{view.Name}'");
            throw;
        }
    }
    
    async Task<IEnumerable<Member>> IMemberRepository.GetMembersByMemberSetsAsync(IDbTransaction transaction, IMemberSet[] memberSets)
    {
        var transactionIds = memberSets.Cast<TransactionId>().Select(x => x.Id);
        var ids = string.Join(",", transactionIds.Select(x => x.ToString()));
        try
        {
            var cmd = Sql.Member.GetMembersByTxnRange.Replace("@TxnIds", ids);
            return await transaction.QueryAsync<MemberRecord>(cmd).ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve page members by member sets '{ids}'");
            throw;
        }
    }

}