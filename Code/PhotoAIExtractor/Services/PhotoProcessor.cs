using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;
using System.Globalization;

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
        string? optimizedBaseFolder = null;
        // Track photo counters per folder for sequential naming
        var folderPhotoCounters = new Dictionary<string, int>();

        if (shouldOptimize)
        {
            optimizedBaseFolder = Path.Combine(folderPath, "optimized");
            Directory.CreateDirectory(optimizedBaseFolder);
            logger.LogInformation("Image optimization enabled - output directory: {OptimizedFolder}", optimizedBaseFolder);
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
                if (shouldOptimize && optimizedBaseFolder != null)
                {
                    // Determine folder name based on date and location
                    var (folderName, photoDate) = DetermineFolderName(photoData);
                    var targetFolder = Path.Combine(optimizedBaseFolder, folderName);
                    Directory.CreateDirectory(targetFolder);

                    // Get sequential counter for this folder
                    if (!folderPhotoCounters.ContainsKey(folderName))
                    {
                        folderPhotoCounters[folderName] = 1;
                    }
                    var photoNumber = folderPhotoCounters[folderName];
                    folderPhotoCounters[folderName]++;

                    // Generate new filename
                    var newFileName = GeneratePhotoFileName(photoNumber, photoDate);

                    var optimizationResult = await imageOptimizer.OptimizeAsync(
                        filePath,
                        targetFolder,
                        cancellationToken);

                    optimizationResults.Add(optimizationResult);

                    if (optimizationResult.Success)
                    {
                        // Rename the optimized file
                        var oldOptimizedPath = optimizationResult.OptimizedPath;
                        var newOptimizedPath = Path.Combine(targetFolder, newFileName);

                        if (File.Exists(oldOptimizedPath))
                        {
                            File.Move(oldOptimizedPath, newOptimizedPath, overwrite: true);
                            optimizationResult = optimizationResult with { OptimizedPath = newOptimizedPath };
                        }

                        // Populate optimized image info in PhotoData with relative path
                        photoData.OptimizedImages = new OptimizedImageInfo
                        {
                            WebPImage = MakeRelativePath(folderPath, newOptimizedPath),
                            OriginalSize = optimizationResult.OriginalSize,
                            OptimizedSize = optimizationResult.OptimizedSize,
                            CompressionRatio = optimizationResult.CompressionRatio
                        };

                        // Update the main JSON file with optimization info
                        await outputWriter.UpdateAsync(photoData, outputPath, cancellationToken);

                        // Also write to the folder-specific JSON file
                        var folderJsonPath = Path.Combine(targetFolder, outputSettings.OutputFileName);
                        await outputWriter.AppendAsync(photoData, folderJsonPath, cancellationToken);

                        logger.LogInformation(
                            "Optimized: {FileName} → {NewFolder}/{NewFileName} ({OriginalSize} → {OptimizedSize}, {CompressionRatio:F1}% saved)",
                            Path.GetFileName(filePath),
                            folderName,
                            newFileName,
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

    /// <summary>
    /// Determines the folder name based on photo date and location
    /// </summary>
    private static (string folderName, DateTime? photoDate) DetermineFolderName(PhotoData photoData)
    {
        // Try to parse the date
        DateTime? photoDate = null;
        string datePrefix = "Unknown_Date";

        if (!string.IsNullOrWhiteSpace(photoData.DateTaken))
        {
            // Try common date formats
            var formats = new[]
            {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy:MM:dd",
                "yyyy-MM-dd"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(photoData.DateTaken, format,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    photoDate = parsedDate;
                    datePrefix = parsedDate.ToString("yyyy-MM-dd");
                    break;
                }
            }

            // Fallback to general parsing
            if (!photoDate.HasValue && DateTime.TryParse(photoData.DateTaken, out var generalDate))
            {
                photoDate = generalDate;
                datePrefix = generalDate.ToString("yyyy-MM-dd");
            }
        }

        // Determine location (City > State > Country)
        var location = "Unknown_Location";
        if (!string.IsNullOrWhiteSpace(photoData.City))
        {
            location = SanitizeFolderName(photoData.City);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.State))
        {
            location = SanitizeFolderName(photoData.State);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.Country))
        {
            location = SanitizeFolderName(photoData.Country);
        }

        return ($"{datePrefix}_{location}", photoDate);
    }

    /// <summary>
    /// Generates a filename for the optimized photo
    /// </summary>
    private static string GeneratePhotoFileName(int photoNumber, DateTime? photoDate)
    {
        // If we have a date with time, use it in the filename for uniqueness
        if (photoDate.HasValue)
        {
            var timeStamp = photoDate.Value.ToString("HHmmss");
            return $"{photoNumber:D3}_{timeStamp}.webp";
        }

        // Otherwise just use sequential numbering
        return $"{photoNumber:D3}.webp";
    }

    /// <summary>
    /// Sanitizes a string to be used as a folder name
    /// </summary>
    private static string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Unknown";
        }

        // Remove or replace invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Replace spaces with underscores
        sanitized = sanitized.Replace(' ', '_');

        // Limit length
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }

        return sanitized;
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
