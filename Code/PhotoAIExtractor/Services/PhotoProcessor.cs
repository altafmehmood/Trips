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
    FileSettings fileSettings) : IPhotoProcessor
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
        Console.WriteLine($"Found {imageFiles.Count} image files.");

        var photoDataList = new List<PhotoData>(imageFiles.Count);

        foreach (var filePath in imageFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var photoData = await metadataExtractor.ExtractPhotoDataAsync(filePath, cancellationToken);
                photoDataList.Add(photoData);
                Console.WriteLine($"✓ Processed: {Path.GetFileName(filePath)}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"⚠ Processing cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error processing {Path.GetFileName(filePath)}: {ex.Message}");
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
        Console.WriteLine($"\nOptimizing {imageFiles.Count} images for web...");

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
