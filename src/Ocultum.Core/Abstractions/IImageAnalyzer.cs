using Ocultum.Core.Models;

namespace Ocultum.Core.Abstractions;

public interface IImageAnalyzer
{
    ImageComparison Compare(ReadOnlySpan<byte> cover, ReadOnlySpan<byte> stego);

    byte[] RecompressJpeg(ReadOnlySpan<byte> jpeg, int quality);
}
