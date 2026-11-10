namespace Ocultum.Core.Errors;

public sealed class EmptyPayloadException()
    : OcultumException("EMPTY_PAYLOAD", "There is nothing to hide.", "Provide a non-empty message or at least one non-empty file.");
