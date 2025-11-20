using Microsoft.AspNetCore.Mvc.Formatters;

namespace LdesServer.Core.InputFormatters;

public class StreamInputFormatter : InputFormatter
{
    public StreamInputFormatter()
    {
        SupportedMediaTypes.Add(RdfMimeTypes.Turtle);
        SupportedMediaTypes.Add(RdfMimeTypes.TriG);
        SupportedMediaTypes.Add(RdfMimeTypes.JsonLd);
        SupportedMediaTypes.Add(RdfMimeTypes.NTriples);
        SupportedMediaTypes.Add(RdfMimeTypes.NQuads);
    }

    protected override bool CanReadType(Type type)
    {
        return type == typeof(Stream);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context)
    {
        var memoryStream = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(memoryStream);
        memoryStream.Position = 0; // Reset position for reading
        
        return await InputFormatterResult.SuccessAsync(memoryStream);
    }
}