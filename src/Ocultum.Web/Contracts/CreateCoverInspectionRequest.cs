using System.ComponentModel.DataAnnotations;

namespace Ocultum.Web.Contracts;

public sealed record CreateCoverInspectionRequest
{
    [Required]
    public IFormFile Cover { get; init; } = null!;
}
