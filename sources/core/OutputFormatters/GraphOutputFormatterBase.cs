using System.Text;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Core.OutputFormatters;

public abstract class GraphOutputFormatterBase : TextOutputFormatter
{
    protected GraphOutputFormatterBase()
    {
        SupportedEncodings.Add(Encoding.Default);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    protected override bool CanWriteType(Type? type)
    {
        return typeof(IGraph).IsAssignableFrom(type) || 
               typeof(ITripleStore).IsAssignableFrom(type) || 
               type == typeof(LdesNode);
    }
    
    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var rdfWriter = CreateWriter();
        var thing = context.Object;
        if (thing is not null)
        {
            if (thing is LdesNode node)
            {
                thing = node.Graph;
                context.HttpContext.Response.Headers.ExtendWith(node.Metadata);
            }
            else if (thing is ITripleStore store)
            {
                var g = new Graph().WithStandardPrefixes().WithServerPrefixes();
                g.Assert(store.Triples);
                store.Dispose();
                thing = g;
            } 
            
            using var graph = thing as IGraph;
            var textWriter = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding);
            rdfWriter.Save(graph, textWriter);
        }
        return Task.CompletedTask;
    }

    protected abstract IRdfWriter CreateWriter();
}