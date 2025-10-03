using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;

public interface IIdentifyVersionStrategy
{
    ILiteralNode FindOrCreateEntityVersion(IEnumerable<Quad> quads, IUriNode subject, DateTimeOffset createdAt);
}