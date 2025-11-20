using LdesServer.Core.Models;

namespace LdesServer.Core.Interfaces;

public interface IMemberRepository
{
    /// <summary>
    /// Stores the set of members into the given collection
    /// NOTE: currently, members with a member id that already exists are ignored
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="collection">The collection</param>
    /// <param name="members">The members</param>
    /// <returns>The set of member ids that were ingested, i.e. the excluding any duplicate members</returns>
    Task<IEnumerable<string>> StoreMembersAsync(IDbTransaction transaction, Collection collection,
        IEnumerable<Member> members);

    /// <summary>
    /// Retrieves a number of member sets that are ready for bucketization for the given view
    /// up to the given maximum member count
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="view">The view</param>
    /// <param name="maxMemberCount">Maximum amount of member to bucketize at once (batch size)</param>
    /// <returns>The set of member set ids for the members that can be bucketized</returns>
    Task<IEnumerable<IMemberSet>> GetBucketizableMemberSetsAsync(IDbTransaction transaction, View view,
        short maxMemberCount);

    /// <summary>
    /// Retrieves the members from the given member sets
    /// </summary>
    /// <param name="transaction">Database transaction</param>
    /// <param name="memberSets">The member sets</param>
    /// <returns>A set of members found in the given member sets</returns>
    Task<IEnumerable<Member>> GetMembersByMemberSetsAsync(IDbTransaction transaction, IMemberSet[] memberSets);
}