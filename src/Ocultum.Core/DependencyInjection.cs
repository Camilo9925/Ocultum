using Microsoft.Extensions.DependencyInjection;
using Ocultum.Core.Abstractions;
using Ocultum.Core.Services;
using Ocultum.Core.Validation;

namespace Ocultum.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddOcultumCore(this IServiceCollection services)
    {
        services.AddSingleton<IFileSignatureReader, FileSignatureReader>();
        services.AddSingleton<CoverFileValidator>();
        services.AddSingleton<PassphraseValidator>();
        services.AddSingleton<ICipherCatalogService, CipherCatalogService>();
        services.AddScoped<ICoverInspectionService, CoverInspectionService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IExtractionService, ExtractionService>();
        services.AddScoped<ISteganalysisService, SteganalysisService>();
        services.AddScoped<IStoredFileService, StoredFileService>();
        return services;
    }
}
