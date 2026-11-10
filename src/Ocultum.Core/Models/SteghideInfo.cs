namespace Ocultum.Core.Models;

public sealed record SteghideInfo(
    string FormatDescription,
    long CapacityBytes,
    EmbeddedDataInfo? Embedded,
    string CommandLine);
