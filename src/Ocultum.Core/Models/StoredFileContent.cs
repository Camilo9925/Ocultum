namespace Ocultum.Core.Models;

public sealed record StoredFileContent(Stream Content, string FileName, string ContentType, long Length);
