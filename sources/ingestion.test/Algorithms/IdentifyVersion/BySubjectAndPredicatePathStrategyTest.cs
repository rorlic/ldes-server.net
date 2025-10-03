using AquilaSolutions.LdesServer.Core.Test;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;
using FluentAssertions;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.IdentifyVersion;

public class BySubjectAndPredicatePathStrategyTest
{
    private static readonly BySubjectAndPredicatePathStrategy SystemUnderTest = 
        new(new UriNode(new Uri("http://purl.org/dc/terms/created")));

    private static readonly UriNode Subject = new(new Uri("http://en.wikipedia.org/wiki/Robin_Hood"));
    private const string ExpectedTimestamp = "2025-01-20T11:38:00.000+01:00";
    private const string ExpectedDataType = "http://www.w3.org/2001/XMLSchema#dateTime";
    private static readonly DateTimeOffset Dummy = DateTimeOffset.UtcNow;

    [Fact]
    public void WhenEntityHasNoVersionMatchThenThrowException()
    {
        var action = () =>
        {
            var quads = LoadResource.FromTurtle("Data.EntityWithoutVersionId.ttl");
            return SystemUnderTest.FindOrCreateEntityVersion(quads, Subject, Dummy);
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenEntityHasSingleVersionMatchThenReturnsLiteral()
    {
        var quads = LoadResource.FromTurtle("Data.SingleEntityInDefaultGraph.ttl");
        var result = SystemUnderTest.FindOrCreateEntityVersion(quads, Subject, Dummy);
        result.Value.Should().BeEquivalentTo(ExpectedTimestamp);
        result.DataType.AbsoluteUri.Should().Be(ExpectedDataType);
    }

    [Fact]
    public void WhenEntityHasTimestampVersionThenReturnsLiteral()
    {
        var quads = LoadResource.FromTurtle("Data.EntityWithIso8601StringDataTypeForVersionId.ttl");
        var result = SystemUnderTest.FindOrCreateEntityVersion(quads, Subject, Dummy);
        result.Value.Should().BeEquivalentTo(ExpectedTimestamp);
        result.DataType.AbsoluteUri.Should().Be(ExpectedDataType);
    }

    [Fact]
    public void WhenEntityHasMultipleMatchesThenThrowException()
    {
        var action = () =>
        {
            var quads = LoadResource.FromTurtle("Data.EntityWithMultipleVersionIds.ttl");
            return SystemUnderTest.FindOrCreateEntityVersion(quads, Subject, Dummy);
        };

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenEntityHasWrongDataTypeThenThrowException()
    {
        var action = () =>
        {
            var quads = LoadResource.FromTurtle("Data.EntityWithWrongDataTypeForVersionId.ttl");
            return SystemUnderTest.FindOrCreateEntityVersion(quads, Subject, Dummy);
        };

        action.Should().Throw<ArgumentException>();
    }
}