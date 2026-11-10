using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Infrastructure.Steghide;

namespace Ocultum.Tests.Unit;

public class SteghideErrorMapperTests
{
    private static SteghideCommandResult Failure(string stderr, string stdout = "") =>
        new(1, stdout, stderr, "steghide");

    [Fact]
    public void Maps_wrong_passphrase() =>
        SteghideErrorMapper.Map(Failure("steghide: could not extract any data with that passphrase!"))
            .Should().BeOfType<WrongPassphraseOrNoDataException>();

    [Fact]
    public void Maps_capacity_exceeded() =>
        SteghideErrorMapper.Map(Failure("steghide: the cover file is too short to embed the data."))
            .Should().BeOfType<CapacityExceededException>();

    [Fact]
    public void Maps_unsupported_format() =>
        SteghideErrorMapper.Map(Failure("steghide: the file format of the file \"fake.jpg\" is not supported."))
            .Should().BeOfType<UnsupportedCoverFormatException>();

    [Fact]
    public void Maps_anything_else_to_a_generic_failure()
    {
        var error = SteghideErrorMapper.Map(Failure("steghide: could not open the file \"nofile.jpg\"."));

        error.Should().BeOfType<SteghideExecutionException>();
        error.Detail.Should().Contain("nofile.jpg");
    }
}
