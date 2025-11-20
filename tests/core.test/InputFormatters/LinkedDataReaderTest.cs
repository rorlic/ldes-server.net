using LdesServer.Core.InputFormatters;
using LdesServer.Core.Models.Configuration;
using FluentAssertions;

namespace LdesServer.Core.Test.InputFormatters;

public class LinkedDataReaderTest
{
    [Theory]
    [InlineData(RdfMimeTypes.NTriples, "Formats.format.nt")]
    [InlineData(RdfMimeTypes.Turtle, "Formats.format.ttl")]
    [InlineData(RdfMimeTypes.JsonLd, "Formats.format.default.jsonld")]
    [InlineData(RdfMimeTypes.NQuads, "Formats.format.default.nq")]
    [InlineData(RdfMimeTypes.TriG, "Formats.format.default.trig")]
    public void CanParseRdfSerializationAsTriples(string mimeType, string resourceName)
    {
        var sut = new LinkedDataReader(new LdesServerConfiguration{BaseUri = "http://example.org/"});
        var stream = LoadResource.GetEmbeddedStream(resourceName);
        var g = sut.ParseGraph(mimeType, stream);
        g.Should().NotBeNull();
    }
    
    [Theory]
    [InlineData(RdfMimeTypes.NTriples, "Formats.format.nt")]
    [InlineData(RdfMimeTypes.Turtle, "Formats.format.ttl")]
    [InlineData(RdfMimeTypes.JsonLd, "Formats.format.jsonld")]
    [InlineData(RdfMimeTypes.NQuads, "Formats.format.nq")]
    [InlineData(RdfMimeTypes.TriG, "Formats.format.trig")]
    public void CanParseRdfSerializationAsQuads(string mimeType, string resourceName)
    {
        var sut = new LinkedDataReader(new LdesServerConfiguration{BaseUri = "http://example.org/"});
        var stream = LoadResource.GetEmbeddedStream(resourceName);
        var store = sut.ParseStore(mimeType, stream);
        store.Should().NotBeNull();
        store.Graphs.Count.Should().Be(1);
    }
}