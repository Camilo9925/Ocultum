using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Core.Validation;

namespace Ocultum.Core.Services;

public sealed class EmbeddingService(
    ISteghideClient steghide,
    ITemporaryWorkspace workspace,
    ICipherCatalogService cipherCatalog,
    CoverFileValidator coverValidator,
    PassphraseValidator passphraseValidator,
    IOptions<LimitsOptions> limits) : IEmbeddingService
{
    private const string MessageFileName = "message.txt";
    private const string ArchiveFileName = "files.zip";

    public async Task<StegoFileResult> EmbedAsync(EmbedCommand command, CancellationToken cancellationToken)
    {
        await ValidateOptionsAsync(command.Options, cancellationToken);

        var hasMessage = !string.IsNullOrEmpty(command.Message);
        if (hasMessage && command.PayloadFiles.Count > 0)
        {
            throw new InvalidInputException("Provide either a message or files, not both.");
        }

        if (!hasMessage && command.PayloadFiles.Count == 0)
        {
            throw new EmptyPayloadException();
        }

        if (command.PayloadFiles.Count > limits.Value.MaxPayloadFiles)
        {
            throw new InvalidInputException(
                "Too many files.",
                $"At most {limits.Value.MaxPayloadFiles} files can be hidden at once.");
        }

        var format = await coverValidator.ValidateAsync(command.Cover, "Cover file", cancellationToken);
        var payload = await BuildPayloadAsync(command, hasMessage, cancellationToken);
        if (payload.Content.Length == 0)
        {
            throw new EmptyPayloadException();
        }

        var directory = workspace.Create();
        try
        {
            var coverFile = WorkspaceFileNames.Cover(format);
            var stegoFile = WorkspaceFileNames.Stego(format);
            var payloadFile = command.Options.EmbedFileName
                ? PayloadFileName(payload.FileName)
                : WorkspaceFileNames.FallbackPayload;

            await directory.WriteAsync(coverFile, command.Cover.Content, cancellationToken);
            await directory.WriteAsync(payloadFile, payload.Content, cancellationToken);

            var coverInfo = await steghide.InfoAsync(directory.Path, coverFile, string.Empty, cancellationToken);

            SteghideCommandResult run;
            try
            {
                run = await steghide.EmbedAsync(directory.Path, coverFile, payloadFile, stegoFile, command.Options, cancellationToken);
            }
            catch (CapacityExceededException)
            {
                throw new CapacityExceededException(payload.Content.Length, coverInfo.CapacityBytes);
            }

            var size = directory.GetLength(stegoFile);
            string sha256;
            await using (var stream = directory.OpenRead(stegoFile))
            {
                sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
            }

            directory.Delete(coverFile);
            directory.Delete(payloadFile);

            var downloadName = FileNameSanitizer.Sanitize(
                Path.GetFileNameWithoutExtension(command.Cover.FileName), "cover") + "-stego" + format.FileExtension();

            await directory.WriteMetadataAsync(
                new StoredFileMetadata("stego-file", stegoFile, downloadName, format.ContentType(), size, sha256),
                cancellationToken);

            return new StegoFileResult(
                directory.Id,
                downloadName,
                size,
                sha256,
                command.Options.Passphrase.Length > 0,
                directory.ExpiresAt,
                format,
                coverInfo.CapacityBytes,
                payload.Content.Length,
                Convert.ToHexStringLower(SHA256.HashData(payload.Content)),
                run.CommandLine);
        }
        catch
        {
            workspace.Remove(directory.Id);
            throw;
        }
    }

    private async Task ValidateOptionsAsync(EmbedOptions options, CancellationToken cancellationToken)
    {
        passphraseValidator.Validate(options.Passphrase);

        if (options.CompressionLevel is < 0 or > 9)
        {
            throw new InvalidInputException("Invalid compression level.", "Use a value between 0 and 9.");
        }

        if (options.Cipher is null)
        {
            if (options.Mode is not null)
            {
                throw new InvalidInputException("A cipher mode requires a cipher.");
            }

            return;
        }

        if (options.Cipher.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var ciphers = await cipherCatalog.GetCiphersAsync(cancellationToken);
        var cipher = ciphers.FirstOrDefault(c => c.Name.Equals(options.Cipher, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidInputException("Unknown cipher.", $"\"{options.Cipher}\" is not available in this Steghide build.");

        if (options.Mode is not null && !cipher.Modes.Contains(options.Mode, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidInputException("Unknown cipher mode.", $"\"{options.Mode}\" is not supported by {cipher.Name}.");
        }
    }

    private async Task<Payload> BuildPayloadAsync(EmbedCommand command, bool hasMessage, CancellationToken cancellationToken)
    {
        var maxBytes = limits.Value.MaxPayloadBytes;

        if (hasMessage)
        {
            var bytes = Encoding.UTF8.GetBytes(command.Message!);
            if (bytes.Length > maxBytes)
            {
                throw new FileTooLargeException("The message", bytes.Length, maxBytes);
            }

            return new Payload(MessageFileName, bytes);
        }

        var total = command.PayloadFiles.Sum(f => f.Length);
        if (total > maxBytes)
        {
            throw new FileTooLargeException("The payload", total, maxBytes);
        }

        if (command.PayloadFiles.Count == 1)
        {
            var file = command.PayloadFiles[0];
            return new Payload(
                FileNameSanitizer.Sanitize(file.FileName, "file.bin"),
                await ReadAllAsync(file.Content, cancellationToken));
        }

        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in command.PayloadFiles)
            {
                var entry = archive.CreateEntry(UniqueName(FileNameSanitizer.Sanitize(file.FileName, "file.bin"), used), CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await file.Content.CopyToAsync(entryStream, cancellationToken);
            }
        }

        return new Payload(ArchiveFileName, buffer.ToArray());
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        var candidate = name;
        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        for (var i = 2; !used.Add(candidate); i++)
        {
            candidate = $"{stem} ({i}){extension}";
        }

        return candidate;
    }

    private static string PayloadFileName(string name)
    {
        // Steghide only sees plain ASCII names; the original name is kept in the stego file for convenience only.
        var ascii = FileNameSanitizer.SanitizeAscii(name, WorkspaceFileNames.FallbackPayload);
        return WorkspaceFileNames.IsReserved(ascii) ? "payload-" + ascii : ascii;
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    private sealed record Payload(string FileName, byte[] Content);
}
