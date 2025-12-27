using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;

// Configure dependency injection container with logging
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddPhotoAIExtractorServices();
await using var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// Parse command-line arguments
if (args.Length == 0)
{
    ShowUsage();
    return 1;
}

var folderPath = args[0];
var shouldOptimize = args.Contains("--optimize") || args.Contains("-o");
var metadataOnly = args.Contains("--metadata-only") || args.Contains("-m");

try
{
    logger.LogInformation("Processing photos in: {FolderPath}", folderPath);

    // Resolve services from DI container
    var photoProcessor = serviceProvider.GetRequiredService<IPhotoProcessor>();
    var outputSettings = serviceProvider.GetRequiredService<OutputSettings>();

    if (!metadataOnly)
    {
        // Process photos (writes metadata after each photo)
        var photoDataList = await photoProcessor.ProcessPhotosAsync(folderPath);

        var outputPath = Path.Combine(folderPath, outputSettings.OutputFileName);
        logger.LogInformation("Metadata extracted successfully! Output: {OutputPath}, Photos: {Count}",
            outputPath, photoDataList.Count);
    }

    // Optimize images if requested
    if (shouldOptimize)
    {
        var optimizationResults = await photoProcessor.OptimizePhotosAsync(folderPath);

        var successCount = optimizationResults.Count(r => r.Success);
        var totalSaved = optimizationResults.Sum(r => r.TotalSizeSaved);
        var totalOriginal = optimizationResults.Sum(r => r.OriginalSize);
        var overallSavings = totalOriginal > 0 ? (double)totalSaved / totalOriginal * 100 : 0;

        logger.LogInformation(
            "Image optimization complete! Images: {Success}/{Total}, Saved: {SavedSize} ({Savings:F1}%), Output: {OutputDir}",
            successCount,
            optimizationResults.Count,
            FormatBytes(totalSaved),
            overallSavings,
            Path.Combine(folderPath, "optimized"));
    }

    return 0;
}
catch (DirectoryNotFoundException ex)
{
    logger.LogError(ex, "Directory not found");
    return 1;
}
catch (OperationCanceledException)
{
    logger.LogWarning("Operation cancelled by user");
    return 1;
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Fatal error occurred");
    return 1;
}

static void ShowUsage()
{
    Console.WriteLine("""
        PhotoAIExtractor - Extract metadata and optimize photos for web

        Usage: PhotoAIExtractor <folder_path> [options]

        Arguments:
          <folder_path>          Path to folder containing photos

        Options:
          --optimize, -o         Optimize images for web rendering
          --metadata-only, -m    Extract metadata only (skip optimization)

        Examples:
          PhotoAIExtractor C:\Photos
          PhotoAIExtractor C:\Photos --optimize
          PhotoAIExtractor C:\Photos -o

        Output:
          - photo_metadata.json: Extracted metadata (unless --metadata-only)
          - optimized/: Folder containing web-optimized images (with --optimize)
        """);
}

static string FormatBytes(long bytes) => bytes switch
{
    < 1024 => $"{bytes} B",
    < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
    < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
    _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
};
