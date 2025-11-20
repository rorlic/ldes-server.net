using LdesServer.Core.Test;
using LdesServer.Ingestion.Algorithms.SplitMessage;
using LdesServer.Ingestion.Extensions;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace LdesServer.Ingestion.Test.Algorithms.SplitMessage;

public class ByPredicateAndObjectStrategyTest
{
    private static readonly ByPredicateAndObjectStrategy SystemUnderTest = new(
        new UriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
        new UriNode(new Uri("http://xmlns.com/foaf/0.1/Person")));

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
    public void WhenMessageContainsOneMatchedSubjectThenSingleEntityIsReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.SingleEntityInNamedGraph.trig"));
        using var store = new TripleStore();
        new TriGParser().Load(store, reader);

        var result = store.Quads.SplitIntoEntitiesUsing(SystemUnderTest).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenMessageContainsMultipleMatchesForSameSubjectThenThatSubjectIsReturned()
    {
        var sut = new ByPredicateAndObjectStrategy(new UriNode(new Uri("http://purl.org/dc/terms/created")), null);

        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.EntityWithMultipleVersionIds.ttl"));
        using var store = new TripleStore();
        var graph = new Graph();
        new TurtleParser().Load(graph, reader);
        store.Add(graph);

        var result = store.Quads.SplitIntoEntitiesUsing(sut).ToArray();
        result.Should().ContainSingle();
    }

    [Fact]
    public void WhenMessageContainsMultipleMatchesForMultipleSubjectsThenAllEntitiesAreReturned()
    {
        using var reader = new StreamReader(LoadResource.GetEmbeddedStream("Data.MultipleEntitiesInNamedGraphs.trig"));
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
    public void WhenMessageContainsOrphanedBlankNodesThenExceptionIsThrown()
    {
        var action = () =>
        {
            var stream = LoadResource.GetEmbeddedStream("Data.MultipleEntitiesAndOrphanedBlankNodes.ttl");
            using var reader = new StreamReader(stream);
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
    public void WhenMessageContainsSharedBlankNodesThenExceptionIsThrown()
    {
        var action = () =>
        {
            var stream = LoadResource.GetEmbeddedStream("Data.MultipleEntitiesWithSharedBlankNodes.ttl");
            using var reader = new StreamReader(stream);
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