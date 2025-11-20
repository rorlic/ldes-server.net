using LdesServer.Core.Extensions;
using LdesServer.Core.Interfaces;
using LdesServer.Core.Namespaces;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Core.Models;

public class View
{
    public const string DefaultName = "_";

    /// <summary>
    /// The view name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// The view definition (as test/turtle)
    /// </summary>
    public string? Definition { get; set; }

    /// <summary>
    /// Parses the view definition into a set of triples
    /// </summary>
    /// <returns>A graph containing the set of triples</returns>
    public IGraph ParseDefinition()
    {
        var parser = new TurtleParser();
        var g = new Graph().WithStandardPrefixes().WithServerPrefixes();
        if (Definition != null) StringParser.Parse(g, Definition, parser);
        return g;
    }
}