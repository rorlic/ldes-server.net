using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;

public interface ISplitMessageStrategy
{
    object? InitializeSplittingContext();

    string? AssignQuadToEntity(Quad quad, object? context);
    
    void FinalizeSplitting(Dictionary<string, List<Quad>?> entityMap, object? context);
}