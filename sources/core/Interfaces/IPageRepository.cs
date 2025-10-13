using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Core.Interfaces;

public interface IPageRepository
{
    /// <summary>
    /// Retrieves the page with the given name 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="collection">The collection</param>
    /// <param name="viewName">The view name</param>
    /// <param name="pageName">The page name</param>
    /// <returns>The page with the given name or null if not found</returns>
    Task<Page?> GetPageAsync(IDbTransaction transaction, Collection collection, string viewName, string pageName);

    /// <summary>
    /// Retrieves the outgoing relations for the given page 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="page">The page</param>
    /// <returns>A set of page relations</returns>
    Task<IEnumerable<PageRelation>> GetPageRelationsAsync(IDbTransaction transaction, Page page);

    /// <summary>
    /// Retrieves the set of members contained in the given page
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="page">The page</param>
    /// <returns>The set of members in the page</returns>
    Task<IEnumerable<Member>> GetPageMembersAsync(IDbTransaction transaction, Page page);

    /// <summary>
    /// Creates a root page with the given page name for the given bucket
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="bucket">The bucket</param>
    /// <param name="pageName">The page name</param>
    /// <returns>The newly created root page or null if the page could not be created</returns>
    Task<Page?> CreateRootPageAsync(IDbTransaction transaction, Bucket bucket, string pageName);

    /// <summary>
    /// Retrieves the root page for the default bucket of the given view
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <returns>The root page</returns>
    Task<Page> GetDefaultBucketRootPageAsync(IDbTransaction transaction, View view);

    /// <summary>
    /// Retrieves the members that are assigned to the given bucket and can be paginated
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="bucket">The bucket</param>
    /// <returns>Returns the identifiers of the members ready for pagination under the given bucket</returns>
    Task<IEnumerable<IMember>> GetMembersReadyForPaginationAsync(IDbTransaction transaction, Bucket bucket);

    /// <summary>
    /// Assign the given members to the given page
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="page">The poge</param>
    /// <param name="members">The members</param>
    /// <returns>
    ///  The number of members assigned to the given page or null is the members could not be assigned
    ///  (e.g. because some other paginatation process has assigned the members to the page)
    /// </returns>
    Task<int?> PaginateMembersAsync(IDbTransaction transaction, Page page, IMember[] members);

    /// <summary>
    /// Retrieves the root (i.e. first) page for the given bucket 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="bucket">The bucket</param>
    /// <returns>The root page or null if that page is currently not available (due to concurrency)</returns>
    Task<Page?> GetRootPageAsync(IDbTransaction transaction, Bucket bucket);
    
    /// <summary>
    /// Retrieves the open (i.e. last) page for the given bucket 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="bucket">The bucket</param>
    /// <returns>The open page or null if that page is currently not available (due to concurrency)</returns>
    Task<Page?> GetOpenPageAsync(IDbTransaction transaction, Bucket bucket);
    
    /// <summary>
    /// Marks the given page as closed (i.e. not open), creates a new page with the given name and
    /// relates the closed page to the newly created open page 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="page">The page to close</param>
    /// <param name="newPageName">The name of the new page</param>
    /// <returns>The newly created page or null if the page could not be closed or
    /// the new page could not be created</returns>
    Task<Page?> ClosePageAndLinkToNewPageAsync(IDbTransaction transaction, Page page, string newPageName);

    /// <summary>
    /// Adds the given page relations to the page
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="fromPage">The origin page</param>
    /// <param name="toPage">The destination page</param>
    /// <param name="relations">The relations</param>
    /// <returns>True if success, false otherwise</returns>
    Task<bool> RelatesPagesAsync(IDbTransaction transaction, Page fromPage, Page toPage, PageRelation[] relations);
}