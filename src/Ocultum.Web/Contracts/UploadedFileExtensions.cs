using Ocultum.Core.Models;

namespace Ocultum.Web.Contracts;

internal static class UploadedFileExtensions
{
    public static UploadedFile ToUploadedFile(this IFormFile file) =>
        new(file.FileName, file.OpenReadStream(), file.Length);
}
