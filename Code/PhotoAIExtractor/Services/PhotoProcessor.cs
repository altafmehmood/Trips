using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for processing multiple photos in a folder
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class PhotoProcessor(
    IPhotoMetadataExtractor metadataExtractor,
    IImageOptimizer imageOptimizer,
    IOutputWriter outputWriter,
    FileSettings fileSettings,
    OutputSettings outputSettings,
    ILogger<PhotoProcessor> logger) : IPhotoProcessor
{
    public async Task<(IReadOnlyCollection<PhotoData> PhotoData, IReadOnlyCollection<OptimizationResult> OptimizationResults)>
        ProcessPhotosAsync(
            string folderPath,
            bool shouldOptimize = false,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder '{folderPath}' does not exist.");
        }

        var imageFiles = GetImageFiles(folderPath);
        logger.LogInformation("Found {Count} image files", imageFiles.Count);

        var photoDataList = new List<PhotoData>(imageFiles.Count);
        var optimizationResults = new List<OptimizationResult>();
        var outputPath = Path.Combine(folderPath, outputSettings.OutputFileName);

        // Delete existing metadata file to start fresh
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
            logger.LogDebug("Deleted existing output file: {Path}", outputPath);
        }

        // Setup optimization output directory if needed
        string? optimizedFolder = null;
        if (shouldOptimize)
        {
            optimizedFolder = Path.Combine(folderPath, "optimized");
            Directory.CreateDirectory(optimizedFolder);
            logger.LogInformation("Image optimization enabled - output directory: {OptimizedFolder}", optimizedFolder);
        }

        foreach (var filePath in imageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Extract metadata
                var photoData = await metadataExtractor.ExtractPhotoDataAsync(filePath, cancellationToken);
                photoDataList.Add(photoData);

                // Convert file path to relative before writing
                photoData.FilePath = MakeRelativePath(folderPath, filePath);

                // Write metadata after each photo
                await outputWriter.AppendAsync(photoData, outputPath, cancellationToken);

                logger.LogInformation("Processed metadata: {FileName}", Path.GetFileName(filePath));

                // Optimize immediately if requested
                if (shouldOptimize && optimizedFolder != null)
                {
                    var optimizationResult = await imageOptimizer.OptimizeAsync(
                        filePath,
                        optimizedFolder,
                        cancellationToken);

                    optimizationResults.Add(optimizationResult);

                    if (optimizationResult.Success)
                    {
                        // Populate optimized image info in PhotoData with relative path
                        photoData.OptimizedImages = new OptimizedImageInfo
                        {
                            WebPImage = MakeRelativePath(folderPath, optimizationResult.OptimizedPath),
                            OriginalSize = optimizationResult.OriginalSize,
                            OptimizedSize = optimizationResult.OptimizedSize,
                            CompressionRatio = optimizationResult.CompressionRatio
                        };

                        // Update the JSON file with optimization info
                        await outputWriter.UpdateAsync(photoData, outputPath, cancellationToken);

                        logger.LogInformation(
                            "Optimized: {FileName} ({OriginalSize} → {OptimizedSize}, {CompressionRatio:F1}% saved)",
                            Path.GetFileName(filePath),
                            FormatBytes(optimizationResult.OriginalSize),
                            FormatBytes(optimizationResult.OptimizedSize),
                            optimizationResult.CompressionRatio);
                    }
                    else if (optimizationResult.Error?.StartsWith("Skipped:") == true)
                    {
                        // File was skipped (unsupported format) - this is informational
                        logger.LogInformation("Skipped {FileName}: {Reason}",
                            Path.GetFileName(filePath),
                            optimizationResult.Error.Replace("Skipped: ", ""));
                    }
                    else
                    {
                        logger.LogWarning("Failed to optimize {FileName}: {Error}",
                            Path.GetFileName(filePath), optimizationResult.Error);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Processing cancelled");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing {FileName}", Path.GetFileName(filePath));
            }
        }

        return (photoDataList.AsReadOnly(), optimizationResults.AsReadOnly());
    }

    private static string MakeRelativePath(string basePath, string fullPath)
    {
        // Ensure paths use consistent directory separators
        var baseUri = new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar);
        var fullUri = new Uri(Path.GetFullPath(fullPath));

        var relativePath = Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString());

        // Replace forward slashes with backslashes on Windows for consistency
        return relativePath.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
    };

    /// <summary>
    /// Optimizes images for web rendering
    /// </summary>
    public async Task<IReadOnlyCollection<OptimizationResult>> OptimizePhotosAsync(
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder '{folderPath}' does not exist.");
        }

        var imageFiles = GetImageFiles(folderPath);
        logger.LogInformation("Optimizing {Count} images for web", imageFiles.Count);

        var optimizedFolder = Path.Combine(folderPath, "optimized");
        Directory.CreateDirectory(optimizedFolder);

        var results = await imageOptimizer.OptimizeBatchAsync(
            imageFiles,
            optimizedFolder,
            cancellationToken);

        return results;
    }

    private IReadOnlyList<string> GetImageFiles(string folderPath)
    {
        return Directory
            .GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => fileSettings.SupportedExtensions.Contains(
                Path.GetExtension(f),
                StringComparer.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }
}
