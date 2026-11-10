namespace Ocultum.Core.Models;

public sealed record RobustnessResult(
    int Quality,
    long OriginalSizeBytes,
    long RecompressedSizeBytes,
    bool Survived);
