using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record AnalysisResponse(
    int Width,
    int Height,
    ImageStatisticsResponse Cover,
    ImageStatisticsResponse Stego,
    double? Psnr,
    bool Identical,
    int MaxPixelDifference,
    double ChangedPixelsPercent,
    int Amplification,
    string DifferenceImage)
{
    public static AnalysisResponse From(ImageComparison comparison) => new(
        comparison.Width,
        comparison.Height,
        new ImageStatisticsResponse(comparison.CoverHistogram, comparison.CoverEntropy),
        new ImageStatisticsResponse(comparison.StegoHistogram, comparison.StegoEntropy),
        comparison.Psnr,
        comparison.MaxPixelDifference == 0,
        comparison.MaxPixelDifference,
        comparison.ChangedPixelsPercent,
        comparison.Amplification,
        Convert.ToBase64String(comparison.DifferenceImagePng));
}

public sealed record ImageStatisticsResponse(ChannelHistograms Histogram, double Entropy);
