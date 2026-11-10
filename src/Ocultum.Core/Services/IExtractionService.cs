using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public interface IExtractionService
{
    Task<ExtractionResult> ExtractAsync(ExtractCommand command, CancellationToken cancellationToken);
}
