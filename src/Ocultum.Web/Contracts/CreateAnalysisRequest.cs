using System.ComponentModel.DataAnnotations;
using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record CreateAnalysisRequest
{
    [Required]
    public IFormFile Cover { get; init; } = null!;

    [Required]
    public IFormFile Stego { get; init; } = null!;

    public AnalysisCommand ToCommand() => new(Cover.ToUploadedFile(), Stego.ToUploadedFile());
}
