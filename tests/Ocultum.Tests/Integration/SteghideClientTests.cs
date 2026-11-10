using System.Security.Cryptography;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Tests.Support;

namespace Ocultum.Tests.Integration;

public class SteghideClientTests : IDisposable
{
    private readonly SteghideTestHost _host = new();
    private readonly string _workDir;

    public SteghideClientTests()
    {
        _workDir = _host.Workspace.Create().Path;
    }

    public void Dispose() => _host.Dispose();

    private void CopySample(string name, string? asName = null) =>
        File.Copy(TestPaths.Sample(name), Path.Combine(_workDir, asName ?? name), overwrite: true);

    [WindowsTheory]
    [InlineData("cover.jpg", "jpeg")]
    [InlineData("cover.bmp", "Windows 3.x bitmap")]
    [InlineData("cover.wav", "wave audio, PCM encoding")]
    [InlineData("cover.au", "au audio, PCM encoding")]
    public async Task Info_reports_format_and_capacity_of_every_supported_cover(string cover, string expectedFormat)
    {
        CopySample(cover);

        var info = await _host.Client.InfoAsync(_workDir, cover, "", CancellationToken.None);

        info.FormatDescription.Should().Be(expectedFormat);
        info.CapacityBytes.Should().BeGreaterThan(1000);
        info.Embedded.Should().BeNull();
    }

    [WindowsTheory]
    [InlineData("cover.jpg", "")]
    [InlineData("cover.jpg", "s3cret pass")]
    [InlineData("cover.bmp", "s3cret pass")]
    [InlineData("cover.wav", "s3cret pass")]
    [InlineData("cover.au", "s3cret pass")]
    public async Task Embed_then_extract_round_trips(string cover, string passphrase)
    {
        CopySample(cover);
        CopySample("secret.txt");
        var stego = "stego" + Path.GetExtension(cover);

        await _host.Client.EmbedAsync(_workDir, cover, "secret.txt", stego, new EmbedOptions(passphrase), CancellationToken.None);
        await _host.Client.ExtractAsync(_workDir, stego, "out.bin", passphrase, CancellationToken.None);

        File.ReadAllBytes(Path.Combine(_workDir, "out.bin")).Should().Equal(File.ReadAllBytes(TestPaths.Sample("secret.txt")));
    }

    [WindowsFact]
    public async Task Info_with_the_right_passphrase_exposes_embedded_data()
    {
        CopySample("cover.jpg");
        CopySample("secret.txt");
        await _host.Client.EmbedAsync(_workDir, "cover.jpg", "secret.txt", "stego.jpg", new EmbedOptions("abc"), CancellationToken.None);

        var info = await _host.Client.InfoAsync(_workDir, "stego.jpg", "abc", CancellationToken.None);

        info.Embedded.Should().NotBeNull();
        info.Embedded!.FileName.Should().Be("secret.txt");
        info.Embedded.Encryption.Should().Be("rijndael-128, cbc");
    }

    [WindowsFact]
    public async Task Info_with_the_wrong_passphrase_still_returns_capacity_but_no_data()
    {
        CopySample("cover.jpg");
        CopySample("secret.txt");
        await _host.Client.EmbedAsync(_workDir, "cover.jpg", "secret.txt", "stego.jpg", new EmbedOptions("abc"), CancellationToken.None);

        var info = await _host.Client.InfoAsync(_workDir, "stego.jpg", "wrong", CancellationToken.None);

        info.Embedded.Should().BeNull();
        info.CapacityBytes.Should().BeGreaterThan(0);
    }

    [WindowsFact]
    public async Task Extract_with_wrong_passphrase_throws_the_typed_error()
    {
        CopySample("cover.jpg");
        CopySample("secret.txt");
        await _host.Client.EmbedAsync(_workDir, "cover.jpg", "secret.txt", "stego.jpg", new EmbedOptions("abc"), CancellationToken.None);

        var act = () => _host.Client.ExtractAsync(_workDir, "stego.jpg", "out.bin", "nope", CancellationToken.None);

        await act.Should().ThrowAsync<WrongPassphraseOrNoDataException>();
    }

