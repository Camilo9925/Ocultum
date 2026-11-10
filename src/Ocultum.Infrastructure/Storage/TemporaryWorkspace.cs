using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocultum.Core.Abstractions;

namespace Ocultum.Infrastructure.Storage;

public sealed partial class TemporaryWorkspace : ITemporaryWorkspace
{
    private readonly string _root;
    private readonly TimeSpan _ttl;
    private readonly TimeProvider _clock;
    private readonly ILogger<TemporaryWorkspace> _logger;

    public TemporaryWorkspace(IOptions<WorkspaceOptions> options, TimeProvider clock, ILogger<TemporaryWorkspace> logger)
    {
        var settings = options.Value;
        _root = settings.RootPath is { Length: > 0 } configured
            ? Path.GetFullPath(configured)
            : Path.Combine(Path.GetTempPath(), "Ocultum");
        _ttl = TimeSpan.FromMinutes(settings.TtlMinutes);
        _clock = clock;
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public IWorkspaceDirectory Create()
    {
        var id = Guid.NewGuid().ToString("N");
        var path = Path.Combine(_root, id);
        Directory.CreateDirectory(path);
        return new WorkspaceDirectory(id, path, _clock.GetUtcNow() + _ttl);
    }

    public IWorkspaceDirectory? Find(string id)
    {
        if (!IdRegex().IsMatch(id))
        {
            return null;
        }

        var path = Path.Combine(_root, id);
        if (!Directory.Exists(path))
        {
            return null;
        }

        var expiresAt = new DateTimeOffset(Directory.GetCreationTimeUtc(path), TimeSpan.Zero) + _ttl;
        return expiresAt > _clock.GetUtcNow() ? new WorkspaceDirectory(id, path, expiresAt) : null;
    }

    public void Remove(string id)
    {
        if (!IdRegex().IsMatch(id))
        {
            return;
        }

        TryDelete(Path.Combine(_root, id));
    }

    public int PurgeExpired()
    {
        var removed = 0;
        var now = _clock.GetUtcNow();
        foreach (var directory in Directory.EnumerateDirectories(_root))
        {
            var createdAt = new DateTimeOffset(Directory.GetCreationTimeUtc(directory), TimeSpan.Zero);
            if (createdAt + _ttl <= now && TryDelete(directory))
            {
                removed++;
            }
        }

        return removed;
    }

    private bool TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }

            return true;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not delete workspace {Path}", path);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Could not delete workspace {Path}", path);
            return false;
        }
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex IdRegex();
}
