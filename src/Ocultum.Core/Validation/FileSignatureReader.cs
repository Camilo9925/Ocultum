using Ocultum.Core.Abstractions;
using Ocultum.Core.Models;

namespace Ocultum.Core.Validation;

public sealed class FileSignatureReader : IFileSignatureReader
{
    public const int HeaderLength = 12;

    public StegoFormat? Detect(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return StegoFormat.Jpeg;
        }

        if (header.Length >= 2 && header[0] == (byte)'B' && header[1] == (byte)'M')
        {
            return StegoFormat.Bmp;
        }

        if (header.Length >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WAVE"u8))
        {
            return StegoFormat.Wav;
        }

        if (header.Length >= 4 && header[..4].SequenceEqual(".snd"u8))
        {
            return StegoFormat.Au;
        }

        return null;
    }
}
