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
    public async Task<IReadOnlyCollection<PhotoData>> ProcessPhotosAsync(
        string folderPath,
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
        var outputPath = Path.Combine(folderPath, outputSettings.OutputFileName);

        // Delete existing file to start fresh
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
            logger.LogDebug("Deleted existing output file: {Path}", outputPath);
        }

        foreach (var filePath in imageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var photoData = await metadataExtractor.ExtractPhotoDataAsync(filePath, cancellationToken);
                photoDataList.Add(photoData);

                // Write metadata after each photo
                await outputWriter.AppendAsync(photoData, outputPath, cancellationToken);

                logger.LogInformation("Processed: {FileName}", Path.GetFileName(filePath));
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

        return photoDataList.AsReadOnly();
    }

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
