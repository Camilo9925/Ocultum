namespace Ocultum.Core.Models;

public sealed record CoverInspection(
    StegoFormat Format,
    string FormatDescription,
    long SizeBytes,
    long CapacityBytes);
