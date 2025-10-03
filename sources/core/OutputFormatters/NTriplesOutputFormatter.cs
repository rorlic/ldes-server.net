using AquilaSolutions.LdesServer.Core.InputFormatters;
using Microsoft.Net.Http.Headers;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace AquilaSolutions.LdesServer.Core.OutputFormatters;

public class NTriplesOutputFormatter : GraphOutputFormatterBase
{
    public NTriplesOutputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(RdfMimeTypes.NTriples));
    }

    protected override IRdfWriter CreateWriter()
    {
        return new NTriplesWriter
        {
            SortTriples = false,
            Syntax = NTriplesSyntax.Rdf11
        };
    }
}