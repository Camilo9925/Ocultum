using Microsoft.AspNetCore.Mvc;
using Ocultum.Core.Services;
using Ocultum.Web.Contracts;

namespace Ocultum.Web.Controllers.Api;

[ApiController]
[Route("api/v1/robustness-tests")]
public sealed class RobustnessTestsController(ISteganalysisService steganalysis) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<RobustnessTestResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromForm] CreateRobustnessTestRequest request, CancellationToken cancellationToken)
    {
        var result = await steganalysis.TestRobustnessAsync(request.ToCommand(), cancellationToken);
        return Ok(RobustnessTestResponse.From(result));
    }
}
