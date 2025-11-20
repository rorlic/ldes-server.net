using LdesServer.Core.Test;
using LdesServer.Ingestion.Algorithms.IdentifyVersion;
using FluentAssertions;
using VDS.RDF;

namespace LdesServer.Ingestion.Test.Algorithms.IdentifyVersion;

public class BySubjectAndSequencePathStrategyTest
{
    private static readonly BySubjectAndSequencePathStrategy SystemUnderTest = new([
        new UriNode(new Uri("http://def.isotc211.org/iso19156/2011/Observation#OM_Observation.phenomenonTime")),
        new UriNode(new Uri("http://www.w3.org/2006/time#hasBeginning")),
        new UriNode(new Uri("http://www.w3.org/2006/time#inXSDDateTimeStamp"))
    ]);

    private static readonly UriNode Subject =
        new(new Uri(
            "https://verkeerscentrum.be/id/verkeersmetingen/observatie/00076HR1/2023-09-27T00:15:00/count/medium/2025-02-15T11:02:29.312619345"));

    private const string ExpectedTimestamp = "2023-09-27T00:15:00";
    private const string ExpectedDataType = "http://www.w3.org/2001/XMLSchema#dateTime";
    private static readonly DateTimeOffset Dummy = DateTimeOffset.UtcNow;

    [Fact]
    public void WhenEntityHasSingleVersionMatchThenReturnsLiteral()
    {
        var quads = LoadResource.FromTurtle("Data.EntityWithStructure.ttl");
        var result = SystemUnderTest.FindOrCreateEntityVersion(quads, Subject, Dummy);
        result.Value.Should().BeEquivalentTo(ExpectedTimestamp);
        result.DataType.AbsoluteUri.Should().Be(ExpectedDataType);
    }
}