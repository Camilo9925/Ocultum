using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocultum.Core.Abstractions;

namespace Ocultum.Infrastructure.Storage;

public sealed class WorkspaceCleanupBackgroundService(
    ITemporaryWorkspace workspace,
    IOptions<WorkspaceOptions> options,
    ILogger<WorkspaceCleanupBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.CleanupIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        Purge();
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                Purge();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private void Purge()
    {
        var removed = workspace.PurgeExpired();
        if (removed > 0)
        {
            logger.LogInformation("Removed {Count} expired workspace(s)", removed);
        }
    }
}
