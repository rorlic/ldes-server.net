using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyEntity;

/// <summary>
/// This strategy searches for a unique named node to use as an entity identifier by matching the given predicate
/// and object values.
/// </summary>
/// <param name="predicate">The predicate value, e.g. http://www.w3.org/1999/02/22-rdf-syntax-ns#type</param>
/// <param name="objectToMatch">The object URI, e.g. http://xmlns.com/foaf/0.1/Person</param>
/// <returns>The unique named node</returns>
/// <exception cref="ArgumentException">Throws an ArgumentException if no unique named node is found.</exception>
public class ByPredicateAndObjectStrategy(IUriNode predicate, INode objectToMatch) : IIdentifyEntityStrategy
{
    private IUriNode Predicate { get; } = predicate;

    private INode ObjectToMatch { get; } = objectToMatch;

    public IUriNode SearchEntityIdentifier(IEnumerable<Quad> quads)
    {
        var namedNodes = quads
            .Where(x => x.Subject.NodeType == NodeType.Uri && Predicate.Equals(x.Predicate) &&  ObjectToMatch.Equals(x.Object))
            .Select(x => x.Subject).Distinct().ToArray();

        if (namedNodes.Length != 1)
            throw new ArgumentException(
                $"The entity does not contain a unique named node for predicate {Predicate} and object {ObjectToMatch}.");

        return (IUriNode)namedNodes[0];
    }
}