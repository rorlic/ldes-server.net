using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;

/// <summary>
/// This strategy checks that the store does not contain shared or orphaned blank nodes and splits the message into triples
/// belonging to each subject found by searching for all triples matching the given predicate and, if provided, the object value.
/// </summary>
/// <param name="predicate">The predicate value, e.g. http://example.com/something</param>
/// <param name="objectToMatch">The object URI, e.g. http://xmlns.com/foaf/0.1/Person</param>
/// <returns>A collection of (named) graphs each containing the triples that belong together.</returns>
/// <exception cref="ArgumentException">Throws this exception if shared or orphaned blank nodes are found.</exception>
public class ByPredicateAndObjectStrategy(IUriNode predicate, INode? objectToMatch)
    : BySubjectStrategyBase
{
    private IUriNode Predicate { get; } = predicate;

    private INode? ObjectToMatch { get; } = objectToMatch;

    protected override bool IsEntity(Quad quad)
    {
        return quad.Predicate.Equals(Predicate) && (ObjectToMatch is null || quad.Object.Equals(ObjectToMatch));
    }
}