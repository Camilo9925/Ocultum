using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public interface IStoredFileService
{
    /// <summary>Opens the content of a stego file or an extraction; throws <c>ResourceNotFoundException</c> when it is unknown, expired or of another kind.</summary>
    Task<StoredFileContent> OpenAsync(string kind, string id, CancellationToken cancellationToken);

    Task DiscardAsync(string kind, string id, CancellationToken cancellationToken);
}
