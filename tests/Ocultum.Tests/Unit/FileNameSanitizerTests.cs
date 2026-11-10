using Ocultum.Core.Validation;

namespace Ocultum.Tests.Unit;

public class FileNameSanitizerTests
{
    [Theory]
    [InlineData("..\\..\\Windows\\win.ini", "win.ini")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("C:\\temp\\report.pdf", "report.pdf")]
    [InlineData("report.pdf", "report.pdf")]
    public void Strips_directories(string input, string expected) =>
        FileNameSanitizer.Sanitize(input, "fallback").Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("...")]
    [InlineData("a/")]
    public void Falls_back_when_nothing_usable_is_left(string? input) =>
        FileNameSanitizer.Sanitize(input, "fallback").Should().Be("fallback");

    [Fact]
    public void Replaces_invalid_characters() =>
        FileNameSanitizer.Sanitize("a<b>c:d\"e|f?g*h.txt", "x").Should().Be("a_b_c_d_e_f_g_h.txt");

    [Theory]
    [InlineData("CON", "_CON")]
    [InlineData("nul.txt", "_nul.txt")]
    public void Prefixes_reserved_device_names(string input, string expected) =>
        FileNameSanitizer.Sanitize(input, "x").Should().Be(expected);

    [Fact]
    public void Limits_length_and_keeps_extension()
    {
        var result = FileNameSanitizer.Sanitize(new string('a', 300) + ".pdf", "x");
        result.Length.Should().BeLessThanOrEqualTo(100);
        result.Should().EndWith(".pdf");
    }

    [Fact]
    public void Ascii_variant_replaces_accents() =>
        FileNameSanitizer.SanitizeAscii("relatório.txt", "x").Should().Be("relat_rio.txt");
}
