namespace Ocultum.Tests.Support;

internal static class TestPaths
{
    private static readonly Lazy<string> RepoRootLazy = new(FindRepoRoot);

    public static string RepoRoot => RepoRootLazy.Value;

    public static string SteghideExecutable => Path.Combine(RepoRoot, "tools", "steghide", "steghide.exe");

    public static string Sample(string name) => Path.Combine(RepoRoot, "samples", name);

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Ocultum.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
