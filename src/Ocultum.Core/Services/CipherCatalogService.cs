using Ocultum.Core.Abstractions;
using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public sealed class CipherCatalogService(ISteghideClient steghide) : ICipherCatalogService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<CipherInfo>? _cache;

    public async Task<IReadOnlyList<CipherInfo>> GetCiphersAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            return _cache ??= await steghide.GetCiphersAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
