using AquilaSolutions.LdesServer.Core.Test;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;
using AquilaSolutions.LdesServer.Ingestion.Extensions;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.SplitMessage;

public class AsSingleEntityStrategyTest
{
    private static readonly AsSingleEntityStrategy SystemUnderTest = new();

    [Fact]
    public void WhenEmptyMessageThenNoEntitiesAreReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.Empty.ttl"));
        using var store = new TripleStore();
        var graph = new Graph();
        new TurtleParser().Load(graph, reader);
        store.Add(graph);
        
        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenMessageContainsOneEntityInDefaultGraphThenSingleEntityIsReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.SingleEntityInDefaultGraph.ttl"));
        using var store = new TripleStore();
        var graph = new Graph();
        new TurtleParser().Load(graph, reader);
        store.Add(graph);
        
        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenMessageContainsOneEntityInNamedGraphThenSingleEntityIsReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.SingleEntityInNamedGraph.trig"));
        using var store = new TripleStore();
        new TriGParser().Load(store, reader);
        
        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenMessageContainsMultipleGraphsThenExceptionIsThrown()
    {
        var action = () =>
        {
            using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesInBothGraphs.trig"));
            using var store = new TripleStore();
            new TriGParser().Load(store, reader);
            
            // ReSharper disable once UseDiscardAssignment
            var dummy = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
            Assert.Fail("Should have thrown exception");
        };
        action.Should().Throw<ArgumentException>();
    }
}