using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using Xunit.Abstractions;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace AquilaSolutions.LdesServer.Fetching.Test.Extensions;

public static class TestOutputHelperExtensions
{
    public static void DumpGraph(this ITestOutputHelper helper, IGraph g)
    {
        var writer = new CompressingTurtleWriter(WriterCompressionLevel.High, TurtleSyntax.W3C);
        var content = StringWriter.Write(g, writer);
        helper.WriteLine("----------");
        helper.WriteLine(content);
        helper.WriteLine("----------");
        helper.WriteLine("");
    }
}