using Ocultum.Core.Models;

namespace Ocultum.Core.Abstractions;

public interface IFileSignatureReader
{
    /// <summary>Identifies a supported cover format from the first bytes of a file; null when unknown.</summary>
    StegoFormat? Detect(ReadOnlySpan<byte> header);
}
