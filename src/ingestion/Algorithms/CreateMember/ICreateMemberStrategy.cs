using LdesServer.Core.Models;
using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.CreateMember;

public interface ICreateMemberStrategy
{
    /// <summary>
    /// Creates a member from the given arguments, transforming the quads as needed. 
    /// </summary>
    /// <param name="quads">The quads defining the member</param>
    /// <param name="memberId">The member ID</param>
    /// <param name="entityId">The entity ID</param>
    /// <param name="createdAt">The creation/ingestion timestamp</param>
    /// <returns>Returns a member with the (transformed) entity.</returns>
    Member CreateMember(IEnumerable<Quad> quads, IUriNode memberId, IUriNode entityId, DateTimeOffset createdAt);
}