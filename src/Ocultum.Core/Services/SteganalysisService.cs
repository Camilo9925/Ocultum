using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Core.Validation;

namespace Ocultum.Core.Services;

public sealed class SteganalysisService(
    ISteghideClient steghide,
    ITemporaryWorkspace workspace,
    IImageAnalyzer analyzer,
    CoverFileValidator coverValidator,
    PassphraseValidator passphraseValidator) : ISteganalysisService
{
    private const string RecompressedFile = "recompressed.jpg";
    private const string ProbeOutput = "probe.out";

    public async Task<ImageComparison> AnalyzeAsync(AnalysisCommand command, CancellationToken cancellationToken)
    {
        var coverFormat = await coverValidator.ValidateAsync(command.Cover, "Cover file", cancellationToken);
        var stegoFormat = await coverValidator.ValidateAsync(command.Stego, "Stego file", cancellationToken);
        if (!coverFormat.IsImage() || !stegoFormat.IsImage())
        {
            throw new UnsupportedCoverFormatException("Visual analysis supports JPEG and BMP images only.");
        }

        var cover = await command.Cover.Content.ReadAllBytesAsync(cancellationToken);
        var stego = await command.Stego.Content.ReadAllBytesAsync(cancellationToken);
        return await Task.Run(() => analyzer.Compare(cover, stego), cancellationToken);
    }

    public async Task<RobustnessResult> TestRobustnessAsync(RobustnessCommand command, CancellationToken cancellationToken)
    {
        passphraseValidator.Validate(command.Passphrase);
        if (command.Quality is < 1 or > 100)
        {
            throw new InvalidInputException("Invalid JPEG quality.", "Use a value between 1 and 100.");
        }

        var format = await coverValidator.ValidateAsync(command.StegoFile, "Stego file", cancellationToken);
        if (format != StegoFormat.Jpeg)
        {
            throw new UnsupportedCoverFormatException("The robustness test supports JPEG files only.");
        }

        var original = await command.StegoFile.Content.ReadAllBytesAsync(cancellationToken);
        var directory = workspace.Create();
        try
        {
            var stegoFile = WorkspaceFileNames.Stego(format);
            await directory.WriteAsync(stegoFile, original, cancellationToken);

            if (!await CanExtractAsync(directory, stegoFile, command.Passphrase, cancellationToken))
            {
                throw new WrongPassphraseOrNoDataException();
            }

            var recompressed = await Task.Run(() => analyzer.RecompressJpeg(original, command.Quality), cancellationToken);
            await directory.WriteAsync(RecompressedFile, recompressed, cancellationToken);

            var survived = await CanExtractAsync(directory, RecompressedFile, command.Passphrase, cancellationToken);
            return new RobustnessResult(command.Quality, original.Length, recompressed.Length, survived);
        }
        finally
        {
            workspace.Remove(directory.Id);
        }
    }

    private async Task<bool> CanExtractAsync(IWorkspaceDirectory directory, string file, string passphrase, CancellationToken cancellationToken)
    {
        try
        {
            await steghide.ExtractAsync(directory.Path, file, ProbeOutput, passphrase, cancellationToken);
            return true;
        }
        catch (WrongPassphraseOrNoDataException)
        {
            return false;
        }
        finally
        {
            if (directory.Exists(ProbeOutput))
            {
                directory.Delete(ProbeOutput);
            }
        }
    }
}
