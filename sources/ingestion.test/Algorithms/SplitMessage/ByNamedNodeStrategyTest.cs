using AquilaSolutions.LdesServer.Core.Test;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;
using AquilaSolutions.LdesServer.Ingestion.Extensions;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.SplitMessage;

public class ByNamedNodeStrategyTest
{
    private static readonly ByNamedNodeStrategy SystemUnderTest = new();

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
    public void WhenMessageContainsOneNamedNodeThenSingleEntityIsReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.SingleEntityInNamedGraph.trig"));
        using var store = new TripleStore();
        new TriGParser().Load(store, reader);

        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenStoreContainsMultipleNamedNodesThenAllEntitiesAreReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesInBothGraphs.trig"));
        using var store = new TripleStore();
        new TriGParser().Load(store, reader);

        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Select(x => x.Select(q => q.Subject.ToString()).Distinct().Single())
            .Should().BeEquivalentTo([
                "http://en.wikipedia.org/wiki/Robin_Hood",
                "http://en.wikipedia.org/wiki/Little_John"
            ]);
    }

    [Fact]
    public void WhenStoreContainsOrphanedBlankNodesThenExceptionIsThrown()
    {
        var action = () =>
        {
            using var reader =
                new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesAndOrphanedBlankNodes.ttl"));
            using var store = new TripleStore();
            var graph = new Graph();
            new TurtleParser().Load(graph, reader);
            store.Add(graph);

            // ReSharper disable once UseDiscardAssignment
            var dummy = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
            Assert.Fail("Should have thrown exception");
        };
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenStoreContainsSharedBlankNodesThenExceptionIsThrown()
    {
        var action = () =>
        {
            using var reader =
                new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesWithSharedBlankNodes.ttl"));
            using var store = new TripleStore();
            var graph = new Graph();
            new TurtleParser().Load(graph, reader);
            store.Add(graph);

            // ReSharper disable once UseDiscardAssignment
            var dummy = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
            Assert.Fail("Should have thrown exception");
        };
        action.Should().Throw<ArgumentException>();
    }
}