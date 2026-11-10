namespace Ocultum.Core.Models;

public sealed record UploadedFile(string FileName, Stream Content, long Length);
