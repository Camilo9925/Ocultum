using Ocultum.Infrastructure.Steghide;

namespace Ocultum.Tests.Unit;

public class SteghideOutputParserTests
{
    private const string InfoWithData = """
        "stego.jpg":
          format: jpeg
          capacity: 9.4 KB
          embedded file "secret.txt":
            size: 80.0 Byte
            encrypted: rijndael-128, cbc
            compressed: yes
        """;

    [Fact]
    public void Parses_info_with_embedded_file()
    {
        SteghideOutputParser.TryParseInfo(InfoWithData, out var format, out var capacity, out var embedded).Should().BeTrue();

        format.Should().Be("jpeg");
        capacity.Should().Be((long)Math.Round(9.4 * 1024));
        embedded.Should().NotBeNull();
        embedded!.FileName.Should().Be("secret.txt");
        embedded.SizeBytes.Should().Be(80);
        embedded.Encryption.Should().Be("rijndael-128, cbc");
        embedded.IsCompressed.Should().BeTrue();
    }

    [Fact]
    public void Parses_info_without_embedded_name()
    {
        const string output = """
            "s7.jpg":
              format: jpeg
              capacity: 9.4 KB
              embedded data:
                size: 80.0 Byte
                encrypted: no
                compressed: no
            """;

        SteghideOutputParser.TryParseInfo(output, out _, out _, out var embedded).Should().BeTrue();

        embedded!.FileName.Should().BeNull();
        embedded.Encryption.Should().BeNull();
        embedded.IsCompressed.Should().BeFalse();
    }

    [Fact]
    public void Parses_info_for_a_cover_with_nothing_embedded()
    {
        const string output = """
            "cover.bmp":
              format: Windows 3.x bitmap
              capacity: 14.6 KB
            """;

        SteghideOutputParser.TryParseInfo(output, out var format, out _, out var embedded).Should().BeTrue();

        format.Should().Be("Windows 3.x bitmap");
        embedded.Should().BeNull();
    }

    [Theory]
    [InlineData("2.0 KB", 2048)]
    [InlineData("1.5 MB", 1572864)]
    [InlineData("512.0 Byte", 512)]
    public void Converts_units_to_bytes(string capacity, long expected)
    {
        var output = $"\"x.au\":\n  format: au audio\n  capacity: {capacity}\n";

        SteghideOutputParser.TryParseInfo(output, out _, out var bytes, out _).Should().BeTrue();

        bytes.Should().Be(expected);
    }

    [Fact]
    public void Rejects_output_without_capacity() =>
        SteghideOutputParser.TryParseInfo("steghide: could not open the file.", out _, out _, out _).Should().BeFalse();

    [Fact]
    public void Parses_ciphers_and_skips_header_lines()
    {
        const string output = """
            encryption algorithms:
            <algorithm>: <supported modes>...
            cast-128: cbc cfb ctr ecb ncfb nofb ofb
            rijndael-128: cbc cfb ctr ecb ncfb nofb ofb
            arcfour: stream
            """;

        var ciphers = SteghideOutputParser.ParseCiphers(output);

        ciphers.Select(c => c.Name).Should().Equal("cast-128", "rijndael-128", "arcfour");
        ciphers[1].Modes.Should().Contain(["cbc", "ofb"]);
        ciphers[2].Modes.Should().Equal("stream");
    }
}
