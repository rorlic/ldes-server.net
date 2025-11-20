using Microsoft.Net.Http.Headers;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace LdesServer.Core.OutputFormatters;

public class NQuadsOutputFormatter : StoreOutputFormatterBase
{
    public NQuadsOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(RdfMimeTypes.NQuads));
    }

    protected override IStoreWriter CreateWriter()
    {
        return new NQuadsWriter
        {
            Syntax = NQuadsSyntax.Rdf11,
            PrettyPrintMode = false,
            UseMultiThreadedWriting = true
        };
    }
}