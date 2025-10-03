using Microsoft.Net.Http.Headers;
using VDS.RDF;
using VDS.RDF.Writing;

namespace AquilaSolutions.LdesServer.Core.OutputFormatters;

public class TriGOutputFormatter : StoreOutputFormatterBase
{
    public TriGOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/x-trig"));
    }

    protected override IStoreWriter CreateWriter()
    {
        return new TriGWriter
        {
            CompressionLevel = WriterCompressionLevel.High,
            HighSpeedModePermitted = false,
            PrettyPrintMode = false
        };
    }
}