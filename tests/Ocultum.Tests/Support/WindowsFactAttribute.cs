namespace Ocultum.Tests.Support;

/// <summary>Integration tests run the real Windows build of Steghide.</summary>
public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Requires the Windows build of Steghide.";
        }
    }
}
