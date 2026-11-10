using Ocultum.Core.Abstractions;
using Ocultum.Core.Models;
using Ocultum.Core.Validation;

namespace Ocultum.Core.Services;

public sealed class CoverInspectionService(
    ISteghideClient steghide,
    ITemporaryWorkspace workspace,
    CoverFileValidator validator) : ICoverInspectionService
{
    public async Task<CoverInspection> InspectAsync(UploadedFile cover, CancellationToken cancellationToken)
    {
        var format = await validator.ValidateAsync(cover, "Cover file", cancellationToken);

        var directory = workspace.Create();
        try
        {
            var coverFile = WorkspaceFileNames.Cover(format);
            await directory.WriteAsync(coverFile, cover.Content, cancellationToken);

            var info = await steghide.InfoAsync(directory.Path, coverFile, string.Empty, cancellationToken);
            return new CoverInspection(format, info.FormatDescription, cover.Length, info.CapacityBytes);
        }
        finally
        {
            workspace.Remove(directory.Id);
        }
    }
}
