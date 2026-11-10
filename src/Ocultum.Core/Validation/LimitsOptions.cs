namespace Ocultum.Core.Validation;

public sealed class LimitsOptions
{
    public const string SectionName = "Limits";

    public long MaxCoverBytes { get; set; } = 20 * 1024 * 1024;

    public long MaxPayloadBytes { get; set; } = 20 * 1024 * 1024;

    public int MaxPayloadFiles { get; set; } = 20;

    public int MaxPassphraseLength { get; set; } = 128;
}
