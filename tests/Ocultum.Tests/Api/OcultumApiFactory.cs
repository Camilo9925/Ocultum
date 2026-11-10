using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Ocultum.Tests.Support;

namespace Ocultum.Tests.Api;

public sealed class OcultumApiFactory : WebApplicationFactory<Program>
{
    private readonly string _workspaceRoot = Path.Combine(Path.GetTempPath(), "OcultumApiTests", Guid.NewGuid().ToString("N"));

    public long? MaxCoverBytes { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Steghide:ExecutablePath", TestPaths.SteghideExecutable);
        builder.UseSetting("Workspace:RootPath", _workspaceRoot);
        if (MaxCoverBytes is { } max)
        {
            builder.UseSetting("Limits:MaxCoverBytes", max.ToString());
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                Directory.Delete(_workspaceRoot, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup of the temp folder.
            }
        }
    }
}
