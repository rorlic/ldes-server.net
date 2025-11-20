using LdesServer.Serving.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LdesServer.Serving.Controllers;

[ApiController]
[Route("feed")]
public class NodeController : ControllerBase
{
    [HttpGet("{ldes}", Name = "GetEventStream")]
    [Tags("Event Streams")]
    [EndpointSummary("Retrieves a Linked Data Event Stream.")]
    public async Task<IActionResult> GetEventStreamNode(
        [FromRoute] string ldes,
        [FromServices] NodeService nodeService)
    {
        var result = await nodeService.GetEventStreamAsync(ldes).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpHead("{ldes}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> GetEventStreamNodeMetadata(
        [FromRoute] string ldes, 
        [FromServices] NodeService nodeService) => GetEventStreamNode(ldes, nodeService);
    

    [HttpGet("{ldes}/{view}", Name = "GetView")]
    [Tags("Event Streams")]
    [EndpointSummary("Retrieves a view.")]
    public async Task<IActionResult> GetViewNode(
        [FromRoute] string ldes,
        [FromRoute] string view,
        [FromServices] NodeService nodeService)
    {
        var result = await nodeService.GetViewAsync(ldes, view).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpHead("{ldes}/{view}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> GetViewNodeMetadata(
        [FromRoute] string ldes, 
        [FromRoute] string view, 
        [FromServices] NodeService nodeService) => GetViewNode(ldes, view, nodeService);


    [HttpGet("{ldes}/{view}/{page}", Name = "GetPage")]
    [Tags("Event Streams")]
    [EndpointSummary("Retrieves a view Page.")]
    public async Task<IActionResult> GetPageNode(
        [FromRoute] string ldes,
        [FromRoute] string view,
        [FromRoute] string page,
        [FromServices] NodeService nodeService)
    {
        var baseUrl = Request.GetEncodedUrl().TrimEnd('/').Replace($"{ldes}/{view}/{page}", string.Empty);
        var result = await nodeService.GetPageAsync(ldes, view, page).ConfigureAwait(false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpHead("{ldes}/{view}/{page}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public Task<IActionResult> GetPageNodeMetadata(
        [FromRoute] string ldes, 
        [FromRoute] string view, 
        [FromRoute] string page, 
        [FromServices] NodeService nodeService) => GetPageNode(ldes, view, page, nodeService);
}