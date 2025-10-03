using AquilaSolutions.LdesServer.Core.InputFormatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using VDS.RDF;
using VDS.RDF.JsonLd.Syntax;
using VDS.RDF.Writing;

namespace AquilaSolutions.LdesServer.Core.OutputFormatters;

public class JsonLdOutputFormatter : StoreOutputFormatterBase
{
    public JsonLdOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(RdfMimeTypes.JsonLd));        
    }

    protected override IStoreWriter CreateWriter()
    {
        return new JsonLdWriter(new JsonLdWriterOptions
        { 
          // TODO: add framing support
          JsonFormatting  = Formatting.Indented,
          ProcessingMode = JsonLdProcessingMode.JsonLd11,
          Ordered = true,
          UseNativeTypes = true
        });
    }
}