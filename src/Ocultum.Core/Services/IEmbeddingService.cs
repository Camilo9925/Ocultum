using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public interface IEmbeddingService
{
    Task<StegoFileResult> EmbedAsync(EmbedCommand command, CancellationToken cancellationToken);
}
