using LdesServer.Core.Extensions;
using LdesServer.Core.Models.Configuration;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Core.InputFormatters;

public class LinkedDataReader(LdesServerConfiguration configuration)
{
    private static readonly IDictionary<string, IStoreReader> QuadReaders = new Dictionary<string, IStoreReader>
    {
        { RdfMimeTypes.NQuads, new NQuadsParser() },
        { RdfMimeTypes.TriG, new TriGParser() },
        { RdfMimeTypes.JsonLd, new JsonLdParser() },
    };

    private static readonly IDictionary<string, IRdfReader> TripleReaders = new Dictionary<string, IRdfReader>
    {
        { RdfMimeTypes.NTriples, new NTriplesParser() },
        { RdfMimeTypes.Turtle, new TurtleParser() },
    };
    
    public static readonly string[] AllMimeTypes = TripleReaders.Keys.Concat(QuadReaders.Keys).ToArray();
    
    public IGraph? ParseGraph(string mimeType, Stream stream)
    {
        var store = ParseStore(mimeType, stream);
        if (store == null) return null;

        var graphs = store.Graphs;
        if (graphs.Count > 1 || graphs.First().Name is not null)
            throw new Exception("No named graphs are allowed in collection or view definition.");
        
        return (graphs.FirstOrDefault() ?? new Graph()).WithBaseUri(configuration.GetBaseUri());
    }
    
    public ITripleStore? ParseStore(string mimeType, Stream stream)
    {
        if (QuadReaders.TryGetValue(mimeType, out var quadsParser))
        {
            using var reader = new StreamReader(stream);
            var store = new SimpleTripleStore();
            if (quadsParser is TriGParser triGParser) 
                triGParser.Load(store, reader, configuration.GetBaseUri());
            else
                quadsParser.Load(store, reader);
            return store;
        }

        if (TripleReaders.TryGetValue(mimeType, out var triplesParser))
        {
            using var reader = new StreamReader(stream);
            var graph = new Graph().WithBaseUri(configuration.GetBaseUri());
            triplesParser.Load(graph, reader);
            
            var store = new SimpleTripleStore();
            store.Add(graph);
            return store;
        }

        return null;
    }
    
}