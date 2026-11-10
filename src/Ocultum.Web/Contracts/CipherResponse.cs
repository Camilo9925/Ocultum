using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

public sealed record CipherResponse(string Name, IReadOnlyList<string> Modes)
{
    public static CipherResponse From(CipherInfo cipher) => new(cipher.Name, cipher.Modes);
}
