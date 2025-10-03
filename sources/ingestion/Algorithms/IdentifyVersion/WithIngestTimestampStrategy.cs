using VDS.RDF;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;

/// <summary>
/// This strategy uses a constant timestamp value as the version identifier for an entity,
/// effectively assigning the same current date & time to all entities ingested together.
/// This allows creating versions for entities that do not have a natural version indicator
/// such as a timestamp or a version number. 
/// </summary>
/// <returns>A literal node representing the time of ingestion.</returns>
public class WithIngestTimestampStrategy : IIdentifyVersionStrategy
{
    public ILiteralNode FindOrCreateEntityVersion(IEnumerable<Quad> quads, IUriNode subject, DateTimeOffset createdAt)
    {
        return new DateTimeNode(createdAt);
    }
}