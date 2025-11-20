using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyVersion;

public interface IIdentifyVersionStrategy
{
    ILiteralNode FindOrCreateEntityVersion(IEnumerable<Quad> quads, IUriNode subject, DateTimeOffset createdAt);
}