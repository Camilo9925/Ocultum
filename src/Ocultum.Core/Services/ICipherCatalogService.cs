using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public interface ICipherCatalogService
{
    Task<IReadOnlyList<CipherInfo>> GetCiphersAsync(CancellationToken cancellationToken);
}
