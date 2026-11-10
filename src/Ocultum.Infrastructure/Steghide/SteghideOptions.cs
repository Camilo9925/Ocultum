namespace Ocultum.Infrastructure.Steghide;

public sealed class SteghideOptions
{
    public const string SectionName = "Steghide";

    /// <summary>Absolute path, or a path relative to the application directory.</summary>
    public string ExecutablePath { get; set; } = Path.Combine("tools", "steghide", "steghide.exe");

    public int TimeoutSeconds { get; set; } = 60;

    public int MaxConcurrency { get; set; } = 2;
}
