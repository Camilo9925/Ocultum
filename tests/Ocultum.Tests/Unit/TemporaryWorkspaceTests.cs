using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ocultum.Core.Models;
using Ocultum.Infrastructure.Storage;

namespace Ocultum.Tests.Unit;

public class TemporaryWorkspaceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "OcultumTests", Guid.NewGuid().ToString("N"));
    private readonly FakeClock _clock = new();

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private TemporaryWorkspace CreateWorkspace(int ttlMinutes = 10) => new(
        Options.Create(new WorkspaceOptions { RootPath = _root, TtlMinutes = ttlMinutes }),
        _clock,
        NullLogger<TemporaryWorkspace>.Instance);

    [Fact]
    public void Created_directories_are_isolated_and_named_by_random_ids()
    {
        var workspace = CreateWorkspace();

        var first = workspace.Create();
        var second = workspace.Create();

        first.Id.Should().MatchRegex("^[0-9a-f]{32}$");
        first.Id.Should().NotBe(second.Id);
        Directory.Exists(first.Path).Should().BeTrue();
        first.Path.Should().NotBe(second.Path);
    }

    [Fact]
    public async Task Only_plain_file_names_are_accepted()
    {
        var directory = CreateWorkspace().Create();

        var act = () => directory.WriteAsync("..\\escape.txt", new byte[] { 1 }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Metadata_round_trips()
    {
        var workspace = CreateWorkspace();
        var directory = workspace.Create();
        var metadata = new StoredFileMetadata("stego-file", "stego.jpg", "photo-stego.jpg", "image/jpeg", 10, "abc");

        await directory.WriteMetadataAsync(metadata, CancellationToken.None);

        (await workspace.Find(directory.Id)!.ReadMetadataAsync(CancellationToken.None)).Should().Be(metadata);
    }

    [Fact]
    public void Entries_expire_after_the_ttl_and_are_purged()
    {
        var workspace = CreateWorkspace(ttlMinutes: 10);
        var directory = workspace.Create();
        workspace.Find(directory.Id).Should().NotBeNull();

        _clock.Advance(TimeSpan.FromMinutes(11));

        workspace.Find(directory.Id).Should().BeNull();
        workspace.PurgeExpired().Should().Be(1);
        Directory.Exists(directory.Path).Should().BeFalse();
    }

    [Fact]
    public void Unexpired_entries_survive_a_purge()
    {
        var workspace = CreateWorkspace(ttlMinutes: 10);
        var directory = workspace.Create();
        _clock.Advance(TimeSpan.FromMinutes(5));

        workspace.PurgeExpired().Should().Be(0);

        Directory.Exists(directory.Path).Should().BeTrue();
    }

    /// <summary>Directory creation times come from the real file system, so the fake clock starts at the real "now".</summary>
    private sealed class FakeClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
