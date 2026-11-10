namespace Ocultum.Core.Errors;

public sealed class WrongPassphraseOrNoDataException()
    : OcultumException(
        "WRONG_PASSPHRASE_OR_NO_DATA",
        "Could not extract any data with that passphrase.",
        "Steghide reports the same error for a wrong passphrase and for a file that hides nothing.");
