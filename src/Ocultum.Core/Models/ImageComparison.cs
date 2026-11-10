namespace Ocultum.Core.Models;

/// <param name="Psnr">Null when both images are pixel-identical.</param>
/// <param name="DifferenceImagePng">Amplified difference map, PNG encoded.</param>
public sealed record ImageComparison(
    int Width,
    int Height,
    ChannelHistograms CoverHistogram,
    ChannelHistograms StegoHistogram,
    double CoverEntropy,
    double StegoEntropy,
    double? Psnr,
    int MaxPixelDifference,
    double ChangedPixelsPercent,
    int Amplification,
    byte[] DifferenceImagePng);

public sealed record ChannelHistograms(long[] Red, long[] Green, long[] Blue);
