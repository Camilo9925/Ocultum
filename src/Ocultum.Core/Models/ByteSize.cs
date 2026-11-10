using System.Globalization;

namespace Ocultum.Core.Models;

public static class ByteSize
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB"];

    public static string Format(long bytes)
    {
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var text = unit == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.0", CultureInfo.InvariantCulture);
        return $"{text} {Units[unit]}";
    }
}
