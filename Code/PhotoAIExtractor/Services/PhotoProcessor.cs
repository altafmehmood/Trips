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
            bool skipGeocoding = false,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder '{folderPath}' does not exist.");
        }

        var imageFiles = GetImageFiles(folderPath);
        logger.LogInformation("Found {Count} image files", imageFiles.Count);

        var photoDataList = new System.Collections.Concurrent.ConcurrentBag<PhotoData>();
        var optimizationResults = new System.Collections.Concurrent.ConcurrentBag<OptimizationResult>();
        var outputPath = Path.Combine(folderPath, outputSettings.OutputFileName);

        // Delete existing metadata file to start fresh
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
            logger.LogDebug("Deleted existing output file: {Path}", outputPath);
        }

        // Setup optimization output directory if needed
        string? optimizedBaseFolder = null;
        // Track photo counters per folder for sequential naming (thread-safe)
        var folderPhotoCounters = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        // Track photos per location folder for batched JSON writes
        var folderPhotoGroups = new System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentBag<PhotoData>>();

        if (shouldOptimize)
        {
            optimizedBaseFolder = Path.Combine(folderPath, "optimized");
            Directory.CreateDirectory(optimizedBaseFolder);
            logger.LogInformation("Image optimization enabled - output directory: {OptimizedFolder}", optimizedBaseFolder);
        }

        // Process photos in parallel for better performance
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(imageFiles, parallelOptions, async (filePath, ct) =>
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // Extract metadata
                var photoData = await metadataExtractor.ExtractPhotoDataAsync(filePath, skipGeocoding, ct);
                photoDataList.Add(photoData);

                // Convert file path to relative
                photoData.FilePath = MakeRelativePath(folderPath, filePath);

                logger.LogInformation("Processed metadata: {FileName}", Path.GetFileName(filePath));

                // Optimize immediately if requested
                if (shouldOptimize && optimizedBaseFolder != null)
                {
                    // Determine date and location separately
                    var (dateFolder, locationFolder, photoDate) = DetermineFolderStructure(photoData);
                    var targetFolder = Path.Combine(optimizedBaseFolder, dateFolder, locationFolder);
                    Directory.CreateDirectory(targetFolder);

                    // Get sequential counter for this location folder (thread-safe)
                    var folderKey = $"{dateFolder}/{locationFolder}";
                    var photoNumber = folderPhotoCounters.AddOrUpdate(
                        folderKey,
                        1,
                        (key, oldValue) => oldValue + 1);

                    // Generate new filename
                    var newFileName = GeneratePhotoFileName(photoNumber, photoDate);

                    var optimizationResult = await imageOptimizer.OptimizeAsync(
                        filePath,
                        targetFolder,
                        ct);

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

                        // Populate optimized image info in PhotoData with relative path (for main JSON)
                        photoData.OptimizedImages = new OptimizedImageInfo
                        {
                            WebPImage = MakeRelativePath(folderPath, newOptimizedPath),
                            OriginalSize = optimizationResult.OriginalSize,
                            OptimizedSize = optimizationResult.OptimizedSize,
                            CompressionRatio = optimizationResult.CompressionRatio
                        };

                        // Create a copy of photoData with paths relative to the target folder for folder-specific JSON
                        var folderPhotoData = photoData with
                        {
                            FilePath = MakeRelativePath(folderPath, filePath),
                            OptimizedImages = new OptimizedImageInfo
                            {
                                WebPImage = newFileName,
                                OriginalSize = optimizationResult.OriginalSize,
                                OptimizedSize = optimizationResult.OptimizedSize,
                                CompressionRatio = optimizationResult.CompressionRatio
                            }
                        };

                        // Add to folder group for batched writing later
                        folderPhotoGroups.AddOrUpdate(
                            folderKey,
                            new System.Collections.Concurrent.ConcurrentBag<PhotoData> { folderPhotoData },
                            (key, existingBag) =>
                            {
                                existingBag.Add(folderPhotoData);
                                return existingBag;
                            });

                        logger.LogInformation(
                            "Optimized: {FileName} → {DateFolder}/{LocationFolder}/{NewFileName} ({OriginalSize} → {OptimizedSize}, {CompressionRatio:F1}% saved)",
                            Path.GetFileName(filePath),
                            dateFolder,
                            locationFolder,
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
        });

        // Batch write all metadata at once (much faster than per-photo writes)
        logger.LogInformation("Writing metadata files...");

        // Write main metadata file
        await outputWriter.WriteAsync(photoDataList.ToList().AsReadOnly(), outputPath, cancellationToken);

        // Write folder-specific metadata files
        if (shouldOptimize && folderPhotoGroups.Count > 0)
        {
            foreach (var folderGroup in folderPhotoGroups)
            {
                var folderKey = folderGroup.Key;
                var photos = folderGroup.Value.ToList();

                // Parse folder key to get the actual folder path
                var folderParts = folderKey.Split('/');
                if (folderParts.Length == 2)
                {
                    var targetFolder = Path.Combine(optimizedBaseFolder!, folderParts[0], folderParts[1]);
                    var folderJsonPath = Path.Combine(targetFolder, outputSettings.OutputFileName);

                    await outputWriter.WriteAsync(photos.AsReadOnly(), folderJsonPath, cancellationToken);
                    logger.LogDebug("Wrote {Count} photos to {FolderJsonPath}", photos.Count, folderJsonPath);
                }
            }

            logger.LogInformation("Wrote metadata for {FolderCount} location folders", folderPhotoGroups.Count);
        }

        return (photoDataList.ToList().AsReadOnly(), optimizationResults.ToList().AsReadOnly());
    }

    /// <summary>
    /// Determines the folder structure based on photo date and location
    /// </summary>
    private static (string dateFolder, string locationFolder, DateTime? photoDate) DetermineFolderStructure(PhotoData photoData)
    {
        // Try to parse the date
        DateTime? photoDate = null;
        string dateFolder = "Unknown_Date";

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
                    dateFolder = parsedDate.ToString("yyyy-MM-dd");
                    break;
                }
            }

            // Fallback to general parsing
            if (!photoDate.HasValue && DateTime.TryParse(photoData.DateTaken, out var generalDate))
            {
                photoDate = generalDate;
                dateFolder = generalDate.ToString("yyyy-MM-dd");
            }
        }

        // Determine location (priority: City > National Park > Protected Area > Region > State > Country)
        var locationFolder = "Unknown_Location";
        if (!string.IsNullOrWhiteSpace(photoData.City))
        {
            locationFolder = SanitizeFolderName(photoData.City);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.NationalPark))
        {
            locationFolder = SanitizeFolderName(photoData.NationalPark);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.ProtectedArea))
        {
            locationFolder = SanitizeFolderName(photoData.ProtectedArea);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.Region))
        {
            locationFolder = SanitizeFolderName(photoData.Region);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.State))
        {
            locationFolder = SanitizeFolderName(photoData.State);
        }
        else if (!string.IsNullOrWhiteSpace(photoData.Country))
        {
            locationFolder = SanitizeFolderName(photoData.Country);
        }

        return (dateFolder, locationFolder, photoDate);
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
