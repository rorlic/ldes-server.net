using System.Data;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models;
using Microsoft.Extensions.Logging;

namespace LdesServer.Fragmentation;

public class DefaultBucketizer(
    IMemberRepository memberRepository,
    IBucketRepository bucketRepository,
    IViewRepository viewRepository,
    ILogger<DefaultBucketizer> logger)
{
    public async Task<int?> BucketizeViewAsync(IDbTransaction transaction, View view, short batchSize)
    {
        logger.LogDebug($"Getting member sets to default bucketize for view {view.Name} ...");
        var memberSets = (await memberRepository
            .GetBucketizableMemberSetsAsync(transaction, view, batchSize)
            .ConfigureAwait(false)).ToArray();
        if (memberSets.Length == 0) return 0;

        logger.LogDebug($"Getting default bucket of view {view.Name} ...");
        var defaultBucket = await bucketRepository
            .GetDefaultBucketAsync(transaction, view)
            .ConfigureAwait(false);
        if (defaultBucket is null)
        {
            logger.LogWarning(
                $"Cancelling default bucketization because cannot get default bucket for view '{view.Name}'");
            return null;
        }

        logger.LogDebug($"Default bucketizing {memberSets.Length} member sets for view {view.Name} ...");
        var memberCount = await bucketRepository
            .BucketizeMembersByMemberSetAsync(transaction, defaultBucket, memberSets)
            .ConfigureAwait(false);
        if (memberCount is null or 0)
        {
            logger.LogWarning(
                $"Cancelling default bucketization because no members were bucketized for view '{view.Name}'");
            return null;
        }

        logger.LogDebug($"Setting last default bucketized member set for view {view.Name} ...");
        var done = await viewRepository
            .UpdateBucketizationStatisticsAsync(transaction, view, memberSets, memberCount.Value)
            .ConfigureAwait(false);
        if (!done)
        {
            logger.LogWarning(
                $"Cancelling time bucketization because cannot update bucketization statistics for view '{view.Name}' has already changed");
            return null;
        }

        logger.LogDebug($"Incrementing default bucketized member count by {memberCount.Value} for view {view.Name} ...");
        return memberCount;
    }
}