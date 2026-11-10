using Ocultum.Core.Models;

namespace Ocultum.Core.Abstractions;

public interface IImageAnalyzer
{
    ImageComparison Compare(byte[] cover, byte[] stego);

    byte[] RecompressJpeg(byte[] jpeg, int quality);
}
