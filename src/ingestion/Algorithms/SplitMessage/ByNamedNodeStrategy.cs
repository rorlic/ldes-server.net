using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.SplitMessage;

/// <summary>
/// This strategy checks that the store does not contain shared or orphaned blank nodes and
/// splits the message into triples recursively belonging to the same named node.
/// </summary>
/// <returns>A collection of (named) graphs each containing the triples that belong together.</returns>
/// <exception cref="ArgumentException">Throws this exception if shared or orphaned blank nodes are found.</exception>
public class ByNamedNodeStrategy : BySubjectStrategyBase
{
    protected override bool IsEntity(Quad _)
    {
        return true;
    }
}