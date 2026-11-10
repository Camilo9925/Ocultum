namespace Ocultum.Core.Errors;

public sealed class ResourceNotFoundException()
    : OcultumException("NOT_FOUND", "The resource does not exist or has expired.");
