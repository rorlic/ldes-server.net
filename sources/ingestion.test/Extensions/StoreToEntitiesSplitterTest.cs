using AquilaSolutions.LdesServer.Core.Test;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;
using AquilaSolutions.LdesServer.Ingestion.Extensions;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Extensions;

public class StoreToEntitiesSplitterTest
{
    [Fact]
    public void WhenMessageContainsStructuredEntityInDefaultGraphThenSingleEntityIsReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.EntityWithStructure.ttl"));
        using var store = new TripleStore();
        var graph = new Graph();
        new TurtleParser().Load(graph, reader);
        store.Add(graph);

        var result = store.Quads.SplitIntoEntitiesUsing(new ByNamedNodeStrategy()).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenMessageContainsOneHundredStructuredEntitiesThenAllEntitiesAreReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.ManyStructuredEntities.nt"));
        using var store = new TripleStore();
        var graph = new Graph();
        new NTriplesParser().Load(graph, reader);
        store.Add(graph);
        
        var result = store.Quads.SplitIntoEntitiesUsing(new ByNamedNodeStrategy()).ToArray();
        result.Should().HaveCount(100);
    }
}