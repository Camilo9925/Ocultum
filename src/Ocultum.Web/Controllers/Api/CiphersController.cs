using Microsoft.AspNetCore.Mvc;
using Ocultum.Core.Services;
using Ocultum.Web.Contracts;

namespace Ocultum.Web.Controllers.Api;

[ApiController]
[Route("api/v1/ciphers")]
public sealed class CiphersController(ICipherCatalogService catalog) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CipherResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var ciphers = await catalog.GetCiphersAsync(cancellationToken);
        return Ok(ciphers.Select(CipherResponse.From).ToList());
    }
}
