using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record RobustnessTestResponse(int Quality, long OriginalSizeBytes, long RecompressedSizeBytes, bool Survived)
{
    public static RobustnessTestResponse From(RobustnessResult result) =>
        new(result.Quality, result.OriginalSizeBytes, result.RecompressedSizeBytes, result.Survived);
}
