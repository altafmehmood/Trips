using Microsoft.Extensions.DependencyInjection;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Services;

namespace PhotoAIExtractor.Configuration;

/// <summary>
/// Extension methods for configuring services in the DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services with the DI container
    /// </summary>
    public static IServiceCollection AddPhotoAIExtractorServices(this IServiceCollection services)
    {
        // Register configuration settings as singletons
        services.AddSingleton(GeocodingSettings.Default);
        services.AddSingleton(FileSettings.Default);
        services.AddSingleton(OutputSettings.Default);
        services.AddSingleton(ImageOptimizationSettings.Default);

        // Register application services
        services.AddSingleton<IGeocodingService, GeocodingService>();
        services.AddSingleton<IPhotoMetadataExtractor, PhotoMetadataExtractor>();
        services.AddSingleton<IImageOptimizer, ImageOptimizer>();
        services.AddSingleton<IPhotoProcessor, PhotoProcessor>();
        services.AddSingleton<IOutputWriter, JsonOutputWriter>();

        return services;
    }
}
