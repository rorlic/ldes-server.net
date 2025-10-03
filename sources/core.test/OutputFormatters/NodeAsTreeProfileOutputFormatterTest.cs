using System.Text;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.InputFormatters;
using AquilaSolutions.LdesServer.Core.OutputFormatters;
using FluentAssertions;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Parsing;

namespace AquilaSolutions.LdesServer.Core.Test.OutputFormatters;

public class NodeAsTreeProfileOutputFormatterTest
{
    [Fact]
    public async Task CanWriteLdesNodeAsNQuads()
    {
        var contentType = RdfMimeTypes.NQuads;
        var ldesNode = LoadResource.FromTrigAsLdesNode("Data.ExamplePage.trig");
        var formatter = new NodeAsTreeProfileOutputFormatter();
        var context = OutputFormatterContextFactory.BuildContext(ldesNode, contentType, typeof(LdesNode));

        await formatter.WriteResponseBodyAsync(context, Encoding.Default).ConfigureAwait(false);

        var response = context.HttpContext.Response;
        response.ContentType.Should().Be($"{contentType}; profile=\"https://w3id.org/tree/profile\"");

        var body = response.Body;
        body.Length.Should().BeGreaterThan(0);
        body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(body);
        var client = new TreeProfileTestClient("http://example.org/ldes");
        new NQuadsParser().Load(client, reader);

        client.GetHypermedia().Should().HaveCount(5);
        var members = client.GetMembers().ToList();
        members.Should().HaveCount(2);
        members.ForEach(x => x.Should().HaveCount(5));
    }

    [Fact]
    public async Task CanWriteLdesNodeAsNtriples()
    {
        var contentType = RdfMimeTypes.NTriples;
        var ldesNode = LoadResource.FromTrigAsLdesNode("Data.ExamplePage.trig");
        var formatter = new NodeAsTreeProfileOutputFormatter();
        var context = OutputFormatterContextFactory.BuildContext(ldesNode, contentType, typeof(LdesNode));

        await formatter.WriteResponseBodyAsync(context, Encoding.Default).ConfigureAwait(false);

        var response = context.HttpContext.Response;
        response.ContentType.Should().Be($"{contentType}; profile=\"https://w3id.org/tree/profile\"");

        var body = response.Body;
        body.Length.Should().BeGreaterThan(0);
        body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(body);
        var client = new TreeProfileTestClient("http://example.org/ldes");
        new NQuadsParser().Load(client, reader);

        client.GetHypermedia().Should().HaveCount(5);
        var members = client.GetMembers().ToList();
        members.Should().HaveCount(2);
        members.ForEach(x => x.Should().HaveCount(5));

        client.GetQuads().ToList().ForEach(x => x.Graph.Should().BeNull());
    }
}