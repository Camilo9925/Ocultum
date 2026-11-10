namespace Ocultum.Infrastructure.Storage;

public sealed class WorkspaceOptions
{
    public const string SectionName = "Workspace";

    /// <summary>Null uses a folder named Ocultum inside the system temp directory.</summary>
    public string? RootPath { get; set; }

    public int TtlMinutes { get; set; } = 30;

    public int CleanupIntervalMinutes { get; set; } = 5;
}
