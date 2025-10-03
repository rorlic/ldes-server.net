using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Namespaces;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Fetching.Extensions;

internal static class GraphExtensions
{
    public static IGraph WithEventStream(this IGraph g, IUriNode eventStreamUri)
    {
        g.Assert(new Triple(eventStreamUri, g.CreateUriNode(QNames.rdf.type),
            g.CreateUriNode(QNames.ldes.EventStream)));
        return g;
    }

    public static IGraph WithNode(this IGraph g, IUriNode nodeUri)
    {
        g.Assert(new Triple(nodeUri,
            g.CreateUriNode(QNames.rdf.type), g.CreateUriNode(QNames.tree.Node)));
        return g;
    }

    public static IGraph WithEventSource(this IGraph g, IUriNode nodeUri)
    {
        var viewDescription = g.CreateBlankNode();
        var triples = new Triple[]
        {
            new(nodeUri, g.CreateUriNode(QNames.tree.viewDescription), viewDescription),
            new(viewDescription, g.CreateUriNode(QNames.rdf.type), g.CreateUriNode(QNames.ldes.EventSource))
        };

        g.Assert(triples);
        return g;
    }

    public static IGraph WithViewReference(this IGraph g, IUriNode eventStream, IUriNode reference)
    {
        g.WithViewReferences(eventStream, [reference]);
        return g;
    }

    public static IGraph WithViewReferences(this IGraph g, IUriNode eventStream, IEnumerable<IUriNode> references)
    {
        var predicate = g.CreateUriNode(QNames.tree.view);
        var triples = references.Select(x => new Triple(eventStream, predicate, x));
        g.Assert(triples);
        return g;
    }

    public static IGraph WithNodeRelations(this IGraph g, IUriNode nodeUri, string pagePrefix,
        IEnumerable<PageRelation> relations)
    {
        var triples = relations.SelectMany(x =>
        {
            var blankNode = g.CreateBlankNode();
            var relationType = x.Type;
            var relation = new List<Triple>
            {
                new(nodeUri, g.CreateUriNode(QNames.tree.relation), blankNode),
                new(blankNode, g.CreateUriNode(QNames.rdf.type),
                    g.CreateUriNode(relationType ?? QNames.tree.Relation)),
                new(blankNode, g.CreateUriNode(QNames.tree.node),
                    g.CreateUriNode(new Uri($"{pagePrefix}{x.Link}", UriKind.Relative)))
            };
            if (relationType is not null)
            {
                if (x.Value is null || x.Path is null)
                    throw new InvalidOperationException(); // NOTE: cannot be null if relation is constrained

                var predicates = x.Path.Split("\n").Select(p => g.CreateUriNode(new Uri(p))).ToArray();
                var isSequencePath = predicates.Length > 1;
                var pathUri = isSequencePath ? g.AssertList(predicates) : predicates[0];
                relation.Add(new Triple(blankNode, g.CreateUriNode(QNames.tree.path), pathUri));
                relation.Add(new Triple(blankNode, g.CreateUriNode(QNames.tree.value), x.Value.AsLiteralNode(g)));
            }

            return relation;
        });
        g.Assert(triples);
        return g;
    }

    public static IGraph WithoutCustomDefinition(this IGraph g)
    {
        g.NamespaceMap.RemoveNamespace(nameof(Prefix.ingest));
        g.NamespaceMap.RemoveNamespace(nameof(Prefix.lsdn));

        var triples = g.Triples.Where(x => x.Predicate.ToString().StartsWith(Prefix.lsdn)).ToList();
        g.Retract(triples);
        triples.Select(x => x.Object)
            .Where(x => x.NodeType == NodeType.Blank).Cast<IBlankNode>().ToList()
            .ForEach(x => ExcludeSubgraph(g, x));
        
        return g;
    }

    private static void ExcludeSubgraph(IGraph g, IBlankNode node)
    {
        var triples = g.Triples.Where(x => x.Subject.Equals(node)).ToList();
        g.Retract(triples);
        triples.Select(x => x.Object)
            .Where(x => x.NodeType == NodeType.Blank).Cast<IBlankNode>().ToList()
            .ForEach(x => ExcludeSubgraph(g, x));
    }
}