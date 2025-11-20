using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyVersion;

public class BySubjectAndSequencePathStrategy(IUriNode[] path)
    : BySubjectAndPredicateStrategyBase
{
    private IUriNode[] SequencePath { get; } = path;

    protected override ILiteralNode SearchLiteralNode(IEnumerable<Quad> quads, IUriNode subject)
    {
        var objects = SequencePath.Aggregate(new INode[] { subject },
            (subjects, p) => subjects.SelectMany(
                s => quads.Where(x => 
                    s.Equals(x.Subject) && p.Equals(x.Predicate)).Select(x => x.Object)).ToArray());
        var literalNodes = objects.Where(x => x.NodeType == NodeType.Literal).Cast<ILiteralNode>().ToArray();

        if (literalNodes.Length != 1)
            throw new ArgumentException(
                $"The entity does not contain a unique literal node for subject {subject} and predicate path {string.Join(", ", SequencePath.Select(x => x.ToString()))}.");

        return literalNodes[0];
    }
}