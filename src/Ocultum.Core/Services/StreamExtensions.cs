namespace Ocultum.Core.Services;

internal static class StreamExtensions
{
    public static async Task<byte[]> ReadAllBytesAsync(this Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}
