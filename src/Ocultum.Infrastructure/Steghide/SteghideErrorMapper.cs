using Ocultum.Core.Errors;
using Ocultum.Core.Models;

namespace Ocultum.Infrastructure.Steghide;

/// <summary>Turns Steghide failures into typed exceptions. Matching is done on the English (<c>LANG=C</c>) messages.</summary>
public static class SteghideErrorMapper
{
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

        var detail = result.StdErr.Trim();
        return new SteghideExecutionException(
            "Steghide failed.",
            detail.Length > 0 ? detail : $"Exit code {result.ExitCode}.");
    }
}
