using System.Text;
using LdesServer.Core.Extensions;
using LdesServer.Core.InputFormatters;
using LdesServer.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using VDS.RDF.Writing.Formatting;

namespace LdesServer.Core.OutputFormatters;

public class NodeAsTreeProfileOutputFormatter : TextOutputFormatter
{
    private const string QuadsContentType = RdfMimeTypes.NQuads;
    private const string TriplesContentType = RdfMimeTypes.NTriples;
    private const string ProfilePostfix = "; profile=\"https://w3id.org/tree/profile\"";

    public NodeAsTreeProfileOutputFormatter()
    {
        SupportedEncodings.Add(Encoding.Default);
        SupportedEncodings.Add(Encoding.Unicode);

        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(QuadsContentType));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(TriplesContentType));
    }

    protected override bool CanWriteType(Type? type)
    {
        return type == typeof(LdesNode);
    }

    private readonly NQuads11Formatter _formatter = new();

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        if (context.Object is not LdesNode node) return;

        var httpResponse = context.HttpContext.Response;
        context.HttpContext.Response.Headers.ExtendWith(node.Metadata);

        var contentType = httpResponse.ContentType;
        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        switch (mediaType.MediaType.ToString())
        {
            case TriplesContentType:
                httpResponse.ContentType += ProfilePostfix;
                foreach (var t in node.Triples)
                {
                    var formatted = _formatter.Format(t);
                    await httpResponse.WriteAsync($"{formatted}\n", selectedEncoding);
                }

                break;
            case QuadsContentType:
            {
                httpResponse.ContentType += ProfilePostfix;
                foreach (var q in node.Quads)
                {
                    var formatted = _formatter.Format(q.AsTriple(), q.Graph);
                    await httpResponse.WriteAsync($"{formatted}\n", selectedEncoding);
                }

                break;
            }
            default:
                throw new NotSupportedException($"Unsupported content type: {contentType}.");
        }
    }
}