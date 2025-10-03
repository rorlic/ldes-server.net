using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;

/// <summary>
/// This strategy checks that no triples exist in the default graph and splits the message into the named graphs.
/// </summary>
/// <returns>A collection of (named) graphs each containing the triples that belong together.</returns>
/// <exception cref="ArgumentException">Throws this exception if triples are found in the default graph.</exception>
public class ByNamedGraphStrategy : ISplitMessageStrategy
{
    public object? InitializeSplittingContext()
    {
        return null;
    }

    public string? AssignQuadToEntity(Quad quad, object? context)
    {
        var graphName = quad.Graph;
        if (graphName is null) throw new ArgumentException("No triples are allowed in the default graph.");
        
        return graphName.ToString();
    }

    public void FinalizeSplitting(Dictionary<string, List<Quad>?> entityMap, object? context)
    {
        // do nothing
    }
}