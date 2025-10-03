using AquilaSolutions.LdesServer.Core.Models;

namespace AquilaSolutions.LdesServer.Core.Interfaces;

public interface IBucketRepository
{
    /// <summary>
    /// Creates a bucket with the given key within the given view
    /// NOTE: the key is a string representation of the characteristics of the bucket
    ///       (e.g. a reference, a range, a geospatial value (WKT), ...) 
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <param name="bucketKey">The bucket key or null (for the default bucket)</param>
    /// <param name="isLeafBucket">True if the bucket can contain members, false otherwise</param>
    /// <returns>The newly created bucket or null if the bucket could not be created</returns>
    Task<Bucket?> CreateBucketAsync(IDbTransaction transaction, View view, string? bucketKey, bool isLeafBucket);
    
    /// <summary>
    /// Retrieves a bucket with the given key within the given view
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <param name="bucketKey">The bucket key</param>
    /// <returns>The bucket or null if the bucket could not be found</returns>
    Task<Bucket?> GetBucketAsync(IDbTransaction transaction, View view, string bucketKey);

    /// <summary>
    /// Retrieves the buckets that are ready for pagination (i.e. bucketized members)
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <returns>Set of buckets that can be paginated</returns>
    Task<Bucket?> GetReadyForPaginationAsync(IDbTransaction transaction);

    /// <summary>
    /// Retrieves the default bucket for the given view
    /// NOTE: the default bucket has a null key and is used to either
    ///       contain all members (in case of an event source or a paged view) or
    ///       the members for which no bucket can be calculated (unknown bucket)   
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <returns>The default bucket or null if currently not available (due to concurrency)</returns>
    Task<Bucket?> GetDefaultBucketAsync(IDbTransaction transaction, View view);
    
    /// <summary>
    /// Bucketizes the members given by the set of member sets into the given bucket, i.e.
    /// assigns each member from the member sets to the given bucket
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="bucket">The bucket to associate the members to</param>
    /// <param name="memberSets">The set of members to bucketize</param>
    /// <returns>The number of members that were bucketized</returns>
    Task<int?> BucketizeMembersByMemberSetAsync(IDbTransaction transaction, Bucket bucket, 
        IMemberSet[] memberSets);

    /// <summary>
    /// Bucketizes the given members into the given bucket
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="bucket">The bucket to associate the members to</param>
    /// <param name="members">The members</param>
    /// <returns>The number of members that were bucketized</returns>
    Task<int?> BucketizeMembersAsync(IDbTransaction transaction, Bucket bucket, IEnumerable<Member> members);
}