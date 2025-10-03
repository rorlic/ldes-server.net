using AquilaSolutions.LdesServer.Core.Models;
using Microsoft.AspNetCore.Http;

namespace AquilaSolutions.LdesServer.Core.Extensions;

public static class HeaderDictionaryExtensions
{
    public static IHeaderDictionary ExtendWith(this IHeaderDictionary headers, LdesNode.Info metadata)
    {
        headers.CacheControl = metadata.CacheControl;
        headers.LastModified = metadata.LastModified;
        headers.ETag = metadata.ETag;
        headers.Vary = new[] { "Accept" };
        return headers;
    }
}