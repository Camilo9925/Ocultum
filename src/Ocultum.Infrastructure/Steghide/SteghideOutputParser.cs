using System.Globalization;
using System.Text.RegularExpressions;
using Ocultum.Core.Models;

namespace Ocultum.Infrastructure.Steghide;

/// <summary>Parses the plain-text output of <c>steghide info</c> and <c>steghide encinfo</c> (run with <c>LANG=C</c>).</summary>
public static partial class SteghideOutputParser
{
    public static bool TryParseInfo(string stdout, out string formatDescription, out long capacityBytes, out EmbeddedDataInfo? embedded)
    {
        formatDescription = string.Empty;
        capacityBytes = 0;
        embedded = null;

        var format = FormatRegex().Match(stdout);
        var capacity = CapacityRegex().Match(stdout);
        if (!format.Success || !capacity.Success)
        {
            return false;
        }

        formatDescription = format.Groups[1].Value.Trim();
        capacityBytes = ToBytes(capacity.Groups[1].Value, capacity.Groups[2].Value);

        var header = EmbeddedHeaderRegex().Match(stdout);
        if (header.Success)
        {
            var section = stdout[header.Index..];
            var size = SizeRegex().Match(section);
            var encrypted = EncryptedRegex().Match(section);
            var compressed = CompressedRegex().Match(section);

            var name = header.Groups[1].Success ? header.Groups[1].Value : null;
            var sizeBytes = size.Success ? ToBytes(size.Groups[1].Value, size.Groups[2].Value) : 0;
            var encryption = encrypted.Success && !encrypted.Groups[1].Value.Trim().Equals("no", StringComparison.OrdinalIgnoreCase)
                ? encrypted.Groups[1].Value.Trim()
                : null;
            var isCompressed = compressed.Success && compressed.Groups[1].Value.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase);

            embedded = new EmbeddedDataInfo(name, sizeBytes, encryption, isCompressed);
        }

        return true;
    }

    public static IReadOnlyList<CipherInfo> ParseCiphers(string stdout)
    {
        var ciphers = new List<CipherInfo>();
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = CipherLineRegex().Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            var modes = match.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            ciphers.Add(new CipherInfo(match.Groups[1].Value, modes));
        }

        return ciphers;
    }

    private static long ToBytes(string number, string unit)
    {
        var value = double.Parse(number, CultureInfo.InvariantCulture);
        var multiplier = unit.ToUpperInvariant() switch
        {
            "KB" => 1024d,
            "MB" => 1024d * 1024,
            "GB" => 1024d * 1024 * 1024,
            _ => 1d,
        };
        return (long)Math.Round(value * multiplier);
    }

    [GeneratedRegex(@"^\s*format:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex FormatRegex();

    [GeneratedRegex(@"^\s*capacity:\s*([\d.]+)\s*(Bytes?|KB|MB|GB)", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex CapacityRegex();

    [GeneratedRegex(@"^\s*embedded (?:file ""(.*)""|data):", RegexOptions.Multiline)]
    private static partial Regex EmbeddedHeaderRegex();

    [GeneratedRegex(@"^\s*size:\s*([\d.]+)\s*(Bytes?|KB|MB|GB)", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"^\s*encrypted:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex EncryptedRegex();

    [GeneratedRegex(@"^\s*compressed:\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex CompressedRegex();

    [GeneratedRegex(@"^([a-z0-9-]+):\s+([a-z0-9 ]+)$")]
    private static partial Regex CipherLineRegex();
}
