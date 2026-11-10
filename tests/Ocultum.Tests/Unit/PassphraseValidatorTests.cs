using Microsoft.Extensions.Options;
using Ocultum.Core.Errors;
using Ocultum.Core.Validation;

namespace Ocultum.Tests.Unit;

public class PassphraseValidatorTests
{
    private readonly PassphraseValidator _validator = new(Options.Create(new LimitsOptions { MaxPassphraseLength = 20 }));

    [Theory]
    [InlineData("")]
    [InlineData("simple")]
    [InlineData("with spaces & symbols!")]
    [InlineData("sênha-ção")]
    public void Accepts_printable_latin1(string passphrase)
    {
        var act = () => _validator.Validate(passphrase[..Math.Min(passphrase.Length, 20)]);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("日本語")]
    [InlineData("euro €")]
    [InlineData("tab\there")]
    public void Rejects_characters_the_cygwin_build_cannot_carry(string passphrase)
    {
        var act = () => _validator.Validate(passphrase);
        act.Should().Throw<InvalidInputException>();
    }

    [Fact]
    public void Rejects_overlong_passphrase()
    {
        var act = () => _validator.Validate(new string('a', 21));
        act.Should().Throw<InvalidInputException>();
    }
}
