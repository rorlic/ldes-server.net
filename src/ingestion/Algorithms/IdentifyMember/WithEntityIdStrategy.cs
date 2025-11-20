using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyMember;

/// <summary>
/// Used for ingesting version entities (i.e. entities which are already versions of some state object). 
/// </summary>
public class WithEntityIdStrategy : IIdentifyMemberStrategy
{
    public IUriNode FindOrCreateMemberIdentifier(IEnumerable<Quad> quads, IUriNode entityId, ILiteralNode version)
    {
        return entityId;
    }
}