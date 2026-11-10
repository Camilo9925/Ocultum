namespace Ocultum.Core.Errors;

public sealed class SteghideUnavailableException(string message, string? detail = null, Exception? inner = null)
    : OcultumException("STEGHIDE_UNAVAILABLE", message, detail, inner);
