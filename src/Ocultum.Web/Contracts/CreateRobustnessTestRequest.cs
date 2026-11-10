using System.ComponentModel.DataAnnotations;
using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record CreateRobustnessTestRequest
{
    [Required]
    public IFormFile StegoFile { get; init; } = null!;

    public string? Passphrase { get; init; }

    [Range(1, 100)]
    public int Quality { get; init; } = 85;

    public RobustnessCommand ToCommand() => new(StegoFile.ToUploadedFile(), Passphrase ?? string.Empty, Quality);
}
