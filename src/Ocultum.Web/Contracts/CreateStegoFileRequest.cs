using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record CreateStegoFileRequest
{
    [Required]
    public IFormFile Cover { get; init; } = null!;

    [FromForm(Name = "payloadFile")]
    public IFormFileCollection? PayloadFiles { get; init; }

    public string? Message { get; init; }

    public string? Passphrase { get; init; }

    /// <summary>Cipher and optional mode, e.g. <c>twofish ofb</c>, or <c>none</c>. Empty keeps the Steghide default.</summary>
    public string? Encryption { get; init; }

    [Range(0, 9)]
    public int? CompressionLevel { get; init; }

    public bool EmbedChecksum { get; init; } = true;

    public bool EmbedFileName { get; init; } = true;

    public EmbedCommand ToCommand()
    {
        var parts = (Encryption ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var options = new EmbedOptions(
            Passphrase ?? string.Empty,
            parts.Length > 0 ? parts[0] : null,
            parts.Length > 1 ? parts[1] : null,
            CompressionLevel,
            EmbedChecksum,
            EmbedFileName);

        var files = PayloadFiles?.Select(f => f.ToUploadedFile()).ToList() ?? [];
        return new EmbedCommand(Cover.ToUploadedFile(), files, Message, options);
    }
}
