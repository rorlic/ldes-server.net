using System.Data;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using Microsoft.Extensions.Logging;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Pagination;

// TODO: refactor BucketPaginator for readability and performance
public class BucketPaginator(
    IBucketRepository bucketRepository,
    IViewRepository viewRepository,
    IPageRepository pageRepository,
    ILogger<BucketPaginator> logger)
{
    internal string WorkerId { get; } = Guid.NewGuid().ToString();

    internal async Task<bool> TryPaginateBucketsAsync(IDbConnection connection, short memberBatchSize, short defaultPageSize)
    {
        using var transaction = connection.BeginTransaction();

        logger.LogDebug($"{WorkerId}: Getting view ready for pagination ...");
        var view = await viewRepository
            .GetViewReadyForPaginationAsync(transaction)
            .ConfigureAwait(false);

        if (view is null)
        {
            logger.LogDebug($"{WorkerId}: No views to paginate.");
            return false;
        }

        var viewName = view.Name;
        logger.LogInformation($"{WorkerId}: Paginating view {viewName} ...");

        logger.LogDebug(
            $"{WorkerId}: Getting buckets ready for pagination for view {viewName} (limited to approximate max {memberBatchSize} members)...");
        var buckets = await bucketRepository
            .GetReadyForPaginationAsync(transaction, view, memberBatchSize)
            .ConfigureAwait(false);

        foreach (var bucket in buckets)
        {
            var bucketKey = bucket.Key ?? "(NULL)";
            var bucketReference = $"bucket '{bucketKey}' of view '{viewName}'";
            logger.LogDebug($"{WorkerId}: Paginating {bucketReference}...");

            using var viewDefinition = view.ParseDefinition();
            var pageSize = (short?)viewDefinition.FindOneByQNamePredicate(QNames.tree.pageSize)?
                .Object?.AsValuedNode().AsInteger() ?? defaultPageSize;

            logger.LogDebug($"{WorkerId}: Getting members ready for pagination of {bucketReference}...");
            var members = (await pageRepository
                .GetMembersReadyForPaginationAsync(transaction, bucket)
                .ConfigureAwait(false)).ToArray();

            logger.LogDebug($"{WorkerId}: Found {members.Length} members for {bucketReference}");
            var processed = await PaginateBucketAsync(transaction, bucket, pageSize, members, bucketReference)
                .ConfigureAwait(false);

            if (processed is null)
            {
                logger.LogWarning($"{WorkerId}: Bucket {bucketKey} could not be paginated.");
                return false;
            }

            logger.LogDebug($"{WorkerId}: Done paginating bucket {bucketKey} (for now).");
        }
        
        logger.LogDebug($"Updating paginated count for view {viewName}...");
        var done = await viewRepository
            .UpdatePaginationStatisticsAsync(transaction, view)
            .ConfigureAwait(false);
        
        if (!done)
        {
            logger.LogWarning($"Cancelling pagination because cannot update statistics for view {viewName}");
            return false;
        }
        
        transaction.Commit();
        logger.LogInformation($"{WorkerId}: Done paginating view {view.Name} for now.");
        return true;
    }

    private async Task<int?> PaginateBucketAsync(IDbTransaction transaction, Bucket bucket, short pageSize,
        IMember[] members, string bucketReference)
    {
        // get open page OP for bucket B, incl. assigned member count
        logger.LogDebug($"Getting current open page of {bucketReference}...");
        var openPage = await pageRepository
            .GetOpenPageAsync(transaction, bucket)
            .ConfigureAwait(false);
        
        if (openPage is null)
        {
            logger.LogWarning(
                $"Cancelling pagination because cannot get open page for {bucketReference}");
            return null; // cannot work on this bucket
        }

        // fill first page
        logger.LogDebug($"Fill open page of {bucketReference}...");
        var openPageCapacity = pageSize - openPage.Assigned;
        var batch = members.Take(openPageCapacity).ToArray();
        openPage = await FillPageAndCreateNewIfNeededAsync(transaction, openPage, batch, pageSize)
            .ConfigureAwait(false);
        
        if (openPage is null)
        {
            logger.LogWarning(
                $"Cancelling pagination because cannot get fill first page or create new page for {bucketReference}");
            return null;
        }

        // fill more pages while there are unassigned members
        var pageIndex = 0;
        while (true)
        {
            batch = members.Skip(pageIndex * pageSize + openPageCapacity).Take(pageSize).ToArray();
            if (batch.Length == 0) break;

            logger.LogDebug($"Fill next page of {bucketReference}...");
            openPage = await FillPageAndCreateNewIfNeededAsync(transaction, openPage, batch, pageSize)
                .ConfigureAwait(false);
            
            if (openPage is null)
            {
                logger.LogWarning(
                    $"Cancelling pagination because cannot get fill subsequent page or create new page for {bucketReference}");
                return null;
            }

            pageIndex++;
        }

        return members.Length;
    }

    private async Task<Page?> FillPageAndCreateNewIfNeededAsync(IDbTransaction transaction, Page openPage,
        IMember[] members, short capacity)
    {
        if (members.Length == 0) return openPage;
        
        // fill page OP with member IDs (assign page OP to page members for bucket B and member IDs) 
        var assigned = await pageRepository
            .PaginateMembersAsync(transaction, openPage, members)
            .ConfigureAwait(false);
        
        if (assigned is null)
        {
            logger.LogWarning($"Cancelling pagination because no members were associated with page '{openPage.Name}'");
            return null;
        }

        // if page OP is full (page size == assigned member count plus affected count), then close OP, create NP (bid,vid,name) & add relation OP => NP
        if (openPage.Assigned + members.Length == capacity)
        {
            var newPageName = Guid.NewGuid().ToString();
            var newPage = await pageRepository
                .ClosePageAndLinkToNewPageAsync(transaction, openPage, newPageName)
                .ConfigureAwait(false);
            return newPage;
        }

        return openPage;
    }
}