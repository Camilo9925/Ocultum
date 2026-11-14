using Ocultum.Core.Errors;
using Ocultum.Core.Models;

namespace Ocultum.Infrastructure.Steghide;

/// <summary>Turns Steghide failures into typed exceptions. Matching is done on the English (<c>LANG=C</c>) messages.</summary>
public static class SteghideErrorMapper
{
    private static readonly string[] CrashMarkers =
    [
        "status_stack_overflow",
        "status_access_violation",
        "handle_exceptions",
        "stackdump",
        "assertion failed",
        "premature end of file",
    ];

    public static OcultumException Map(SteghideCommandResult result)
    {
        var output = (result.StdErr + "\n" + result.StdOut).ToLowerInvariant();

        if (output.Contains("could not extract any data with that passphrase"))
        {
            return new WrongPassphraseOrNoDataException();
        }

        if (output.Contains("too short to embed"))
        {
            return new CapacityExceededException();
        }

        if (output.Contains("is not supported") || output.Contains("file format"))
        {
            return new UnsupportedCoverFormatException();
        }

        // The Windows (Cygwin) build of Steghide is 32-bit and aborts on some valid files: it overflows its stack on large
        // covers with big payloads, asserts on 8-bit palette BMPs and chokes on truncated audio. Don't leak the raw stack dump.
        if (CrashMarkers.Any(output.Contains))
        {
            return new CoverNotProcessableException();
        }

        var detail = result.StdErr.Trim();
        return new SteghideExecutionException(
            "Steghide failed.",
            detail.Length > 0 ? detail : $"Exit code {result.ExitCode}.");
    }
}
