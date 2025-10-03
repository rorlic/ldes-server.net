using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyMember;

public interface IIdentifyMemberStrategy
{
    IUriNode FindOrCreateMemberIdentifier(IEnumerable<Quad> quads, IUriNode entityId, ILiteralNode version);
}