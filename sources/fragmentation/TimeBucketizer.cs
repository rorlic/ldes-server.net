using System.Data;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using AquilaSolutions.LdesServer.Fragmentation.Models;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace AquilaSolutions.LdesServer.Fragmentation;

// TODO: refactor TimeBucketizer for readability and performance (parallel member processing?) 
public class TimeBucketizer(
    IMemberRepository memberRepository,
    IBucketRepository bucketRepository,
    IPageRepository pageRepository,
    IViewRepository viewRepository,
    ILogger<TimeBucketizer> logger)
{
    public async Task<int?> BucketizeViewAsync(IDbTransaction transaction, View view,
        TimeFragmentation fragmentation, short batchSize)
    {
        logger.LogDebug($"Getting member sets to time bucketize for view {view.Name} ...");
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
                $"Cancelling time bucketization because cannot get default bucket for view '{view.Name}'");
            return null;
        }

        logger.LogDebug($"Getting members to time bucketize for view {view.Name} ...");
        var membersToProcess = (await memberRepository
            .GetMembersByMemberSetsAsync(transaction, memberSets)
            .ConfigureAwait(false)).ToList();

        var memberCount = membersToProcess.Count;
        var parentBucket = defaultBucket;
        var bucketCacheByKey = new Dictionary<string, Bucket>();
        var membersByBucket = new Dictionary<Bucket, List<Member>>()
        {
            { defaultBucket, new List<Member>() }
        };

        logger.LogInformation($"Time bucketizing {memberCount} members for view {view.Name} ...");
        foreach (var member in membersToProcess)
        {
            List<TimeBucketPath> bucketPaths;
            try
            {
                // if any value is not a valid date time offset, the member is not bucketized but put in the unknown
                bucketPaths = fragmentation.TimeBucketPathsFor(member).ToList();
            }
            catch (RdfQueryException e)
            {
                logger.LogWarning($"Entity '{member.EntityId}' contains an invalid date time value: {e.Message}");
                bucketPaths = [];
            }

            if (bucketPaths.Count == 0)
            {
                membersByBucket[defaultBucket].Add(member);
            }
            else
            {
                var path = string.Join("\n", fragmentation.SequencePath.Select(x => x.ToString()));
                var gte = QNames.tree.GreaterThanOrEqualToRelation;
                var lt = QNames.tree.LessThanRelation;

                foreach (var bucketPath in bucketPaths)
                {
                    var last = bucketPath[^1];
                    foreach (var b in bucketPath)
                    {
                        var isLeafBucket = b.Equals(last);
                        var key = b.Key;
                        if (!bucketCacheByKey.ContainsKey(key))
                        {
                            var bucketForKey = await bucketRepository
                                .GetBucketAsync(transaction, view, key)
                                .ConfigureAwait(false);
                            if (bucketForKey is null)
                            {
                                bucketForKey =
                                    await bucketRepository
                                        .CreateBucketAsync(transaction, view, key, isLeafBucket)
                                        .ConfigureAwait(false);
                                if (bucketForKey is null)
                                {
                                    logger.LogWarning(
                                        $"Cancelling time-based bucketization because cannot create bucket '{key}' for view '{view.Name}'");
                                    return null;
                                }

                                var pageName = Guid.NewGuid().ToString();
                                var rootPage =
                                    await pageRepository
                                        .CreateRootPageAsync(transaction, bucketForKey, pageName)
                                        .ConfigureAwait(false);
                                if (rootPage is null)
                                {
                                    logger.LogWarning(
                                        $"Cancelling time-based bucketization because cannot create root page for bucket '{key}' in view '{view.Name}'");
                                    return null;
                                }

                                var parentPage = await pageRepository
                                    .GetRootPageAsync(transaction, parentBucket)
                                    .ConfigureAwait(false);
                                if (parentPage is null)
                                {
                                    logger.LogWarning(
                                        $"Cancelling time-based bucketization because cannot get page for bucket '{key}' in view '{view.Name}'");
                                    return null;
                                }

                                var relations = new PageRelation[]
                                {
                                    new() { Path = path, Type = gte, Value = b.From },
                                    new() { Path = path, Type = lt, Value = b.To },
                                };

                                var added = await pageRepository
                                    .RelatesPagesAsync(transaction, parentPage, rootPage, relations)
                                    .ConfigureAwait(false);
                                if (!added)
                                {
                                    logger.LogWarning(
                                        $"Cancelling time-based bucketization because cannot get add relations for page '{parentPage.Name}' in view '{view.Name}'");
                                    return null;
                                }
                            }

                            bucketCacheByKey.Add(key, bucketForKey);
                        }

                        var bucket = bucketCacheByKey[key];

                        if (isLeafBucket)
                        {
                            if (!membersByBucket.ContainsKey(bucket))
                            {
                                membersByBucket.Add(bucket, new List<Member>());
                            }

                            membersByBucket[bucket].Add(member);
                        }

                        parentBucket = bucket;
                    }
                }
            }
        }

        foreach (var bucket in membersByBucket.Keys)
        {
            var members = membersByBucket[bucket];
            if (members.Count == 0) continue;

            var affected = await bucketRepository
                .BucketizeMembersAsync(transaction, bucket, members)
                .ConfigureAwait(false);
            if (affected == null || affected != members.Count)
            {
                logger.LogWarning(
                    $"Cancelling time-based bucketization because cannot bucketize members for bucket '{bucket.Key}' in view '{view.Name}'");
                return null;
            }
        }

        logger.LogDebug($"Setting last time bucketized member set for view {view.Name} ...");
        var done = await viewRepository
            .UpdateBucketizationStatisticsAsync(transaction, view, memberSets, memberCount)
            .ConfigureAwait(false);
        if (!done)
        {
            logger.LogWarning(
                $"Cancelling time bucketization because cannot update bucketization statistics for view '{view.Name}' has already changed");
            return null;
        }

        logger.LogInformation($"Incrementing time bucketized member count by {memberCount} for view {view.Name} ...");
        return memberCount;
    }
}