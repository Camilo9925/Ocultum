namespace Ocultum.Web.Hosting;

public sealed class ServerOptions
{
    public const string SectionName = "Server";

    public int Port { get; set; } = 5080;

    /// <summary>How many consecutive ports are tried when the configured one is taken.</summary>
    public int PortSearchRange { get; set; } = 20;

    public bool OpenBrowser { get; set; } = true;
}
