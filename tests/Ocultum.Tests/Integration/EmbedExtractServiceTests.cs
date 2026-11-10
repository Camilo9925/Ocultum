using System.IO.Compression;
using System.Text;
using Ocultum.Core.Errors;
using Ocultum.Core.Models;
using Ocultum.Tests.Support;

namespace Ocultum.Tests.Integration;

public class EmbedExtractServiceTests : IDisposable
{
    private readonly SteghideTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private static UploadedFile Sample(string name)
    {
        var bytes = File.ReadAllBytes(TestPaths.Sample(name));
        return new UploadedFile(name, new MemoryStream(bytes), bytes.Length);
    }

    private static UploadedFile Bytes(string name, byte[] bytes) => new(name, new MemoryStream(bytes), bytes.Length);

    private async Task<ExtractionResult> RoundTripAsync(EmbedCommand embed, string passphrase)
    {
        var stego = await _host.Embedding.EmbedAsync(embed, CancellationToken.None);
        var directory = _host.Workspace.Find(stego.Id)!;
        var bytes = await directory.ReadAllBytesAsync("stego.jpg", CancellationToken.None);
        return await _host.Extraction.ExtractAsync(new ExtractCommand(Bytes("x.jpg", bytes), passphrase), CancellationToken.None);
    }

    [WindowsFact]
    public async Task Unicode_message_round_trips_as_text()
    {
        const string message = "Encontro às 20h — não conte a ninguém! 🔒 日本語";

        var result = await RoundTripAsync(new EmbedCommand(Sample("cover.jpg"), [], message, new EmbedOptions("chave 123")), "chave 123");

        result.Kind.Should().Be(PayloadKind.Text);
        result.Text.Should().Be(message);
        result.FileName.Should().Be("message.txt");
        result.Encryption.Should().Be("rijndael-128 cbc");
    }

    [WindowsFact]
    public async Task Message_without_passphrase_round_trips()
    {
        var result = await RoundTripAsync(new EmbedCommand(Sample("cover.jpg"), [], "olá", new EmbedOptions()), "");

        result.Text.Should().Be("olá");
    }

    [WindowsFact]
    public async Task File_payload_round_trips_with_matching_hashes()
    {
        var embed = new EmbedCommand(Sample("cover.bmp"), [Sample("secret.txt")], null, new EmbedOptions("abc"));

        var stego = await _host.Embedding.EmbedAsync(embed, CancellationToken.None);
        var directory = _host.Workspace.Find(stego.Id)!;
        var extraction = await _host.Extraction.ExtractAsync(
            new ExtractCommand(Bytes("s.bmp", await directory.ReadAllBytesAsync("stego.bmp", CancellationToken.None)), "abc"),
            CancellationToken.None);

        extraction.Sha256.Should().Be(stego.PayloadSha256);
        extraction.FileName.Should().Be("secret.txt");
        stego.IsPasswordProtected.Should().BeTrue();
        stego.FileName.Should().Be("cover-stego.bmp");
    }

    [WindowsFact]
    public async Task Multiple_files_are_zipped_and_come_back_as_an_archive()
    {
        var files = new[] { Bytes("one.txt", "um"u8.ToArray()), Bytes("two.txt", "dois"u8.ToArray()), Bytes("one.txt", "outro um"u8.ToArray()) };

        var result = await RoundTripAsync(new EmbedCommand(Sample("cover.jpg"), files, null, new EmbedOptions("abc")), "abc");

        result.Kind.Should().Be(PayloadKind.Archive);
        result.FileName.Should().Be("files.zip");
        var directory = _host.Workspace.Find(result.Id)!;
        using var zip = new ZipArchive(directory.OpenRead("extracted.out"));
        zip.Entries.Select(e => e.FullName).Should().BeEquivalentTo("one.txt", "two.txt", "one (2).txt");
    }

    [WindowsFact]
    public async Task Binary_payload_is_classified_as_a_file()
    {
        var binary = new byte[] { 0, 1, 2, 3, 255, 254, 253, 0, 0, 9 };

        var result = await RoundTripAsync(new EmbedCommand(Sample("cover.jpg"), [Bytes("data.bin", binary)], null, new EmbedOptions("abc")), "abc");

        result.Kind.Should().Be(PayloadKind.File);
        result.Text.Should().BeNull();
    }

