namespace Ocultum.Core.Models;

public sealed record RobustnessCommand(UploadedFile StegoFile, string Passphrase, int Quality);
