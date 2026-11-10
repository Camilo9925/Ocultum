namespace Ocultum.Core.Errors;

public sealed class UnsupportedCoverFormatException(string? detail = null)
    : OcultumException("UNSUPPORTED_COVER_FORMAT", "The file format is not supported.", detail ?? "Supported formats are JPEG, BMP, WAV and AU.");
