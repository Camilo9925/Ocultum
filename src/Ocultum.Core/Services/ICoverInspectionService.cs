using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public interface ICoverInspectionService
{
    Task<CoverInspection> InspectAsync(UploadedFile cover, CancellationToken cancellationToken);
}
