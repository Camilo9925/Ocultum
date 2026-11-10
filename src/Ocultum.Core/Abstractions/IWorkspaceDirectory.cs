using Ocultum.Core.Models;

namespace Ocultum.Core.Abstractions;

public interface IWorkspaceDirectory
{
    string Id { get; }

    /// <summary>Absolute path, used as the working directory of the Steghide process.</summary>
    string Path { get; }

    DateTimeOffset ExpiresAt { get; }

    Task WriteAsync(string fileName, Stream content, CancellationToken cancellationToken);

    Task WriteAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    Stream OpenRead(string fileName);

    Task<byte[]> ReadAllBytesAsync(string fileName, CancellationToken cancellationToken);

    bool Exists(string fileName);

    long GetLength(string fileName);

    void Delete(string fileName);

    Task WriteMetadataAsync(StoredFileMetadata metadata, CancellationToken cancellationToken);

    Task<StoredFileMetadata?> ReadMetadataAsync(CancellationToken cancellationToken);
}
