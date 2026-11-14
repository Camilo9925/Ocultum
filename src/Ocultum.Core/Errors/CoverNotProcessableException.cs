namespace Ocultum.Core.Errors;

/// <summary>
/// Steghide crashed or aborted on a file that looked valid (very large covers with a heavy payload, 8-bit palette BMPs, truncated audio...).
/// It is a limit of Steghide itself, not a server fault, so it is reported as a client-side 422.
/// </summary>
public sealed class CoverNotProcessableException(string? detail = null)
    : OcultumException(
        "COVER_NOT_PROCESSABLE",
        "Steghide could not process this file.",
        detail ?? "Steghide stopped while handling this cover and content. Try a JPEG, a smaller image, or less content.");
