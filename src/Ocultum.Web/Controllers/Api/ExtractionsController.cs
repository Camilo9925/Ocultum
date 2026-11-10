using Microsoft.AspNetCore.Mvc;
using Ocultum.Core.Models;
using Ocultum.Core.Services;
using Ocultum.Web.Contracts;

namespace Ocultum.Web.Controllers.Api;

[ApiController]
[Route("api/v1/extractions")]
public sealed class ExtractionsController(IExtractionService extraction, IStoredFileService storedFiles) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<ExtractionResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromForm] CreateExtractionRequest request, CancellationToken cancellationToken)
    {
        var result = await extraction.ExtractAsync(request.ToCommand(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ExtractionResponse.From(result));
    }

    [HttpGet("{id}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContentAsync(string id, CancellationToken cancellationToken)
    {
        var content = await storedFiles.OpenAsync(StoredFileKinds.Extraction, id, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(content.Content, content.ContentType, content.FileName);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await storedFiles.DiscardAsync(StoredFileKinds.Extraction, id, cancellationToken);
        return NoContent();
    }
}
