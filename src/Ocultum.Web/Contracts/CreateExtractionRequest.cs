using System.ComponentModel.DataAnnotations;
using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record CreateExtractionRequest
{
    [Required]
    public IFormFile StegoFile { get; init; } = null!;

    public string? Passphrase { get; init; }

    public ExtractCommand ToCommand() => new(StegoFile.ToUploadedFile(), Passphrase ?? string.Empty);
}
