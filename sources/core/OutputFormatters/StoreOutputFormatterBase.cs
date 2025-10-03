using System.Text;
using AquilaSolutions.LdesServer.Core.Extensions;
using AquilaSolutions.LdesServer.Core.Models;
using Microsoft.AspNetCore.Mvc.Formatters;
using VDS.RDF;

namespace AquilaSolutions.LdesServer.Core.OutputFormatters;

public abstract class StoreOutputFormatterBase : TextOutputFormatter
{
    protected StoreOutputFormatterBase()
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
        var thing = context.Object;
        if (thing is null) return Task.CompletedTask;

        ITripleStore store;
        switch (thing)
        {
            case ITripleStore t:
                store = t;
                break;
            case IGraph graph:
                store = new TripleStore();
                store.Add(graph);
                break;
            case LdesNode n:
            {
                context.HttpContext.Response.Headers.ExtendWith(n.Metadata);
                store = n.Store;
                break;
            }
            default:
                throw new InvalidOperationException($"Cannot format unknown type {thing.GetType()}");
        }

        try
        {
            var rdfWriter = CreateWriter();
            var textWriter = context.WriterFactory(context.HttpContext.Response.Body, selectedEncoding);
            rdfWriter.Save(store, textWriter);
        }
        finally
        {
            (thing as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    protected abstract IStoreWriter CreateWriter();
}