using Microsoft.Extensions.Options;
using Ocultum.Core.Errors;

namespace Ocultum.Core.Validation;

public sealed class PassphraseValidator(IOptions<LimitsOptions> limits)
{
    /// <summary>
    /// The Cygwin build of Steghide reads its command line through the ANSI code page. Latin-1 characters survive
    /// (verified against the real executable), but anything outside it is silently turned into <c>?</c>,
    /// which would make different passphrases equivalent.
    /// </summary>
    public void Validate(string passphrase)
    {
        if (passphrase.Length > limits.Value.MaxPassphraseLength)
        {
            throw new InvalidInputException(
                "The passphrase is too long.",
                $"The maximum length is {limits.Value.MaxPassphraseLength} characters.");
        }

        foreach (var c in passphrase)
        {
            if (char.IsControl(c) || c > 'ÿ')
            {
                throw new InvalidInputException(
                    "The passphrase contains unsupported characters.",
                    "Use only Latin letters (accents are fine), digits, spaces and common punctuation.");
            }
        }
    }
}
