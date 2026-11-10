using Microsoft.Extensions.Options;
using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;

namespace Ocultum.Core.Validation;

public sealed class CoverFileValidator(IFileSignatureReader signatures, IOptions<LimitsOptions> limits)
{
    /// <summary>Checks size and magic bytes (not the extension) and leaves the stream at position 0.</summary>
    public async Task<StegoFormat> ValidateAsync(UploadedFile file, string description, CancellationToken cancellationToken)
    {
        if (file.Length > limits.Value.MaxCoverBytes)
        {
            throw new FileTooLargeException(description, file.Length, limits.Value.MaxCoverBytes);
        }

        if (!file.Content.CanSeek)
        {
            throw new InvalidOperationException("Uploaded files must be seekable.");
        }

        file.Content.Position = 0;
        var header = new byte[FileSignatureReader.HeaderLength];
        var read = await file.Content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        file.Content.Position = 0;

        var format = signatures.Detect(header.AsSpan(0, read));
        return format ?? throw new UnsupportedCoverFormatException();
    }
}
