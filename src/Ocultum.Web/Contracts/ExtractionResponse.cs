using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record ExtractionResponse(
    string Id,
    PayloadKind Kind,
    string? Text,
    string FileName,
    long SizeBytes,
    string Sha256,
    bool IsCompressed,
    string Encryption,
    DateTime ExpiresAt,
    string Command)
{
    public static ExtractionResponse From(ExtractionResult result) => new(
        result.Id,
        result.Kind,
        result.Text,
        result.FileName,
        result.SizeBytes,
        result.Sha256,
        result.IsCompressed,
        result.Encryption,
        result.ExpiresAt.UtcDateTime,
        result.CommandLine);
}
