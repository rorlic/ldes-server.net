namespace AquilaSolutions.LdesServer.Core.Test.OutputFormatters;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;

public static class OutputFormatterContextFactory
{
    public static OutputFormatterWriteContext BuildContext(object model, string contentType, Type modelType)
    {
        var httpContext = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream(),
                ContentType = contentType,
            }
        };

        return new OutputFormatterWriteContext(httpContext,
            (stream, encoding) => new HttpResponseStreamWriter(stream, encoding), modelType, model);
    }
}