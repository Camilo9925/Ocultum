using Microsoft.AspNetCore.Mvc;
using Ocultum.Core.Services;
using Ocultum.Web.Contracts;

namespace Ocultum.Web.Controllers.Api;

[ApiController]
[Route("api/v1/cover-inspections")]
public sealed class CoverInspectionsController(ICoverInspectionService inspection) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<CoverInspectionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromForm] CreateCoverInspectionRequest request, CancellationToken cancellationToken)
    {
        var result = await inspection.InspectAsync(request.Cover.ToUploadedFile(), cancellationToken);
        return Ok(CoverInspectionResponse.From(result));
    }
}
