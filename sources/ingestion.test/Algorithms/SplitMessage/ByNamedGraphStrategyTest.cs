using AquilaSolutions.LdesServer.Core.Test;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.SplitMessage;
using AquilaSolutions.LdesServer.Ingestion.Extensions;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.SplitMessage;

public class ByNamedGraphStrategyTest
{
    private static readonly ByNamedGraphStrategy SystemUnderTest = new();

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
    public void WhenMessageContainsOneNamedGraphThenSingleEntityIsReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.SingleEntityInNamedGraph.trig"));
        using var store = new TripleStore();
        new TriGParser().Load(store, reader);

        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenMessageContainsMultipleNamedGraphsThenAllEntitiesAreReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesInNamedGraphs.trig"));
        using var store = new TripleStore();
        new TriGParser().Load(store, reader);

        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Select(x => x.Select(q => q.Graph!.ToString()).Distinct().Single())
            .Should().BeEquivalentTo([
                "http://en.wikipedia.org/wiki/Robin_Hood",
                "http://en.wikipedia.org/wiki/Merry_Men"
            ]);
    }

    [Fact]
    public void WhenMessageContainsDefaultGraphThenExceptionIsThrown()
    {
        var action = () =>
        {
            using var reader =
                new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesInBothGraphs.trig"));
            using var store = new TripleStore();
            new TriGParser().Load(store, reader);

            // ReSharper disable once UseDiscardAssignment
            var dummy = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
            Assert.Fail("Should have thrown exception");
        };
        action.Should().Throw<ArgumentException>();
    }
}