using System.Security.Cryptography;
using ImageMagick;
using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;
using SkiaSharp;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for optimizing images for web rendering using SkiaSharp
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class ImageOptimizer(
    ImageOptimizationSettings settings,
    ILogger<ImageOptimizer> logger) : IImageOptimizer
{
    public async Task<OptimizationResult> OptimizeAsync(
        string imagePath,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        // Check if format is supported for optimization
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        var isHeic = extension is ".heic" or ".heif";

        if (!settings.SupportedFormats.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            var skippedReason = $"Format '{extension}' is not supported for optimization";
            logger.LogInformation("Skipped optimization for {FileName}: {Reason}",
                Path.GetFileName(imagePath), skippedReason);

            return CreateSkippedResult(imagePath, skippedReason);
        }

        Directory.CreateDirectory(outputDirectory);

        try
        {
            var fileInfo = new FileInfo(imagePath);
            var fileName = Path.GetFileNameWithoutExtension(imagePath);

            // Use Magick.NET for HEIC files, SkiaSharp for others
            if (isHeic)
            {
                return await OptimizeHeicAsync(imagePath, outputDirectory, fileInfo, fileName, cancellationToken);
            }
            else
            {
                return await OptimizeWithSkiaSharpAsync(imagePath, outputDirectory, fileInfo, fileName, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResult(imagePath, ex.Message);
        }
    }

    private async Task<OptimizationResult> OptimizeWithSkiaSharpAsync(
        string imagePath,
        string outputDirectory,
        FileInfo fileInfo,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate unique hash-based filename
            var optimizedFileName = GenerateOptimizedFilename(imagePath, settings.OutputFormat);
            var optimizedPath = Path.Combine(outputDirectory, optimizedFileName);

            // Check if already optimized - skip if exists
            if (File.Exists(optimizedPath))
            {
                var existingFileInfo = new FileInfo(optimizedPath);
                logger.LogInformation(
                    "Skipping already optimized file: {FileName} → {OptimizedFileName}",
                    Path.GetFileName(imagePath),
                    optimizedFileName);

                return CreateSkippedResult(imagePath, $"already exists as {optimizedFileName}");
            }

            using var originalBitmap = SKBitmap.Decode(imagePath);
            if (originalBitmap is null)
            {
                return CreateErrorResult(imagePath, "Failed to decode image");
            }

            var format = GetSkiaFormat(settings.OutputFormat);

            // Optimize image without resizing - preserve original dimensions
            await SaveOptimizedImageAsync(
                originalBitmap,
                optimizedPath,
                originalBitmap.Width,
                originalBitmap.Height,
                settings.Quality,
                format,
                cancellationToken);

            var optimizedFileInfo = new FileInfo(optimizedPath);

            return new OptimizationResult
            {
                OriginalPath = imagePath,
                OptimizedPath = optimizedPath,
                OriginalSize = fileInfo.Length,
                OptimizedSize = optimizedFileInfo.Length,
                OriginalWidth = originalBitmap.Width,
                OriginalHeight = originalBitmap.Height,
                OptimizedWidth = originalBitmap.Width,
                OptimizedHeight = originalBitmap.Height,
                Format = settings.OutputFormat,
                Variants = Array.Empty<VariantResult>()
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult(imagePath, ex.Message);
        }
    }

    private async Task<OptimizationResult> OptimizeHeicAsync(
        string imagePath,
        string outputDirectory,
        FileInfo fileInfo,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Generate unique hash-based filename
            var optimizedFileName = GenerateOptimizedFilename(imagePath, settings.OutputFormat);
            var optimizedPath = Path.Combine(outputDirectory, optimizedFileName);

            // Check if already optimized - skip if exists
            if (File.Exists(optimizedPath))
            {
                var existingFileInfo = new FileInfo(optimizedPath);
                logger.LogInformation(
                    "Skipping already optimized file: {FileName} → {OptimizedFileName}",
                    Path.GetFileName(imagePath),
                    optimizedFileName);

                return CreateSkippedResult(imagePath, $"already exists as {optimizedFileName}");
            }

            using var magickImage = new MagickImage(imagePath);

            var originalWidth = (int)magickImage.Width;
            var originalHeight = (int)magickImage.Height;

            // Set output format and quality without resizing
            magickImage.Format = MagickFormat.WebP;
            magickImage.Quality = (uint)settings.Quality;

            // Save optimized image
            await Task.Run(() => magickImage.Write(optimizedPath), cancellationToken);

            var optimizedFileInfo = new FileInfo(optimizedPath);

            return new OptimizationResult
            {
                OriginalPath = imagePath,
                OptimizedPath = optimizedPath,
                OriginalSize = fileInfo.Length,
                OptimizedSize = optimizedFileInfo.Length,
                OriginalWidth = originalWidth,
                OriginalHeight = originalHeight,
                OptimizedWidth = originalWidth,
                OptimizedHeight = originalHeight,
                Format = settings.OutputFormat,
                Variants = Array.Empty<VariantResult>()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to optimize HEIC file: {FileName}", Path.GetFileName(imagePath));
            return CreateErrorResult(imagePath, ex.Message);
        }
    }


    public async Task<IReadOnlyCollection<OptimizationResult>> OptimizeBatchAsync(
        IEnumerable<string> imagePaths,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(imagePaths);

        var results = new List<OptimizationResult>();

        foreach (var imagePath in imagePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await OptimizeAsync(imagePath, outputDirectory, cancellationToken);
                results.Add(result);

                if (result.Success)
                {
                    logger.LogInformation(
                        "Optimized: {FileName} ({OriginalSize} → {OptimizedSize}, {CompressionRatio:F1}% saved)",
                        Path.GetFileName(imagePath),
                        FormatBytes(result.OriginalSize),
                        FormatBytes(result.OptimizedSize),
                        result.CompressionRatio);
                }
                else
                {
                    logger.LogWarning("Failed to optimize {FileName}: {Error}",
                        Path.GetFileName(imagePath), result.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error optimizing {FileName}", Path.GetFileName(imagePath));
                results.Add(CreateErrorResult(imagePath, ex.Message));
            }
        }

        return results.AsReadOnly();
    }

    private async Task SaveOptimizedImageAsync(
        SKBitmap sourceBitmap,
        string outputPath,
        int width,
        int height,
        int quality,
        SKEncodedImageFormat format,
        CancellationToken cancellationToken)
    {
        var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        using var resizedBitmap = sourceBitmap.Resize(
            new SKImageInfo(width, height),
            samplingOptions);

        if (resizedBitmap is null)
        {
            throw new InvalidOperationException("Failed to resize image");
        }

        using var image = SKImage.FromBitmap(resizedBitmap);
        using var data = image.Encode(format, quality);

        await using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
    }


    private static SKEncodedImageFormat GetSkiaFormat(string format) => format.ToLowerInvariant() switch
    {
        "webp" => SKEncodedImageFormat.Webp,
        "jpeg" or "jpg" => SKEncodedImageFormat.Jpeg,
        "png" => SKEncodedImageFormat.Png,
        _ => SKEncodedImageFormat.Webp
    };

    private static OptimizationResult CreateErrorResult(string imagePath, string error)
    {
        return new OptimizationResult
        {
            OriginalPath = imagePath,
            OptimizedPath = string.Empty,
            OriginalSize = 0,
            OptimizedSize = 0,
            OriginalWidth = 0,
            OriginalHeight = 0,
            OptimizedWidth = 0,
            OptimizedHeight = 0,
            Format = string.Empty,
            Variants = Array.Empty<VariantResult>(),
            Error = error
        };
    }

    private static OptimizationResult CreateSkippedResult(string imagePath, string reason)
    {
        var fileInfo = new FileInfo(imagePath);
        return new OptimizationResult
        {
            OriginalPath = imagePath,
            OptimizedPath = imagePath, // Keep original
            OriginalSize = fileInfo.Length,
            OptimizedSize = fileInfo.Length, // No size change
            OriginalWidth = 0,
            OriginalHeight = 0,
            OptimizedWidth = 0,
            OptimizedHeight = 0,
            Format = Path.GetExtension(imagePath),
            Variants = Array.Empty<VariantResult>(),
            Error = $"Skipped: {reason}"
        };
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
    };

    /// <summary>
    /// Generates a unique filename for the optimized image based on SHA256 hash of the file content.
    /// This ensures that:
    /// - The same image always gets the same filename (deterministic)
    /// - Different images get different filenames (collision-resistant)
    /// - Re-running the optimizer skips already-optimized images
    /// </summary>
    private static string GenerateOptimizedFilename(string imagePath, string outputFormat)
    {
        using var stream = File.OpenRead(imagePath);
        var hash = SHA256.HashData(stream);
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();

        // Use first 16 characters of hash for reasonable filename length while maintaining uniqueness
        return $"{hashString[..16]}.{outputFormat}";
    }
}
