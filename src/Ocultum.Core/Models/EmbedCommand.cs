namespace Ocultum.Core.Models;

/// <summary>Exactly one of <see cref="Message"/> or a non-empty <see cref="PayloadFiles"/> must be provided.</summary>
public sealed record EmbedCommand(
    UploadedFile Cover,
    IReadOnlyList<UploadedFile> PayloadFiles,
    string? Message,
    EmbedOptions Options);
