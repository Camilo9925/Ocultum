namespace Ocultum.Core.Models;

public sealed record ExtractionResult(
    string Id,
    PayloadKind Kind,
    string? Text,
    string FileName,
    long SizeBytes,
    string Sha256,
    bool IsCompressed,
    string Encryption,
    DateTimeOffset ExpiresAt,
    string CommandLine);
