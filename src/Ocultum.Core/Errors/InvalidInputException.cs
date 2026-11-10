namespace Ocultum.Core.Errors;

public sealed class InvalidInputException(string message, string? detail = null)
    : OcultumException("INVALID_INPUT", message, detail);
