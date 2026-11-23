// src/serving/VaryAcceptExtensions.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace LdesServer.Serving.Extensions;

public static class VaryAcceptExtensions
{
    /// <summary>
    /// Adds "Vary: Accept" to every GET/HEAD response under <paramref name="pathPrefix"/>,
    /// whatever its status code. Never duplicates "Accept" and never touches "Vary: *".
    /// </summary>
    public static IApplicationBuilder UseVaryAccept(this IApplicationBuilder app, PathString pathPrefix)
    {
        return app.Use(async (context, next) =>
        {
            if ((HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
                context.Request.Path.StartsWithSegments(pathPrefix))
            {
                context.Response.OnStarting(() =>
                {
                    var vary = context.Response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
                    if (!vary.Any(value => value == "*" ||
                                           value.Equals(HeaderNames.Accept, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.Accept);
                    }
                    return Task.CompletedTask;
                });
            }

            await next();
        });
    }
}