using AquilaSolutions.LdesServer.Administration.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AquilaSolutions.LdesServer.Administration.Controllers;

[ApiController]
[Route("admin/api/v1/shacl")]
public class ShaclController : ControllerBase
{
    [HttpGet("collection")]
    [Tags("Collections")]
    [EndpointSummary("Returns the collection definition SHACL.")]
    public Task<IActionResult> GetCollectionDefinitionShacl()
    {
        return Task.FromResult<IActionResult>(Ok(new CollectionValidator().Shacl));
    }

    [HttpHead("collection")]
    [Tags("Collections")]
    public Task<IActionResult> GetCollectionDefinitionShaclMetadata() => GetCollectionDefinitionShacl();
    
    
    [HttpGet("view")]
    [Tags("Views")]
    [EndpointSummary("Returns the view definition SHACL.")]
    public Task<IActionResult> GetViewDefinitionShacl()
    {
        return Task.FromResult<IActionResult>(Ok(new ViewValidator().Shacl));
    }

    [HttpHead("view")]
    [Tags("Views")]
    public Task<IActionResult> GetViewDefinitionShaclMetadata() => GetViewDefinitionShacl();
}