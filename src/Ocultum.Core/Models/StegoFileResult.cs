namespace Ocultum.Core.Models;

public sealed record StegoFileResult(
    string Id,
    string FileName,
    long SizeBytes,
    string Sha256,
    bool IsPasswordProtected,
    DateTimeOffset ExpiresAt,
    StegoFormat Format,
    long CoverCapacityBytes,
    long PayloadSizeBytes,
    string PayloadSha256,
    string CommandLine);
