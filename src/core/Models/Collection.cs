using LdesServer.Core.Extensions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Core.Models;

public class Collection
{
    /// <summary>
    /// The collection name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// The collection definition (as text/turtle)
    /// </summary>
    public required string Definition { get; set; }

    /// <summary>
    /// Parses the collection definition into a set of triples
    /// </summary>
    /// <param name="baseUri">The base URI</param>
    /// <returns>A graph containing the set of triples</returns>
    public IGraph ParseDefinition(Uri baseUri)
    {
        var parser = new TurtleParser();
        var g = new Graph().WithBaseUri(baseUri).WithStandardPrefixes();
        StringParser.Parse(g, Definition, parser);
        return g;
    }
}