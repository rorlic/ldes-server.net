using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.SplitMessage;

/// <summary>
/// This strategy checks that the message store contains at most one (default or named) graph
/// and assumes that all triples in that graph belong to the same entity.
/// </summary>
/// <returns>A single (default) graph containing the triples found, or an empty graph collection (if no triples found).</returns>
/// <exception cref="ArgumentException">Throws an ArgumentException if multiple graphs are found.</exception>
public class AsSingleEntityStrategy : ISplitMessageStrategy
{
    private class State
    {
        public string? GraphName { get; set; }
    }

    public object InitializeSplittingContext()
    {
        return new State();
    }

    public string AssignQuadToEntity(Quad quad, object? context)
    {
        var state = context as State;
        ArgumentNullException.ThrowIfNull(state);

        var key = quad.Graph?.ToString() ?? string.Empty; // all quads use the same key
        state.GraphName ??= key;

        if (state.GraphName != key) throw new ArgumentException("Message must contain at most one graph.");
        return key;
    }

    public void FinalizeSplitting(Dictionary<string, List<Quad>?> entityMap, object? context)
    {
        // do nothing
    }
}