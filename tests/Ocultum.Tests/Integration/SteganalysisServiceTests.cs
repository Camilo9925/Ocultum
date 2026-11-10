using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Tests.Support;

namespace Ocultum.Tests.Integration;

public class SteganalysisServiceTests : IDisposable
{
    private readonly SteghideTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static UploadedFile Sample(string name)
    {
        var bytes = File.ReadAllBytes(TestPaths.Sample(name));
        return new UploadedFile(name, new MemoryStream(bytes), bytes.Length);
    }

    private async Task<UploadedFile> HideAsync(string cover, string message, string passphrase)
    {
        var stego = await _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample(cover), [], message, new EmbedOptions(passphrase)), CancellationToken.None);
        var directory = _host.Workspace.Find(stego.Id)!;
        var bytes = await directory.ReadAllBytesAsync("stego" + Path.GetExtension(cover), CancellationToken.None);
        return new UploadedFile(stego.FileName, new MemoryStream(bytes), bytes.Length);
    }

    [WindowsFact]
    public async Task Identical_images_have_no_difference()
    {
        var result = await _host.Steganalysis.AnalyzeAsync(new AnalysisCommand(Sample("cover.jpg"), Sample("cover.jpg")), CancellationToken.None);

        result.Psnr.Should().BeNull();
        result.MaxPixelDifference.Should().Be(0);
        result.CoverEntropy.Should().Be(result.StegoEntropy);
    }

    [WindowsFact]
    public async Task Steghide_keeps_first_order_statistics_almost_untouched()
    {
        var stego = await HideAsync("cover.jpg", new string('x', 2000), "abc");

        var result = await _host.Steganalysis.AnalyzeAsync(new AnalysisCommand(Sample("cover.jpg"), stego), CancellationToken.None);

        result.Psnr.Should().BeGreaterThan(35);
        Math.Abs(result.CoverEntropy - result.StegoEntropy).Should().BeLessThan(0.05);
        result.DifferenceImagePng.Take(4).Should().Equal(0x89, 0x50, 0x4E, 0x47);
    }

    [WindowsFact]
    public async Task Audio_files_are_not_supported_by_the_visual_analysis()
    {
        var act = () => _host.Steganalysis.AnalyzeAsync(new AnalysisCommand(Sample("cover.wav"), Sample("cover.wav")), CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedCoverFormatException>();
    }

    [WindowsFact]
    public async Task Recompression_destroys_the_hidden_data()
    {
        var stego = await HideAsync("cover.jpg", "segredo", "abc");

        var result = await _host.Steganalysis.TestRobustnessAsync(new RobustnessCommand(stego, "abc", 85), CancellationToken.None);

        result.Survived.Should().BeFalse();
        result.Quality.Should().Be(85);
        result.RecompressedSizeBytes.Should().BeGreaterThan(0);
    }

    [WindowsFact]
    public async Task Robustness_test_requires_the_right_passphrase()
    {
        var stego = await HideAsync("cover.jpg", "segredo", "abc");

        var act = () => _host.Steganalysis.TestRobustnessAsync(new RobustnessCommand(stego, "wrong", 85), CancellationToken.None);

        await act.Should().ThrowAsync<WrongPassphraseOrNoDataException>();
    }

    [WindowsFact]
    public async Task Robustness_test_is_limited_to_jpeg()
    {
        var act = () => _host.Steganalysis.TestRobustnessAsync(new RobustnessCommand(Sample("cover.bmp"), "", 85), CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedCoverFormatException>();
    }
}
