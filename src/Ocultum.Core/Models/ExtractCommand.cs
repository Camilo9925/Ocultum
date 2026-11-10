namespace Ocultum.Core.Models;

public sealed record ExtractCommand(UploadedFile StegoFile, string Passphrase);
