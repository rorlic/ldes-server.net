using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;

/// <summary>
/// This strategy searches for a unique timestamp value in the object position for the configured predicate
/// and the (calculated) entity identifier (subject). 
/// </summary>
/// <param name="predicate">The predicate value, e.g. http://example.com/some-property</param>
/// <returns>The literal value found in the object position.</returns>
public class BySubjectAndPredicatePathStrategy(IUriNode predicate) : BySubjectAndPredicateStrategyBase
{
    private IUriNode Predicate { get; } = predicate;

    protected override ILiteralNode SearchLiteralNode(IEnumerable<Quad> quads, IUriNode subject)
    {
        var literalNodes = quads
            .Where(x => x.Object.NodeType == NodeType.Literal && subject.Equals(x.Subject) && Predicate.Equals(x.Predicate))
            .Select(x => x.Object).Cast<ILiteralNode>().ToArray();
        
        if (literalNodes.Length != 1)
            throw new ArgumentException(
                $"The entity does not contain a unique literal node for subject {subject} and predicate {Predicate}.");
        
        return literalNodes[0];
    }
}