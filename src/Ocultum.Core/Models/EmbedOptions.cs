namespace Ocultum.Core.Models;

/// <param name="Passphrase">Empty string means no passphrase.</param>
/// <param name="Cipher">Algorithm name from <c>steghide encinfo</c>, or <c>none</c>. Null keeps the Steghide default.</param>
/// <param name="Mode">Cipher mode; ignored for stream ciphers and when <paramref name="Cipher"/> is null.</param>
/// <param name="CompressionLevel">0 disables compression, 1-9 sets the level. Null keeps the Steghide default.</param>
public sealed record EmbedOptions(
    string Passphrase = "",
    string? Cipher = null,
    string? Mode = null,
    int? CompressionLevel = null,
    bool EmbedChecksum = true,
    bool EmbedFileName = true);
