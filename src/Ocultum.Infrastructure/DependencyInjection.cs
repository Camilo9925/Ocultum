using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocultum.Core.Abstractions;
using Ocultum.Infrastructure.Imaging;
using Ocultum.Infrastructure.Steghide;
using Ocultum.Infrastructure.Storage;

namespace Ocultum.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOcultumInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SteghideOptions>(configuration.GetSection(SteghideOptions.SectionName));
        services.Configure<WorkspaceOptions>(configuration.GetSection(WorkspaceOptions.SectionName));

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ISteghideClient, SteghideClient>();
        services.AddSingleton<ITemporaryWorkspace, TemporaryWorkspace>();
        services.AddSingleton<IImageAnalyzer, ImageAnalysisService>();
        services.AddHostedService<WorkspaceCleanupBackgroundService>();
        return services;
    }
}
