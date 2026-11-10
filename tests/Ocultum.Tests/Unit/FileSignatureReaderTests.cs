using Ocultum.Core.Models;
using Ocultum.Core.Validation;

namespace Ocultum.Tests.Unit;

public class FileSignatureReaderTests
{
    private readonly FileSignatureReader _reader = new();

    [Fact]
    public void Detects_jpeg() =>
        _reader.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0]).Should().Be(StegoFormat.Jpeg);

    [Fact]
    public void Detects_bmp() =>
        _reader.Detect("BM"u8.ToArray().Concat(new byte[10]).ToArray()).Should().Be(StegoFormat.Bmp);

    [Fact]
    public void Detects_wav() =>
        _reader.Detect("RIFF\0\0\0\0WAVE"u8.ToArray()).Should().Be(StegoFormat.Wav);

    [Fact]
    public void Detects_au() =>
        _reader.Detect(".snd\0\0\0\0\0\0\0\0"u8.ToArray()).Should().Be(StegoFormat.Au);

    [Fact]
    public void Rejects_riff_that_is_not_wave() =>
        _reader.Detect("RIFF\0\0\0\0AVI "u8.ToArray()).Should().BeNull();

    [Fact]
    public void Rejects_png() =>
        _reader.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0]).Should().BeNull();

    [Fact]
    public void Rejects_short_input() =>
        _reader.Detect([0xFF]).Should().BeNull();
}
