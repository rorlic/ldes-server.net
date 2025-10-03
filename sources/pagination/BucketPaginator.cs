using System.Data;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Interfaces;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using Microsoft.Extensions.Logging;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Pagination;

// TODO: refactor BucketPaginator for readability and performance
internal class BucketPaginator(
    IBucketRepository bucketRepository,
    IViewRepository viewRepository,
    IPageRepository pageRepository,
    ILogger<BucketPaginator> logger)
{
    internal string WorkerId { get; } = Guid.NewGuid().ToString();

    internal async Task<bool> TryPaginateBucketsAsync(IDbConnection connection, short memberBatchSize, short defaultPageSize)
    {
        using var transaction = connection.BeginTransaction();

        var bucket = await bucketRepository
            .GetReadyForPaginationAsync(transaction)
            .ConfigureAwait(false);

        if (bucket is null)
            return false;

        var bucketKey = bucket.Key ?? "(NULL)";
        logger.LogInformation($"{WorkerId}: Paginating bucket {bucketKey}...");

        var view = await viewRepository
            .GetViewByBucketAsync(connection, bucket)
            .ConfigureAwait(false);
        if (view is null)
        {
            logger.LogWarning($"{WorkerId}: Cannot get view for bucket '{bucketKey}'...");
            return false;
        }

        var viewName = view.Name;
        var bucketReference = $"bucket '{bucketKey}' of view '{viewName}'";

        using var viewDefinition = view.ParseDefinition();
        var pageSize = (short?)viewDefinition.FindOneByQNamePredicate(QNames.tree.pageSize)?
            .Object?.AsValuedNode().AsInteger() ?? defaultPageSize;

        logger.LogInformation(
            $"{WorkerId}: Getting members ready for pagination of {bucketReference} (max {memberBatchSize} members)...");
        var members = (await pageRepository
            .GetMembersReadyForPaginationAsync(transaction, bucket, memberBatchSize)
            .ConfigureAwait(false)).ToArray();
        if (members.Length == 0)
        {
            logger.LogWarning($"{WorkerId}: No members found for {bucketReference}");
            return false;
        }

        logger.LogInformation($"{WorkerId}: Found {members.Length} members for {bucketReference}");
        var processed = await PaginateBucketAsync(transaction, bucket, pageSize, members, bucketReference, viewName)
            .ConfigureAwait(false);

        if (processed is null)
        {
            logger.LogWarning($"{WorkerId}: Bucket {bucketKey} could not be paginated.");
            return false;
        }

        logger.LogInformation($"{WorkerId}: Done paginating bucket {bucketKey} (for now).");
        transaction.Commit();
        return true;
    }

    private async Task<int?> PaginateBucketAsync(IDbTransaction transaction, Bucket bucket, short pageSize,
        IMember[] members, string bucketReference, string viewName)
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

        // update bucket B (if not changed) with last member ID
        logger.LogDebug($"Setting last paginated member for {bucketReference}...");
        var done = await pageRepository
            .SetLastPaginatedMemberAsync(transaction, bucket, members)
            .ConfigureAwait(false);
        
        if (!done)
        {
            logger.LogWarning(
                $"Cancelling pagination because last paginated member id for {bucketReference} has already changed");
            return null; // cannot work on this bucket
        }

        return members.Length;
    }

    private async Task<Page?> FillPageAndCreateNewIfNeededAsync(IDbTransaction transaction, Page openPage,
        IMember[] members, short capacity)
    {
        // fill page OP with member IDs (assign page OP to page members for bucket B and member IDs) 
        var assigned = await pageRepository
            .PaginateMembersAsync(transaction, openPage, members)
            .ConfigureAwait(false);
        
        if (assigned is null)
        {
            logger.LogWarning($"Cancelling pagination because no members were associated with page '{openPage.Name}'");
            return null;
        }

        // if page OP is full (page size == assigned member count + affected count) then close OP, create NP (bid,vid,name) & add relation OP => NP
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