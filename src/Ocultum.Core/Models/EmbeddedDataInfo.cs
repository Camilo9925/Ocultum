namespace Ocultum.Core.Models;

/// <param name="FileName">Name stored inside the stego file. Untrusted; null when it was embedded without a name.</param>
/// <param name="Encryption">Algorithm and mode as reported by Steghide, or null when not encrypted.</param>
public sealed record EmbeddedDataInfo(string? FileName, long SizeBytes, string? Encryption, bool IsCompressed);
