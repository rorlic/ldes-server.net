using System.Text;
using AquilaSolutions.LdesServer.Core.InputFormatters;
using AquilaSolutions.LdesServer.Core.Models;
using AquilaSolutions.LdesServer.Core.OutputFormatters;
using FluentAssertions;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Core.Test.OutputFormatters;

public class GraphOutputFormatterBaseTest
{
    public static IEnumerable<object[]> InputData =>
    [
        [RdfMimeTypes.NTriples, new NTriplesOutputFormatter()],
        [RdfMimeTypes.Turtle, new TurtleOutputFormatter()],
    ];

    [Theory]
    [MemberData(nameof(InputData))]
    public async Task CanWriteGraphAsRdfSerialization(string contentType, GraphOutputFormatterBase formatter)
    {
        using var store = new SimpleTripleStore();
        var quads = LoadResource.FromTurtle("Formats.format.ttl");
        quads.ToList().ForEach(x => store.Assert(x));

        var g = store.Graphs[null as IRefNode];
        var context = OutputFormatterContextFactory.BuildContext(g, contentType, typeof(IGraph));

        await formatter.WriteResponseBodyAsync(context, Encoding.Default).ConfigureAwait(false);

        context.HttpContext.Response.Body.Length.Should().BeGreaterThan(0);
    }
    
    [Theory]
    [MemberData(nameof(InputData))]
    public async Task CanWriteLdesNodeAsRdfSerialization(string contentType, GraphOutputFormatterBase formatter)
    {
        var node = LoadResource.FromTrigAsLdesNode("Data.ExamplePage.trig");
        var context = OutputFormatterContextFactory.BuildContext(node, contentType, typeof(LdesNode));

        await formatter.WriteResponseBodyAsync(context, Encoding.Default).ConfigureAwait(false);

        context.HttpContext.Response.Body.Length.Should().BeGreaterThan(0);
    }
}