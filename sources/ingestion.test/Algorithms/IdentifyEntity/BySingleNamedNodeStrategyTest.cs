using AquilaSolutions.LdesServer.Core.Test;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyEntity;
using FluentAssertions;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.IdentifyEntity;

public class BySingleNamedNodeStrategyTest
{
    private static readonly BySingleNamedNodeStrategy SystemUnderTest = new();

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
}