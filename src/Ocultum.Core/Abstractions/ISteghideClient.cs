using Ocultum.Core.Models;

namespace Ocultum.Core.Abstractions;

/// <summary>Adapter around the Steghide executable. All files are simple names inside <c>workDir</c>.</summary>
public interface ISteghideClient
{
    /// <summary>
    /// Runs <c>steghide info</c>. A wrong passphrase is not an error here: capacity is still returned
    /// and <see cref="SteghideInfo.Embedded"/> is null.
    /// </summary>
    Task<SteghideInfo> InfoAsync(string workDir, string fileName, string passphrase, CancellationToken cancellationToken);

    Task<SteghideCommandResult> EmbedAsync(
        string workDir,
        string coverFile,
        string payloadFile,
        string stegoFile,
        EmbedOptions options,
        CancellationToken cancellationToken);

    Task<SteghideCommandResult> ExtractAsync(
        string workDir,
        string stegoFile,
        string outputFile,
        string passphrase,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CipherInfo>> GetCiphersAsync(CancellationToken cancellationToken);
}
