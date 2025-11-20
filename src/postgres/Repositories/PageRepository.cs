using System.Data;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Models;
using LdesServer.Storage.Postgres.Models;
using LdesServer.Storage.Postgres.Queries;
using Dapper.Transaction;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LdesServer.Storage.Postgres.Repositories;

public class PageRepository(ILogger<PageRepository> logger) : IPageRepository
{
    async Task<Page?> IPageRepository.GetOpenPageAsync(IDbTransaction transaction, Bucket bucket)
    {
        var bucketRecord = bucket as BucketRecord;
        ArgumentNullException.ThrowIfNull(bucketRecord);

        var bucketId = bucketRecord.Bid;
        try
        {
            return await transaction
                .QuerySingleOrDefaultAsync<PageRecord?>(Sql.Page.GetOpenPage, new { Bid = bucketId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve open page for bucket with id '{bucketId}'");
            return null;
        }
    }

    public async Task<Page?> GetRootPageAsync(IDbTransaction transaction, Bucket bucket)
    {
        var bucketRecord = bucket as BucketRecord;
        ArgumentNullException.ThrowIfNull(bucketRecord);

        var bucketId = bucketRecord.Bid;
        try
        {
            return await transaction
                .QuerySingleOrDefaultAsync<PageRecord?>(Sql.Page.GetRootPage, new { Bid = bucketId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve root page for bucket with id '{bucketId}'");
            return null;
        }
    }

    async Task<Page?> IPageRepository.CreateRootPageAsync(IDbTransaction transaction, Bucket bucket, string pageName)
    {
        var bucketRecord = bucket as BucketRecord;
        ArgumentNullException.ThrowIfNull(bucketRecord);

        var bucketId = bucketRecord.Bid;
        var viewId = bucketRecord.Vid;
        try
        {
            var pageId = await transaction
                .QuerySingleOrDefaultAsync<long?>(Sql.Page.CreatePage,
                    new { Bid = bucketId, Vid = viewId, Name = pageName, Root = true })
                .ConfigureAwait(false);
            if (pageId is null) return null;

            return new PageRecord
            {
                Pid = pageId.Value, Bid = bucketId, Vid = viewId, Name = pageName, Root = true, Open = true,
                Assigned = 0
            };
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                $"Cannot create root page named '{pageName}' for bucket with id '{bucketId}' and view id '{viewId}'");
            return null;
        }
    }

    async Task<IEnumerable<IMember>> IPageRepository.GetMembersReadyForPaginationAsync(IDbTransaction transaction, Bucket bucket)
    {
        var bucketRecord = bucket as BucketRecord;
        ArgumentNullException.ThrowIfNull(bucketRecord);

        var bucketId = bucketRecord.Bid;
        var lastMid = bucketRecord.LastMid;
        try
        {
            var memberIds = await transaction
                .QueryAsync<long>(Sql.Bucket.GetMembersToPaginate, new { Bid = bucketId, LastMid = lastMid })
                .ConfigureAwait(false);
            return memberIds.Select(x => new MemberId(x)).ToArray();
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve available members for bucket with id '{bucketId}'");
            throw;
        }
    }

    async Task<int?> IPageRepository.PaginateMembersAsync(IDbTransaction transaction, Page page, IMember[] memberIds)
    {
        var pageRecord = page as PageRecord;
        ArgumentNullException.ThrowIfNull(pageRecord);

        var pageId = pageRecord.Pid;
        var bucketId = pageRecord.Bid;
        var viewId = pageRecord.Vid;
        var ids = memberIds.Cast<MemberId>().Select(x => x.Id).ToArray();
        try
        {
            var values = string.Join(",", ids.Select(x => $"({x})"));
            var removeBucketMembersCmd = Sql.Bucket.RemoveBucketMembers.Replace("@Ids", string.Join(",", ids));
            var deleted = await transaction
                .ExecuteAsync(removeBucketMembersCmd, new { Bid = bucketId })
                .ConfigureAwait(false);

            var associateMembersToPageCommand = Sql.Page.AssociateMembersToPage.Replace("@Ids", values);
            var affected = await transaction
                .ExecuteAsync(associateMembersToPageCommand, new { Pid = pageId, Vid = viewId })
                .ConfigureAwait(false);

            if (deleted != affected)
            {
                logger.LogWarning(
                    $"Deleted count {deleted} from bucket '{bucketId}' does not match paginated count {affected} for page '{pageId}'");
                return null;
            }

            return await transaction
                .ExecuteAsync(Sql.Page.UpdateAssignedMemberCount, new { Pid = pageId, Count = affected })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                $"Cannot associate available members to page '{pageId}' ({page.Name}) for bucket with id '{bucketId}'");
            return null;
        }
    }

    async Task<Page?> IPageRepository.ClosePageAndLinkToNewPageAsync(IDbTransaction transaction, Page page,
        string newPageName)
    {
        var pageRecord = page as PageRecord;
        ArgumentNullException.ThrowIfNull(pageRecord);

        var pageId = pageRecord.Pid;
        var bucketId = pageRecord.Bid;
        var viewId = pageRecord.Vid;

        try
        {
            var affected = await transaction
                .ExecuteAsync(Sql.Page.ClosePage, new { Pid = pageId })
                .ConfigureAwait(false);
            if (affected == 0) return null;

            var newPageId = await transaction
                .ExecuteScalarAsync<long?>(Sql.Page.CreatePage,
                    new { Bid = bucketId, Vid = viewId, Name = newPageName, Root = false })
                .ConfigureAwait(false);
            if (newPageId is null) return null;

            var done = await transaction
                .ExecuteAsync(Sql.Page.CreateGenericPageRelation, new { Fid = pageId, Tid = newPageId, Vid = viewId })
                .ConfigureAwait(false) == 1;

            done &= await transaction
                .ExecuteAsync(Sql.Page.UpdateViewTimestamp, new { Fid = pageId })
                .ConfigureAwait(false) == 1;

            if (!done) return null;

            return new PageRecord
            {
                Pid = newPageId.Value, Bid = bucketId, Vid = viewId, Name = newPageName,
                Open = true, Root = false, Assigned = 0
            };
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                $"Cannot close open page '{pageId}' ({page.Name}) for bucket with id '{bucketId}' or create new page '{newPageName}'");
            return null;
        }
    }

    async Task<Page> IPageRepository.GetDefaultBucketRootPageAsync(IDbTransaction transaction, View view)
    {
        var viewRecord = view as ViewRecord;
        ArgumentNullException.ThrowIfNull(viewRecord);

        var viewId = viewRecord.Vid;
        try
        {
            return await transaction
                .QuerySingleAsync<PageRecord>(Sql.Page.GetRootPageByDefaultBucket, new { Vid = viewId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve default view root page by for view with id '{viewId}'");
            throw;
        }
    }

    async Task<Page?> IPageRepository.GetPageAsync(IDbTransaction transaction, Collection collection, string viewName,
        string pageName)
    {
        var collectionRecord = collection as CollectionRecord;
        ArgumentNullException.ThrowIfNull(collectionRecord);

        var collectionId = collectionRecord.Cid;
        try
        {
            return await transaction
                .QuerySingleOrDefaultAsync<PageRecord?>(Sql.Page.GetPageByViewAndPageNames,
                    new { PageName = pageName, ViewName = viewName, Cid = collectionId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve page with name '{pageName}' for view '{viewName}'");
            throw;
        }
    }

    async Task<IEnumerable<PageRelation>> IPageRepository.GetPageRelationsAsync(IDbTransaction transaction, Page page)
    {
        var pageRecord = page as PageRecord;
        ArgumentNullException.ThrowIfNull(pageRecord);

        var pageId = pageRecord.Pid;
        try
        {
            return await transaction
                .QueryAsync<PageRelation>(Sql.Page.GetPageLinks, new { Pid = pageId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve page links by page with id '{pageId}'");
            throw;
        }
    }

    async Task<IEnumerable<Member>> IPageRepository.GetPageMembersAsync(IDbTransaction transaction, Page page)
    {
        var pageRecord = page as PageRecord;
        ArgumentNullException.ThrowIfNull(pageRecord);

        var pageId = pageRecord.Pid;
        try
        {
            return await transaction
                .QueryAsync<Member>(Sql.Member.GetPageMembers, new { Pid = pageId })
                .ConfigureAwait(false);
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception, $"Cannot retrieve page members by page with id '{pageId}'");
            throw;
        }
    }

    async Task<bool> IPageRepository.RelatesPagesAsync(IDbTransaction transaction, Page fromPage, Page toPage,
        PageRelation[] relations)
    {
        var fromPageRecord = fromPage as PageRecord;
        ArgumentNullException.ThrowIfNull(fromPageRecord);

        var toPageRecord = toPage as PageRecord;
        ArgumentNullException.ThrowIfNull(toPageRecord);

        var fid = fromPageRecord.Pid;
        var tid = toPageRecord.Pid;
        var vid = fromPageRecord.Vid;

        var relationRecords = relations.Select(x
            => new { Fid = fid, Tid = tid, Vid = vid, x.Type, x.Path, x.Value });

        try
        {
            var done = await transaction
                .ExecuteAsync(Sql.Page.CreatePageRelation, relationRecords)
                .ConfigureAwait(false) == relations.Length;

            done &= await transaction
                .ExecuteAsync(Sql.Page.UpdateViewTimestamp, new { Fid = fid })
                .ConfigureAwait(false) == 1;

            return done;
        }
        catch (PostgresException exception)
        {
            logger.LogError(exception,
                $"Cannot create relations from page '{fid}' ({fromPage.Name}) to page '{tid}' ({toPage.Name})");
            return false;
        }
    }
}