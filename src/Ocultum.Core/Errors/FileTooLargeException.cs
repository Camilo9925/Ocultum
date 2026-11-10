using Ocultum.Core.Models;

namespace Ocultum.Core.Errors;

public sealed class FileTooLargeException(string what, long actualBytes, long limitBytes)
    : OcultumException(
        "FILE_TOO_LARGE",
        "The file is larger than the allowed limit.",
        $"{what} is {ByteSize.Format(actualBytes)} and the limit is {ByteSize.Format(limitBytes)}.");
