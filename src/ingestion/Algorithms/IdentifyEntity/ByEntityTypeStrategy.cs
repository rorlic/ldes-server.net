using LdesServer.Core.Namespaces;
using VDS.RDF;

namespace LdesServer.Ingestion.Algorithms.IdentifyEntity;

/// <summary>
/// This strategy searches for a unique named node to use as an entity identifier by matching the given object value
/// for the RDF type (&lt;http://www.w3.org/1999/02/22-rdf-syntax-ns#type&gt; predicate).
/// </summary>
/// <param name="objectToMatch">The object URI, e.g. http://xmlns.com/foaf/0.1/Person</param>
/// <returns>The unique named node</returns>
/// <exception cref="ArgumentException">Throws an ArgumentException if no unique named node is found.</exception>
// ReSharper disable once UnusedType.Global
public class ByEntityTypeStrategy(INode objectToMatch) : ByPredicateAndObjectStrategy(
    new UriNode(new Uri($"{Prefix.rdf}type")), objectToMatch);