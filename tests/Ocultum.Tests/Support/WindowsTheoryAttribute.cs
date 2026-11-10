namespace Ocultum.Tests.Support;

public sealed class WindowsTheoryAttribute : TheoryAttribute
{
    public WindowsTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Requires the Windows build of Steghide.";
        }
    }
}