    [WindowsFact]
    public async Task Extract_from_a_clean_cover_reports_the_same_error()
    {
        CopySample("cover.jpg");

        var act = () => _host.Client.ExtractAsync(_workDir, "cover.jpg", "out.bin", "", CancellationToken.None);

        await act.Should().ThrowAsync<WrongPassphraseOrNoDataException>();
    }

    [WindowsFact]
    public async Task Embed_larger_than_capacity_throws_capacity_exceeded()
    {
        CopySample("cover.au");
        File.WriteAllBytes(Path.Combine(_workDir, "big.bin"), RandomNumberGenerator.GetBytes(50_000));

        var act = () => _host.Client.EmbedAsync(_workDir, "cover.au", "big.bin", "stego.au", new EmbedOptions(), CancellationToken.None);

        await act.Should().ThrowAsync<CapacityExceededException>();
    }

    [WindowsFact]
    public async Task Embed_into_a_fake_jpeg_throws_unsupported_format()
    {
        File.WriteAllText(Path.Combine(_workDir, "fake.jpg"), "not really a jpeg");
        CopySample("secret.txt");

        var act = () => _host.Client.EmbedAsync(_workDir, "fake.jpg", "secret.txt", "stego.jpg", new EmbedOptions(), CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedCoverFormatException>();
    }

    [WindowsFact]
    public async Task Advanced_options_are_honoured()
    {
        CopySample("cover.jpg");
        CopySample("secret.txt");
        var options = new EmbedOptions("abc", Cipher: "twofish", Mode: "ofb", CompressionLevel: 9, EmbedChecksum: false, EmbedFileName: false);

        var run = await _host.Client.EmbedAsync(_workDir, "cover.jpg", "secret.txt", "stego.jpg", options, CancellationToken.None);
        var info = await _host.Client.InfoAsync(_workDir, "stego.jpg", "abc", CancellationToken.None);

        run.CommandLine.Should().Contain("-e twofish ofb").And.Contain("-z 9").And.Contain("-K").And.Contain("-N");
        info.Embedded!.Encryption.Should().Be("twofish, ofb");
        info.Embedded.FileName.Should().BeNull();
    }

    [WindowsFact]
    public async Task Displayed_command_masks_the_passphrase()
    {
        CopySample("cover.jpg");
        CopySample("secret.txt");

        var run = await _host.Client.EmbedAsync(_workDir, "cover.jpg", "secret.txt", "stego.jpg", new EmbedOptions("topsecret"), CancellationToken.None);

        run.CommandLine.Should().Contain("-p \"********\"").And.NotContain("topsecret");
    }

    [WindowsFact]
    public async Task Lists_the_available_ciphers()
    {
        var ciphers = await _host.Client.GetCiphersAsync(CancellationToken.None);

        ciphers.Should().Contain(c => c.Name == "rijndael-128" && c.Modes.Contains("cbc"));
    }

    [WindowsFact]
    public async Task A_missing_executable_is_reported_as_unavailable()
    {
        using var client = new Ocultum.Infrastructure.Steghide.SteghideClient(
            Microsoft.Extensions.Options.Options.Create(new Ocultum.Infrastructure.Steghide.SteghideOptions { ExecutablePath = Path.Combine(_workDir, "nope.exe") }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Ocultum.Infrastructure.Steghide.SteghideClient>.Instance);

        var act = () => client.GetCiphersAsync(CancellationToken.None);

        await act.Should().ThrowAsync<SteghideUnavailableException>();
    }

    [WindowsFact]
    public async Task Latin1_passphrases_round_trip_and_stay_distinct_from_question_marks()
    {
        CopySample("cover.jpg");
        CopySample("secret.txt");
        await _host.Client.EmbedAsync(_workDir, "cover.jpg", "secret.txt", "stego.jpg", new EmbedOptions("sênha-ção"), CancellationToken.None);

        await _host.Client.ExtractAsync(_workDir, "stego.jpg", "out.bin", "sênha-ção", CancellationToken.None);
        var wrong = () => _host.Client.ExtractAsync(_workDir, "stego.jpg", "out2.bin", "s?nha-??o", CancellationToken.None);

        await wrong.Should().ThrowAsync<WrongPassphraseOrNoDataException>();
    }
}
