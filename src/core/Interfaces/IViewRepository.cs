using LdesServer.Core.Models;

namespace LdesServer.Core.Interfaces;

public interface IViewRepository
{
    /// <summary>
    /// Retrieves the views for the given collection
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="collection">The collection</param>
    /// <returns>The set of views for the collection</returns>
    Task<IEnumerable<View>> GetCollectionViewsAsync(IDbTransaction transaction, Collection collection);

    /// <summary>
    /// Retrieves the view with the given name for the given collection
    /// NOTES:
    /// * the view name is unique within a collection, not across collections
    /// * the view name can be the empty string to indicate the event source of the collection
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="collection">The collection</param>
    /// <param name="viewName">View name</param>
    /// <returns>The view with the given name for the given collection or null if not found</returns>
    Task<View?> GetCollectionViewAsync(IDbTransaction transaction, Collection collection, string viewName);

    /// <summary>
    /// Retrieve at most one view that is ready for bucketization (i.e. new members available)
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <returns>View that can be bucketized or null</returns>
    Task<View?> GetViewReadyForBucketizationAsync(IDbTransaction transaction);

    /// <summary>
    /// Retrieve at most one view that is ready for pagination (i.e. new members bucketized)
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <returns>View that can be paginated or null</returns>
    Task<View?> GetViewReadyForPaginationAsync(IDbTransaction transaction);

    /// <summary>
    /// Create a view with the given name and definition for the given collection
    /// In addition, a default bucket and its root page are also created 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="collection">The collection</param>
    /// <param name="name">The view name</param>
    /// <param name="definition">The view definition</param>
    /// <returns>The newly created view or null if the view (or its default bucket and root page) could not be created</returns>
    Task<View?> CreateViewAsync(IDbTransaction transaction, Collection collection, string name, string definition);

    /// <summary>
    /// Deletes the view with the given name for the given collection
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="collection">The collection</param>
    /// <param name="name">The view name</param>
    /// <returns>True if deleted, false otherwise</returns>
    Task<bool> DeleteViewAsync(IDbTransaction transaction, Collection collection, string name);

    /// <summary>
    /// Updates the bucketization statistics for the given view
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <param name="memberSets">The member sets that were bucketized</param>
    /// <param name="bucketizedCount">The number of bucketized members to increment the statistics with</param>
    /// <returns>True if updated, false otherwise</returns>
    Task<bool> UpdateBucketizationStatisticsAsync(IDbTransaction transaction, View view, IMemberSet[] memberSets, int bucketizedCount);

    /// <summary>
    /// Updates the pagination statistics for the given view
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <returns>True if success, false otherwise</returns>
    Task<bool> UpdatePaginationStatisticsAsync(IDbTransaction transaction, View view);
}