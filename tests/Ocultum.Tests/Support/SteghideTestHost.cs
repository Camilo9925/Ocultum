using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ocultum.Core.Services;
using Ocultum.Core.Validation;
using Ocultum.Infrastructure.Steghide;
using Ocultum.Infrastructure.Imaging;
using Ocultum.Infrastructure.Storage;

namespace Ocultum.Tests.Support;

/// <summary>Wires the real services against the real Steghide executable, with an isolated workspace root.</summary>
internal sealed class SteghideTestHost : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "OcultumTests", Guid.NewGuid().ToString("N"));

    public SteghideTestHost(LimitsOptions? limits = null)
    {
        Limits = Options.Create(limits ?? new LimitsOptions());
        Client = new SteghideClient(
            Options.Create(new SteghideOptions { ExecutablePath = TestPaths.SteghideExecutable }),
            NullLogger<SteghideClient>.Instance);
        Workspace = new TemporaryWorkspace(
            Options.Create(new WorkspaceOptions { RootPath = _root, TtlMinutes = 5 }),
            TimeProvider.System,
            NullLogger<TemporaryWorkspace>.Instance);

        var signatures = new FileSignatureReader();
        var coverValidator = new CoverFileValidator(signatures, Limits);
        var passphraseValidator = new PassphraseValidator(Limits);
        Ciphers = new CipherCatalogService(Client);

        Inspection = new CoverInspectionService(Client, Workspace, coverValidator);
        Embedding = new EmbeddingService(Client, Workspace, Ciphers, coverValidator, passphraseValidator, Limits);
        Extraction = new ExtractionService(Client, Workspace, coverValidator, passphraseValidator);
        Steganalysis = new SteganalysisService(Client, Workspace, new ImageAnalysisService(), coverValidator, passphraseValidator);
    }

    public IOptions<LimitsOptions> Limits { get; }

    public SteghideClient Client { get; }

    public TemporaryWorkspace Workspace { get; }

    public CipherCatalogService Ciphers { get; }

    public CoverInspectionService Inspection { get; }

    public EmbeddingService Embedding { get; }

    public ExtractionService Extraction { get; }

    public SteganalysisService Steganalysis { get; }

    public void Dispose()
    {
        Client.Dispose();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temp folder.
        }
    }
}
