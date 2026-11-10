using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using SkiaSharp;

namespace Ocultum.Infrastructure.Imaging;

public sealed class ImageAnalysisService : IImageAnalyzer
{
    private const int MaxDifferenceImageWidth = 1600;
    private const int MaxAmplification = 64;

    public ImageComparison Compare(byte[] cover, byte[] stego)
    {
        using var coverBitmap = Decode(cover);
        using var stegoBitmap = Decode(stego);

        if (coverBitmap.Width != stegoBitmap.Width || coverBitmap.Height != stegoBitmap.Height)
        {
            throw new ImageProcessingException(
                "The images have different dimensions.",
                $"Cover is {coverBitmap.Width}x{coverBitmap.Height} and stego is {stegoBitmap.Width}x{stegoBitmap.Height}.");
        }

        var width = coverBitmap.Width;
        var height = coverBitmap.Height;
        var coverPixels = coverBitmap.Pixels;
        var stegoPixels = stegoBitmap.Pixels;

        var coverHistogram = BuildHistogram(coverPixels);
        var stegoHistogram = BuildHistogram(stegoPixels);

        var distances = new byte[coverPixels.Length];
        double squaredError = 0;
        long changed = 0;
        var maxDifference = 0;

        for (var i = 0; i < coverPixels.Length; i++)
        {
            var a = coverPixels[i];
            var b = stegoPixels[i];
            int dr = a.Red - b.Red, dg = a.Green - b.Green, db = a.Blue - b.Blue;
            squaredError += (double)dr * dr + (double)dg * dg + (double)db * db;

            var distance = Math.Max(Math.Abs(dr), Math.Max(Math.Abs(dg), Math.Abs(db)));
            distances[i] = (byte)distance;
            if (distance > 0)
            {
                changed++;
                maxDifference = Math.Max(maxDifference, distance);
            }
        }

        var mse = squaredError / (coverPixels.Length * 3d);
        double? psnr = mse == 0 ? null : 10 * Math.Log10(255d * 255d / mse);
        var amplification = maxDifference == 0 ? 1 : Math.Clamp(255 / maxDifference, 1, MaxAmplification);

        return new ImageComparison(
            width,
            height,
            coverHistogram,
            stegoHistogram,
            Entropy(coverHistogram),
            Entropy(stegoHistogram),
            psnr,
            maxDifference,
            changed * 100d / coverPixels.Length,
            amplification,
            RenderDifference(distances, width, height, amplification));
    }

    public byte[] RecompressJpeg(byte[] jpeg, int quality)
    {
        using var bitmap = Decode(jpeg);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return encoded?.ToArray() ?? throw new ImageProcessingException("The image could not be re-encoded.");
    }

    private static SKBitmap Decode(byte[] data)
    {
        return SKBitmap.Decode(data)
            ?? throw new ImageProcessingException("The image could not be decoded.");
    }

    private static ChannelHistograms BuildHistogram(SKColor[] pixels)
    {
        var red = new long[256];
        var green = new long[256];
        var blue = new long[256];
        foreach (var pixel in pixels)
        {
            red[pixel.Red]++;
            green[pixel.Green]++;
            blue[pixel.Blue]++;
        }

        return new ChannelHistograms(red, green, blue);
    }

    /// <summary>Shannon entropy, in bits per sample, over all three channels together.</summary>
    private static double Entropy(ChannelHistograms histogram)
    {
        var total = 0d;
        var counts = new double[256];
        for (var i = 0; i < 256; i++)
        {
            counts[i] = histogram.Red[i] + histogram.Green[i] + histogram.Blue[i];
            total += counts[i];
        }

        var entropy = 0d;
        foreach (var count in counts)
        {
            if (count > 0)
            {
                var p = count / total;
                entropy -= p * Math.Log2(p);
            }
        }

        return entropy;
    }

    private static byte[] RenderDifference(byte[] distances, int width, int height, int amplification)
    {
        var pixels = new SKColor[distances.Length];
        for (var i = 0; i < distances.Length; i++)
        {
            var level = Math.Min(255, distances[i] * amplification);
            pixels[i] = new SKColor((byte)level, (byte)(level * 0.55), 0);
        }

        using var map = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque) { Pixels = pixels };
        var target = map;
        SKBitmap? resized = null;
        if (width > MaxDifferenceImageWidth)
        {
            var scaledHeight = Math.Max(1, (int)((long)height * MaxDifferenceImageWidth / width));
            resized = map.Resize(new SKImageInfo(MaxDifferenceImageWidth, scaledHeight), new SKSamplingOptions(SKFilterMode.Linear));
            target = resized ?? map;
        }

        try
        {
            using var image = SKImage.FromBitmap(target);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }
        finally
        {
            resized?.Dispose();
        }
    }
}
