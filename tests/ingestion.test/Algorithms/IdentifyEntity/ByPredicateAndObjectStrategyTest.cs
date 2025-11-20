using LdesServer.Core.Test;
using LdesServer.Ingestion.Algorithms.IdentifyEntity;
using FluentAssertions;
using VDS.RDF;

namespace LdesServer.Ingestion.Test.Algorithms.IdentifyEntity;

public class ByPredicateAndObjectStrategyTest
{
    private static readonly ByPredicateAndObjectStrategy SystemUnderTest = new(
        new UriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")), 
        new UriNode(new Uri("http://xmlns.com/foaf/0.1/Person"))
    );

    [Fact]
    public void WhenEntityHasNoNamedNodeThenThrowException()
    {
        var action = () =>
        {
            var quads = LoadResource.FromTurtle("Data.EntityWithoutNamedNode.ttl");
            return SystemUnderTest.SearchEntityIdentifier(quads);
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenEntityHasSingleNamedNodeThenReturnsNamedNode()
    {
        var quads = LoadResource.FromTurtle("Data.SingleEntityInDefaultGraph.ttl");
        var result = SystemUnderTest.SearchEntityIdentifier(quads);

        result.Uri.Should().BeEquivalentTo(new Uri("http://en.wikipedia.org/wiki/Robin_Hood"));
    }

    [Fact]
    public void WhenEntityHasMultipleNamedNodesThenThrowException()
    {
        var action = () =>
        {
            var quads = LoadResource.FromTurtle("Data.EntityWithMultipleNamedNodes.ttl");
            return SystemUnderTest.SearchEntityIdentifier(quads);
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenEntityHasNoIdentifierThenThrowException()
    {
        var action = () =>
        {
            var quads = LoadResource.FromTurtle("Data.EntityWithoutNamedNode.ttl");
            return SystemUnderTest.SearchEntityIdentifier(quads);
        };

        action.Should().Throw<ArgumentException>();
    }
}