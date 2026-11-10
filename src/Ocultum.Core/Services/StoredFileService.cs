using Ocultum.Core.Abstractions;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public sealed class StoredFileService(ITemporaryWorkspace workspace) : IStoredFileService
{
    public async Task<StoredFileContent> OpenAsync(string kind, string id, CancellationToken cancellationToken)
    {
        var (directory, metadata) = await FindAsync(kind, id, cancellationToken);
        return new StoredFileContent(directory.OpenRead(metadata.ContentFile), metadata.FileName, metadata.ContentType, metadata.SizeBytes);
    }

    public async Task DiscardAsync(string kind, string id, CancellationToken cancellationToken)
    {
        var (directory, _) = await FindAsync(kind, id, cancellationToken);
        workspace.Remove(directory.Id);
    }

    private async Task<(IWorkspaceDirectory Directory, StoredFileMetadata Metadata)> FindAsync(
        string kind, string id, CancellationToken cancellationToken)
    {
        var directory = workspace.Find(id) ?? throw new ResourceNotFoundException();
        var metadata = await directory.ReadMetadataAsync(cancellationToken);
        if (metadata is null || metadata.Kind != kind)
        {
            throw new ResourceNotFoundException();
        }

        return (directory, metadata);
    }
}
