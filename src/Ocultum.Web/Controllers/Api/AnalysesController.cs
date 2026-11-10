using Microsoft.AspNetCore.Mvc;
using Ocultum.Core.Services;
using Ocultum.Web.Contracts;

namespace Ocultum.Web.Controllers.Api;

[ApiController]
[Route("api/v1/analyses")]
public sealed class AnalysesController(ISteganalysisService steganalysis) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<AnalysisResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromForm] CreateAnalysisRequest request, CancellationToken cancellationToken)
    {
        var result = await steganalysis.AnalyzeAsync(request.ToCommand(), cancellationToken);
        return Ok(AnalysisResponse.From(result));
    }
}
