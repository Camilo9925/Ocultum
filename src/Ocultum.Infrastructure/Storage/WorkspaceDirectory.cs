using System.Text.Json;
using Ocultum.Core.Abstractions;
using Ocultum.Core.Models;

namespace Ocultum.Infrastructure.Storage;

internal sealed class WorkspaceDirectory(string id, string path, DateTimeOffset expiresAt) : IWorkspaceDirectory
{
    private const string MetadataFileName = "meta.json";

    public string Id { get; } = id;

    public string Path { get; } = path;

    public DateTimeOffset ExpiresAt { get; } = expiresAt;

    public async Task WriteAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(Resolve(fileName), FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Task WriteAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken cancellationToken) =>
        File.WriteAllBytesAsync(Resolve(fileName), content.ToArray(), cancellationToken);

    public Stream OpenRead(string fileName) =>
        new FileStream(Resolve(fileName), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

    public Task<byte[]> ReadAllBytesAsync(string fileName, CancellationToken cancellationToken) =>
        File.ReadAllBytesAsync(Resolve(fileName), cancellationToken);

    public bool Exists(string fileName) => File.Exists(Resolve(fileName));

    public long GetLength(string fileName) => new FileInfo(Resolve(fileName)).Length;

    public void Delete(string fileName) => File.Delete(Resolve(fileName));

    public async Task WriteMetadataAsync(StoredFileMetadata metadata, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(Resolve(MetadataFileName), FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await JsonSerializer.SerializeAsync(file, metadata, cancellationToken: cancellationToken);
    }

    public async Task<StoredFileMetadata?> ReadMetadataAsync(CancellationToken cancellationToken)
    {
        var path = Resolve(MetadataFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await JsonSerializer.DeserializeAsync<StoredFileMetadata>(file, cancellationToken: cancellationToken);
    }

    private string Resolve(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) || System.IO.Path.GetFileName(fileName) != fileName)
        {
            throw new ArgumentException("Only plain file names are allowed inside a workspace.", nameof(fileName));
        }

        return System.IO.Path.Combine(Path, fileName);
    }
}
