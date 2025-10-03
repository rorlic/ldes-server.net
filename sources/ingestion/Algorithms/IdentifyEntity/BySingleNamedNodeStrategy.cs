using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyEntity;

/// <summary>
/// This strategy searches for a unique named node to use as an entity identifier.
/// </summary>
/// <returns>The unique named node</returns>
/// <exception cref="ArgumentException">Throws an ArgumentException if no unique named node is found.</exception>
public class BySingleNamedNodeStrategy : IIdentifyEntityStrategy
{
    public IUriNode SearchEntityIdentifier(IEnumerable<Quad> quads)
    {
        var namedNodes = quads
            .Where(x => x.Subject.NodeType == NodeType.Uri)
            .Select(x => x.Subject).Distinct().ToArray();

        if (namedNodes.Length != 1)
            throw new ArgumentException("The entity does not contain a unique named node.");

        return (IUriNode)namedNodes[0];
    }
}