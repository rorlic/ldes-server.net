using System.Reflection;
using LdesServer.Core.Extensions;
using LdesServer.Core.Models;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;

namespace LdesServer.Core.Test;

public static class LoadResource
{
    public static Stream GetEmbeddedStream(string resourceName, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();
        var assemblyName = assembly.GetName().Name;
        var resource = $"{assemblyName}.Resources.{resourceName}";
        var stream = assembly.GetManifestResourceStream(resource);
        return stream ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
    }

    public static IEnumerable<Quad> FromTurtle(string resourceName, Assembly? assembly = null)
    {
        using var reader = new StreamReader(GetEmbeddedStream(resourceName, assembly));
        var rdfHandler = new CollectQuadsHandler();
        new TurtleParser().Load(rdfHandler, reader);
        return rdfHandler.Quads;
    }

    public static IEnumerable<Quad> FromTrig(string resourceName, Assembly? assembly = null)
    {
        using var reader = new StreamReader(GetEmbeddedStream(resourceName, assembly));
        var rdfHandler = new CollectQuadsHandler();
        new TriGParser().Load(rdfHandler, reader);
        return rdfHandler.Quads;
    }

    private static LdesNode CreateLdesNode(IGraph graph, IEnumerable<IGraph> namedGraphs)
    {
        var members = new List<Member>();
        foreach (var g in namedGraphs)
        {
            var memberId = (g.Name as IUriNode)!;
            var quads = g.Triples.Select(x => new Quad(x, memberId));
            var entityId =
                (graph.GetObjectBySubjectPredicate(memberId, graph.CreateUriNode("dct:isVersionOf")) as IUriNode)!;
            var timestamp =
                (graph.GetObjectBySubjectPredicate(memberId, graph.CreateUriNode("prov:generatedAtTime")) as
                    ILiteralNode)!;
            members.Add(Member.From(quads, memberId, entityId, timestamp.AsValuedNode().AsDateTimeOffset()));
        }

        graph.Retract(graph.GetTriplesWithPredicate(new Uri("https://w3id.org/tree#member")));
        graph.Retract(graph.GetTriplesWithPredicate(new Uri("http://purl.org/dc/terms/isVersionOf")));
        graph.Retract(graph.GetTriplesWithPredicate(new Uri("http://www.w3.org/ns/prov#generatedAtTime")));

        return new LdesNode(graph, members.OrderBy(x => x.CreatedAt), new LdesNode.Info(DateTime.UtcNow, TimeSpan.FromMinutes(1), true));
    }

    public static LdesNode FromTrigAsLdesNode(string resourceName, Assembly? assembly = null)
    {
        using var store = new SimpleTripleStore();
        FromTrig(resourceName, assembly).ToList().ForEach(x => store.Assert(x));

        var defaultGraph = null as IRefNode;
        var g = store.Graphs[defaultGraph].WithStandardPrefixes();

        var namedGraphs = store.Graphs.Where(x => !(x.Name is null));
        return CreateLdesNode(g, namedGraphs);
    }
}