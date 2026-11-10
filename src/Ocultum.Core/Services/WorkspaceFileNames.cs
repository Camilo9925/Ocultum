using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

internal static class WorkspaceFileNames
{
    public const string Extracted = "extracted.out";
    public const string FallbackPayload = "data.bin";

    public static string Cover(StegoFormat format) => "cover" + format.FileExtension();

    public static string Stego(StegoFormat format) => "stego" + format.FileExtension();

    public static bool IsReserved(string fileName) =>
        fileName.StartsWith("cover.", StringComparison.OrdinalIgnoreCase)
        || fileName.StartsWith("stego.", StringComparison.OrdinalIgnoreCase)
        || fileName.Equals(Extracted, StringComparison.OrdinalIgnoreCase);
}
