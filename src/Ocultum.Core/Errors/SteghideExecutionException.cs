namespace Ocultum.Core.Errors;

public sealed class SteghideExecutionException(string message, string? detail = null, Exception? inner = null)
    : OcultumException("STEGHIDE_FAILURE", message, detail, inner);
