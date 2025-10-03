using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyMember;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.IdentifyMember;

public class ByEntityIdAndVersionStrategyTest
{
    [Theory]
    [InlineData("http://example.org/id/something", "/", 1, "http://example.org/id/something/1")]
    [InlineData("http://example.org/id/something", "#", 1, "http://example.org/id/something#1")]
    [InlineData("http://example.org/id/something", "?version=", 1, "http://example.org/id/something?version=1")]
    public void IdentifyMemberByEntityIdAndNumericVersion(string entityId, string separator, long version,
        string expected)
    {
        var sut = new ByEntityIdAndVersionStrategy(separator);

        var result =
            sut.FindOrCreateMemberIdentifier([], new UriNode(new Uri(entityId)), new LongNode(version));
        result.Uri.AbsoluteUri.Should().Be(expected);
    }

    [Theory]
    [InlineData("http://example.org/id/something", "/", "2025-01-20T13:47:00.000+10:00",
        "http://example.org/id/something/2025-01-20T13:47:00.000+10:00")]
    [InlineData("http://example.org/id/something", "#", "2025-01-20T13:47:00.000+10:00",
        "http://example.org/id/something#2025-01-20T13:47:00.000+10:00")]
    [InlineData("http://example.org/id/something", "?version=", "2025-01-20T13:47:00.000+10:00",
        "http://example.org/id/something?version=2025-01-20T13:47:00.000+10:00")]
    public void IdentifyMemberByEntityIdAndTimestampVersion(string entityId, string separator, string version,
        string expected)
    {
        var sut = new ByEntityIdAndVersionStrategy(separator);

        var result = sut.FindOrCreateMemberIdentifier([], new UriNode(new Uri(entityId)),
            new LiteralNode(version, new Uri("http://www.w3.org/2001/XMLSchema#dateTime")));
        result.Uri.AbsoluteUri.Should().Be(expected);
    }
}