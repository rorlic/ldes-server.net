using LdesServer.Core.Models;
using LdesServer.Core.Test;
using LdesServer.Ingestion.Algorithms.CreateMember;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace LdesServer.Ingestion.Test.Algorithms.TransformMember;

public class MaterializeEntityStrategyTest
{
    private static readonly WithEntityMaterializationStrategy SystemUnderTest = new(null);

    [Fact]
    public void WhenIsNotVersionEntityThenExceptionIsThrown()
    {
        var action = () =>
        {
            const string dateTime = "2025-01-20T11:38:00.000+01:00";
            var entityId = new UriNode(new Uri("http://en.wikipedia.org/wiki/Robin_Hood"));
            var memberId = new UriNode(new Uri($"http://en.wikipedia.org/wiki/Robin_Hood#{dateTime}"));
            var quads = LoadResource.FromTurtle("Data.SingleEntityInDefaultGraph.ttl");
            var timestamp = DateTimeOffset.UtcNow;
            
            return SystemUnderTest.CreateMember(quads, memberId, entityId, timestamp);
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenIsVersionEntityThenReturnsMaterializedEntity()
    {
        const string dateTime = "2025-01-20T11:38:00.000+01:00";
        var state = LoadResource.FromTurtle("Data.SingleEntityInDefaultGraph.ttl");
        var stateId = new UriNode(new Uri("http://en.wikipedia.org/wiki/Robin_Hood"));
        var version = LoadResource.FromTurtle("Data.SingleVersionEntityInDefaultGraph.ttl");
        var versionId = new UriNode(new Uri($"http://en.wikipedia.org/wiki/Robin_Hood#{dateTime}"));
        var timestamp = DateTimeOffset.UtcNow;

        var result = SystemUnderTest.CreateMember(version, versionId, versionId, timestamp);

        var expected = Member.From(state, versionId, stateId, timestamp);
        result.Should().BeEquivalentTo(expected);
    }
}