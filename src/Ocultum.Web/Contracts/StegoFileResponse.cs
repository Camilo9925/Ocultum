using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record StegoFileResponse(
    string Id,
    string FileName,
    long SizeBytes,
    string Sha256,
    bool IsPasswordProtected,
    DateTime ExpiresAt,
    StegoFormat Format,
    long CoverCapacityBytes,
    long PayloadSizeBytes,
    string PayloadSha256,
    string Command)
{
    public static StegoFileResponse From(StegoFileResult result) => new(
        result.Id,
        result.FileName,
        result.SizeBytes,
        result.Sha256,
        result.IsPasswordProtected,
        result.ExpiresAt.UtcDateTime,
        result.Format,
        result.CoverCapacityBytes,
        result.PayloadSizeBytes,
        result.PayloadSha256,
        result.CommandLine);
}
