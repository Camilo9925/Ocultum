namespace Ocultum.Core.Models;

/// <param name="Kind">Which API resource owns the entry (<c>stego-file</c> or <c>extraction</c>).</param>
/// <param name="ContentFile">Simple file name of the content inside the workspace directory.</param>
/// <param name="FileName">Sanitized name suggested to the user on download.</param>
public sealed record StoredFileMetadata(
    string Kind,
    string ContentFile,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Sha256);
