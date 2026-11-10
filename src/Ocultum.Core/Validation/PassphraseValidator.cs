using Microsoft.Extensions.Options;
using Ocultum.Core.Errors;

namespace Ocultum.Core.Validation;

public sealed class PassphraseValidator(IOptions<LimitsOptions> limits)
{
    /// <summary>
    /// The Cygwin build of Steghide reads its command line through the ANSI code page, so characters outside
    /// printable ASCII can be silently turned into <c>?</c>, making different passphrases equivalent.
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
            if (c is < ' ' or > '~')
            {
                throw new InvalidInputException(
                    "The passphrase contains unsupported characters.",
                    "Use only letters without accents, digits, spaces and common punctuation.");
            }
        }
    }
}
