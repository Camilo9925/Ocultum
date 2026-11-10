namespace Ocultum.Core.Errors;

public sealed class ImageProcessingException(string message, string? detail = null)
    : OcultumException("IMAGE_PROCESSING_FAILED", message, detail);
