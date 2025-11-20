using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyEntity;

public interface IIdentifyEntityStrategy
{
    IUriNode SearchEntityIdentifier(IEnumerable<Quad> quads);
}