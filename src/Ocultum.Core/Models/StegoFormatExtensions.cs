namespace Ocultum.Core.Models;

public static class StegoFormatExtensions
{
    public static string FileExtension(this StegoFormat format) => format switch
    {
        StegoFormat.Jpeg => ".jpg",
        StegoFormat.Bmp => ".bmp",
        StegoFormat.Wav => ".wav",
        StegoFormat.Au => ".au",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static string ContentType(this StegoFormat format) => format switch
    {
        StegoFormat.Jpeg => "image/jpeg",
        StegoFormat.Bmp => "image/bmp",
        StegoFormat.Wav => "audio/wav",
        StegoFormat.Au => "audio/basic",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    public static bool IsImage(this StegoFormat format) => format is StegoFormat.Jpeg or StegoFormat.Bmp;
}
