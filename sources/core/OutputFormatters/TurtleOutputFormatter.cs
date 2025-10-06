using Microsoft.Net.Http.Headers;
using VDS.RDF;
using VDS.RDF.Writing;

namespace AquilaSolutions.LdesServer.Core.OutputFormatters;

public class TurtleOutputFormatter : GraphOutputFormatterBase
{
    public TurtleOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(RdfMimeTypes.Turtle));
    }

    protected override IRdfWriter CreateWriter()
    {
        return new CompressingTurtleWriter
        {
            CompressionLevel = WriterCompressionLevel.High, 
            PrettyPrintMode = false
        };
    }
}