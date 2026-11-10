namespace Ocultum.Core.Models;

/// <param name="CommandLine">Command as shown to the user, with the passphrase masked.</param>
public sealed record SteghideCommandResult(int ExitCode, string StdOut, string StdErr, string CommandLine);
