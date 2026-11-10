using System.Text;

namespace Ocultum.Core.Validation;

public static class FileNameSanitizer
{
    private const int MaxLength = 100;

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Strips any directory part and unsafe characters; never returns an empty or reserved name.</summary>
    public static string Sanitize(string? name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return fallback;
        }

        var lastSeparator = name.LastIndexOfAny(['/', '\\']);
        var baseName = lastSeparator >= 0 ? name[(lastSeparator + 1)..] : name;

        var builder = new StringBuilder(baseName.Length);
        foreach (var c in baseName)
        {
            if (char.IsControl(c) || "<>:\"|?*".Contains(c))
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(c);
            }
        }

        var cleaned = builder.ToString().Trim().TrimEnd('.', ' ');
        if (cleaned.Length > MaxLength)
        {
            var extension = Path.GetExtension(cleaned);
            if (extension.Length > 12)
            {
                extension = string.Empty;
            }

            cleaned = cleaned[..(MaxLength - extension.Length)] + extension;
        }

        if (cleaned.Length == 0 || cleaned.All(c => c == '.'))
        {
            return fallback;
        }

        var stem = Path.GetFileNameWithoutExtension(cleaned);
        return ReservedDeviceNames.Contains(stem) ? "_" + cleaned : cleaned;
    }

    /// <summary>Same as <see cref="Sanitize"/> but also replaces everything outside printable ASCII.</summary>
    public static string SanitizeAscii(string? name, string fallback)
    {
        var cleaned = Sanitize(name, fallback);
        var builder = new StringBuilder(cleaned.Length);
        foreach (var c in cleaned)
        {
            builder.Append(c is >= ' ' and <= '~' ? c : '_');
        }

        return builder.ToString();
    }
}
