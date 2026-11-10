namespace Ocultum.Web.ViewModels;

public sealed record EmbedPageViewModel(long MaxCoverBytes, long MaxPayloadBytes, int MaxPayloadFiles, int MaxPassphraseLength);