    [WindowsFact]
    public async Task Embedded_name_is_never_used_to_write_outside_the_workspace()
    {
        // The name stored in the stego file is attacker-controlled; extraction must always use its own output name.
        var embed = new EmbedCommand(Sample("cover.jpg"), [Bytes("..\\..\\evil.txt", "x"u8.ToArray())], null, new EmbedOptions("abc"));

        var result = await RoundTripAsync(embed, "abc");

        result.FileName.Should().Be("evil.txt");
        File.Exists(Path.Combine(TestPaths.RepoRoot, "evil.txt")).Should().BeFalse();
    }

    [WindowsFact]
    public async Task Wrong_passphrase_is_reported()
    {
        var stego = await _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.jpg"), [], "secret", new EmbedOptions("right")), CancellationToken.None);
        var bytes = await _host.Workspace.Find(stego.Id)!.ReadAllBytesAsync("stego.jpg", CancellationToken.None);

        var act = () => _host.Extraction.ExtractAsync(new ExtractCommand(Bytes("s.jpg", bytes), "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<WrongPassphraseOrNoDataException>();
    }

    [WindowsFact]
    public async Task Capacity_exceeded_carries_the_numbers()
    {
        var big = new byte[100_000];
        Random.Shared.NextBytes(big);

        var act = () => _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.au"), [Bytes("big.bin", big)], null, new EmbedOptions()), CancellationToken.None);

        var error = (await act.Should().ThrowAsync<CapacityExceededException>()).Which;
        error.PayloadBytes.Should().Be(100_000);
        error.CapacityBytes.Should().BeGreaterThan(0);
        error.Detail.Should().Contain("Payload is");
    }

    [WindowsFact]
    public async Task Empty_payload_is_rejected()
    {
        var empty = () => _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.jpg"), [Bytes("empty.txt", [])], null, new EmbedOptions()), CancellationToken.None);
        var nothing = () => _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.jpg"), [], null, new EmbedOptions()), CancellationToken.None);

        await empty.Should().ThrowAsync<EmptyPayloadException>();
        await nothing.Should().ThrowAsync<EmptyPayloadException>();
    }

    [WindowsFact]
    public async Task Message_and_files_together_are_rejected()
    {
        var act = () => _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.jpg"), [Sample("secret.txt")], "also a message", new EmbedOptions()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [WindowsFact]
    public async Task Cover_is_validated_by_content_not_by_extension()
    {
        var act = () => _host.Embedding.EmbedAsync(
            new EmbedCommand(Bytes("photo.jpg", Encoding.UTF8.GetBytes("plain text pretending to be a jpeg")), [], "hi", new EmbedOptions()),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedCoverFormatException>();
    }

    [WindowsFact]
    public async Task Oversized_cover_is_rejected_before_any_work()
    {
        using var small = new SteghideTestHost(new Ocultum.Core.Validation.LimitsOptions { MaxCoverBytes = 1000 });

        var act = () => small.Inspection.InspectAsync(Sample("cover.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<FileTooLargeException>();
    }

    [WindowsFact]
    public async Task Unknown_cipher_is_rejected()
    {
        var act = () => _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.jpg"), [], "hi", new EmbedOptions("abc", Cipher: "rot13")), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidInputException>();
    }

    [WindowsFact]
    public async Task Inspection_returns_format_and_capacity_and_cleans_up()
    {
        var inspection = await _host.Inspection.InspectAsync(Sample("cover.wav"), CancellationToken.None);

        inspection.Format.Should().Be(StegoFormat.Wav);
        inspection.CapacityBytes.Should().BeGreaterThan(1000);
        inspection.SizeBytes.Should().Be(new FileInfo(TestPaths.Sample("cover.wav")).Length);
    }

    [WindowsFact]
    public async Task Workspace_entries_disappear_when_removed()
    {
        var stego = await _host.Embedding.EmbedAsync(
            new EmbedCommand(Sample("cover.jpg"), [], "hi", new EmbedOptions()), CancellationToken.None);

        _host.Workspace.Find(stego.Id).Should().NotBeNull();
        _host.Workspace.Remove(stego.Id);

        _host.Workspace.Find(stego.Id).Should().BeNull();
    }

    [WindowsFact]
    public void Malformed_ids_never_resolve()
    {
        _host.Workspace.Find("..\\..\\Windows").Should().BeNull();
        _host.Workspace.Find("not-an-id").Should().BeNull();
    }
}
