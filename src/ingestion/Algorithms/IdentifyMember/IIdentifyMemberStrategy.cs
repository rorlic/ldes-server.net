using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyMember;

public interface IIdentifyMemberStrategy
{
    IUriNode FindOrCreateMemberIdentifier(IEnumerable<Quad> quads, IUriNode entityId, ILiteralNode version);
}