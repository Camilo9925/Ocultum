using System.Security.Cryptography;
using System.Text;
using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Core.Validation;

namespace Ocultum.Core.Services;

public sealed class ExtractionService(
    ISteghideClient steghide,
    ITemporaryWorkspace workspace,
    CoverFileValidator coverValidator,
    PassphraseValidator passphraseValidator) : IExtractionService
{
    private const int MaxInlineTextBytes = 256 * 1024;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public async Task<ExtractionResult> ExtractAsync(ExtractCommand command, CancellationToken cancellationToken)
    {
        passphraseValidator.Validate(command.Passphrase);
        var format = await coverValidator.ValidateAsync(command.StegoFile, "Stego file", cancellationToken);

        var directory = workspace.Create();
        try
        {
            var stegoFile = WorkspaceFileNames.Stego(format);
            await directory.WriteAsync(stegoFile, command.StegoFile.Content, cancellationToken);

            var info = await steghide.InfoAsync(directory.Path, stegoFile, command.Passphrase, cancellationToken);
            if (info.Embedded is null)
            {
                throw new WrongPassphraseOrNoDataException();
            }

            // The output name is always ours: the name stored in the file is attacker-controlled.
            var run = await steghide.ExtractAsync(directory.Path, stegoFile, WorkspaceFileNames.Extracted, command.Passphrase, cancellationToken);

            var content = await directory.ReadAllBytesAsync(WorkspaceFileNames.Extracted, cancellationToken);
            directory.Delete(stegoFile);

            var (kind, text) = Classify(content);
            var fileName = ResolveFileName(info.Embedded.FileName, kind);
            var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));

            await directory.WriteMetadataAsync(
                new StoredFileMetadata(StoredFileKinds.Extraction, WorkspaceFileNames.Extracted, fileName, "application/octet-stream", content.Length, sha256),
                cancellationToken);

            return new ExtractionResult(
                directory.Id,
                kind,
                text,
                fileName,
                content.Length,
                sha256,
                info.Embedded.IsCompressed,
                info.Embedded.Encryption?.Replace(", ", " ") ?? "none",
                directory.ExpiresAt,
                run.CommandLine);
        }
        catch
        {
            workspace.Remove(directory.Id);
            throw;
        }
    }

    private static (PayloadKind Kind, string? Text) Classify(byte[] content)
    {
        if (content.Length >= 4 && content[0] == 'P' && content[1] == 'K' && content[2] == 3 && content[3] == 4)
        {
            return (PayloadKind.Archive, null);
        }

        if (content.Length > 0 && content.Length <= MaxInlineTextBytes && TryDecodeText(content, out var text))
        {
            return (PayloadKind.Text, text);
        }

        return (PayloadKind.File, null);
    }

    private static bool TryDecodeText(byte[] content, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(content);
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }

        return !text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t'));
    }

    private static string ResolveFileName(string? embeddedName, PayloadKind kind)
    {
        var fallback = kind switch
        {
            PayloadKind.Text => "message.txt",
            PayloadKind.Archive => "files.zip",
            _ => "extracted.bin",
        };

        return FileNameSanitizer.Sanitize(embeddedName, fallback);
    }
}
