using AquilaSolutions.LdesServer.Core.Models;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.CreateMember;

/// <summary>
/// Used for ingesting regular (non-version) entities. 
/// </summary>
public class AsIsStrategy : ICreateMemberStrategy
{
    public Member CreateMember(
        IEnumerable<Quad> quads, IUriNode memberId, IUriNode entityId, DateTimeOffset createdAt)
    {
        return Member.From(quads, memberId, entityId, createdAt);
    }
}