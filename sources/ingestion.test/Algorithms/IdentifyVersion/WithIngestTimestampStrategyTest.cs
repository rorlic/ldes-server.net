using System.Globalization;
using AquilaSolutions.LdesServer.Ingestion.Algorithms.IdentifyVersion;
using FluentAssertions;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Ingestion.Test.Algorithms.IdentifyVersion;

public class WithIngestTimestampStrategyTest
{
    [Fact]
    public void ReturnsTheCurrentTimestamp()
    {
        var createdAt = DateTimeOffset.UtcNow;
        var sut = new WithIngestTimestampStrategy();
        
        var result = sut.FindOrCreateEntityVersion(Array.Empty<Quad>(), new UriNode(new Uri("http://dummy.com")), createdAt);

        result.DataType.AbsoluteUri.Should().BeEquivalentTo("http://www.w3.org/2001/XMLSchema#dateTime");
        var value = DateTimeOffset.Parse(result.Value, null, DateTimeStyles.RoundtripKind);
        value.Should().BeCloseTo(createdAt, TimeSpan.FromMicroseconds(1));
    }
}