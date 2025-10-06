using AquilaSolutions.LdesServer.Core;
using AquilaSolutions.LdesServer.Core.InputFormatters;
using AquilaSolutions.LdesServer.Ingestion.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AquilaSolutions.LdesServer.Ingestion.Controllers;

[ApiController]
[Route("data")]
public class MemberController : ControllerBase
{
    [HttpPost("{collectionName}", Name = "PostMemberData")]
    [Tags("Members")]
    [EndpointSummary("Ingests one or more members for the given collection.")]
    [Consumes(RdfMimeTypes.Turtle, RdfMimeTypes.TriG, RdfMimeTypes.JsonLd, RdfMimeTypes.NTriples, RdfMimeTypes.NQuads)]
    public async Task<IActionResult> CreateMembers(
        [FromRoute] string collectionName,
        [FromBody] Stream stream,
        [FromHeader(Name = "Content-Type")] string mimeType,
        [FromServices] MemberService memberService,
        [FromServices] LinkedDataReader reader)
    {
        try
        {
            using var store = reader.ParseStore(mimeType, stream);
        
            if (store is null)
                return new UnsupportedMediaTypeResult();
            
            var memberIds = await memberService
                .IngestCollectionMembersAsync(collectionName, store.Quads)
                .ConfigureAwait(false);

            return memberIds is null
                ? NotFound(collectionName)
                : new JsonResult(memberIds.ToArray()) { StatusCode = StatusCodes.Status201Created };
        }
        finally
        {
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }
}