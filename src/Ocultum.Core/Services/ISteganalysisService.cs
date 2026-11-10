using Ocultum.Core.Models;

namespace Ocultum.Core.Services;

public interface ISteganalysisService
{
    Task<ImageComparison> AnalyzeAsync(AnalysisCommand command, CancellationToken cancellationToken);

    Task<RobustnessResult> TestRobustnessAsync(RobustnessCommand command, CancellationToken cancellationToken);
}
