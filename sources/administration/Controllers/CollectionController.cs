using AquilaSolutions.LdesServer.Administration.Interfaces;
using AquilaSolutions.LdesServer.Core;
using AquilaSolutions.LdesServer.Core.InputFormatters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace AquilaSolutions.LdesServer.Administration.Controllers;

[ApiController]
[Route("admin/api/v1/collection")]
public class CollectionController : ControllerBase
{
    [HttpPost("", Name = "CreateCollection")]
    [Tags("Collections")]
    [EndpointSummary("Validates and stores a new collection given a collection definition.")]
    [Consumes(RdfMimeTypes.Turtle, RdfMimeTypes.TriG, RdfMimeTypes.JsonLd, RdfMimeTypes.NTriples, RdfMimeTypes.NQuads)]
    public async Task<IActionResult> CreateCollection(
        [FromBody] Stream stream,
        [FromHeader(Name = "Content-Type")] string mimeType,
        [FromServices] ICollectionService collectionService,
        [FromServices] LinkedDataReader reader)
    {
        using var graph = reader.ParseGraph(mimeType, stream);
        if (graph is null)
            return new UnsupportedMediaTypeResult();
        
        var result = await collectionService.CreateCollectionAsync(graph).ConfigureAwait(false);
        return result.IsValid
            ? new CreatedResult { StatusCode = 201 }
            : ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));
    }


    [HttpDelete("{collection}", Name = "DeleteCollection")]
    [Tags("Collections")]
    [EndpointSummary("Finds the given collection by name and removes it.")]
    public async Task<IActionResult> DeleteCollection(
        [FromRoute] string collection,
        [FromServices] ICollectionService collectionService)
    {
        var deleted = await collectionService.DeleteCollectionAsync(collection).ConfigureAwait(false);
        return deleted ? Ok() : NotFound(collection);
    }


    [HttpGet("{collection}", Name = "GetCollectionDefinition")]
    [Tags("Collections")]
    [EndpointSummary("Looks up the requested collection by name and returns its definition.")]
    public async Task<IActionResult> GetCollectionDefinition(
        [FromRoute] string collection,
        [FromServices] ICollectionService collectionService)
    {
        var result = await collectionService.GetCollectionAsync(collection).ConfigureAwait(false);
        return result is not null ? Ok(result) : NotFound(collection);
    }

    [HttpHead("{collection}")]
    [Tags("Collections")]
    public Task<IActionResult> GetCollectionDefinitionMetadata(
        [FromRoute] string collection, [FromServices] ICollectionService collectionService) =>
        GetCollectionDefinition(collection, collectionService);

    
    [HttpGet("", Name = "GetCollectionDefinitions")]
    [Tags("Collections")]
    [EndpointSummary("Looks up all collections and returns their definitions.")]
    public async Task<IActionResult> GetCollectionDefinitions(
        [FromServices] ICollectionService collectionService)
    {
        var result = await collectionService.GetCollectionsAsync().ConfigureAwait(false);
        return Ok(result);
    }

    [HttpHead("")]
    [Tags("Collections")]
    [ExcludeFromDescription]
    public Task<IActionResult> GetCollectionDefinitionsMetadata([FromServices] ICollectionService collectionService)
        => GetCollectionDefinitions(collectionService);
}