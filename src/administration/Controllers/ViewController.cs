using LdesServer.Administration.Interfaces;
using LdesServer.Core;
using LdesServer.Core.InputFormatters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LdesServer.Administration.Controllers;

[ApiController]
[Route("admin/api/v1/collection")]
public class ViewController : ControllerBase
{
    [HttpPost("{collection}/view", Name = "CreateView")]
    [Tags("Views")]
    [EndpointSummary("Validates and stores a new view on a collection given a view definition.")]
    [Consumes(RdfMimeTypes.Turtle, RdfMimeTypes.TriG, RdfMimeTypes.JsonLd, RdfMimeTypes.NTriples, RdfMimeTypes.NQuads)]
    public async Task<IActionResult> CreateView(
        [FromRoute] string collection,
        [FromBody] Stream stream,
        [FromHeader(Name = "Content-Type")] string mimeType,
        [FromServices] IViewService viewService,
        [FromServices] LinkedDataReader reader)
    {
        using var graph = reader.ParseGraph(mimeType, stream);
        if (graph is null)
            return new UnsupportedMediaTypeResult();

        var result = await viewService
            .CreateViewAsync(graph, collection)
            .ConfigureAwait(false);
        if (result is null) return NotFound(collection);
        return result.IsValid
            ? new CreatedResult { StatusCode = 201 }
            : ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
    }

    [HttpDelete("{collection}/view/{view}", Name = "DeleteView")]
    [Tags("Views")]
    [EndpointSummary("Finds the given view by collection and view name and removes it.")]
    public async Task<IActionResult> DeleteView(
        [FromRoute] string collection,
        [FromRoute] string view,
        [FromServices] IViewService viewService)
    {
        var deleted = await viewService
            .DeleteViewAsync(collection, view)
            .ConfigureAwait(false);
        return deleted ? Ok() : NotFound(collection);
    }

    [HttpGet("{collection}/view/{view}", Name = "GetViewDefinition")]
    [Tags("Views")]
    [EndpointSummary("Looks up the requested view by collection and view name and returns its definition.")]
    public async Task<IActionResult> GetViewDefinition(
        [FromRoute] string collection,
        [FromRoute] string view,
        [FromServices] IViewService viewService)
    {
        var result = await viewService
            .GetViewAsync(collection, view)
            .ConfigureAwait(false);
        return result is not null ? Ok(result) : NotFound(collection);
    }

    [HttpHead("{collection}/view/{view}")]
    [Tags("Views")]
    public Task<IActionResult> GetViewDefinitionMetadata(
        [FromRoute] string collection, [FromRoute] string view, [FromServices] IViewService viewService) =>
        GetViewDefinition(collection, view, viewService);

    
    [HttpGet("{collection}/view", Name = "GetViewDefinitions")]
    [Tags("Views")]
    [EndpointSummary("Looks up all views for the given collection and returns their definitions.")]
    public async Task<IActionResult> GetViewDefinitions(
        [FromRoute] string collection,
        [FromServices] IViewService viewService)
    {
        var result = await viewService
            .GetViewsAsync(collection)
            .ConfigureAwait(false);
        return Ok(result);
    }

    [HttpHead("{collection}/view")]
    [Tags("Views")]
    public Task<IActionResult> GetViewDefinitionsMetadata(
        [FromRoute] string collection, [FromServices] IViewService viewService) =>
        GetViewDefinitions(collection, viewService);
}