using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;
using SkiaSharp;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for optimizing images for web rendering using SkiaSharp
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class ImageOptimizer(ImageOptimizationSettings settings) : IImageOptimizer
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

        Directory.CreateDirectory(outputDirectory);

        try
        {
            using var originalBitmap = SKBitmap.Decode(imagePath);
            if (originalBitmap is null)
            {
                return CreateErrorResult(imagePath, "Failed to decode image");
            }

            var fileInfo = new FileInfo(imagePath);
            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var format = GetSkiaFormat(settings.OutputFormat);

            // Optimize main image
            var optimizedPath = Path.Combine(
                outputDirectory,
                $"{fileName}{settings.OutputSuffix}.{settings.OutputFormat}");

            var (optimizedWidth, optimizedHeight) = CalculateOptimizedDimensions(
                originalBitmap.Width,
                originalBitmap.Height,
                settings.MaxWidth,
                settings.MaxHeight);

            await SaveOptimizedImageAsync(
                originalBitmap,
                optimizedPath,
                optimizedWidth,
                optimizedHeight,
                settings.Quality,
                format,
                cancellationToken);

            var optimizedFileInfo = new FileInfo(optimizedPath);

            // Create responsive variants
            var variants = new List<VariantResult>();
            if (settings.CreateResponsiveVariants)
            {
                variants = await CreateResponsiveVariantsAsync(
                    originalBitmap,
                    outputDirectory,
                    fileName,
                    format,
                    fileInfo.Length,
                    cancellationToken);
            }

            return new OptimizationResult
            {
                OriginalPath = imagePath,
                OptimizedPath = optimizedPath,
                OriginalSize = fileInfo.Length,
                OptimizedSize = optimizedFileInfo.Length,
                OriginalWidth = originalBitmap.Width,
                OriginalHeight = originalBitmap.Height,
                OptimizedWidth = optimizedWidth,
                OptimizedHeight = optimizedHeight,
                Format = settings.OutputFormat,
                Variants = variants
            };
        }
        catch (Exception ex)
        {
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
                    Console.WriteLine($"  ✓ Optimized: {Path.GetFileName(imagePath)} " +
                        $"({FormatBytes(result.OriginalSize)} → {FormatBytes(result.OptimizedSize)}, " +
                        $"{result.CompressionRatio:F1}% saved)");
                }
                else
                {
                    Console.WriteLine($"  ✗ Failed: {Path.GetFileName(imagePath)} - {result.Error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error optimizing {Path.GetFileName(imagePath)}: {ex.Message}");
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

    private async Task<List<VariantResult>> CreateResponsiveVariantsAsync(
        SKBitmap originalBitmap,
        string outputDirectory,
        string baseFileName,
        SKEncodedImageFormat format,
        long originalFileSize,
        CancellationToken cancellationToken)
    {
        var variants = new List<VariantResult>();

        foreach (var variant in settings.ResponsiveVariants)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var variantPath = Path.Combine(
                outputDirectory,
                $"{baseFileName}_{variant.Name}.{settings.OutputFormat}");

            var (width, height) = CalculateOptimizedDimensions(
                originalBitmap.Width,
                originalBitmap.Height,
                variant.MaxWidth,
                variant.MaxHeight);

            await SaveOptimizedImageAsync(
                originalBitmap,
                variantPath,
                width,
                height,
                variant.Quality,
                format,
                cancellationToken);

            var variantFileInfo = new FileInfo(variantPath);

            variants.Add(new VariantResult
            {
                Name = variant.Name,
                Path = variantPath,
                OriginalSize = originalFileSize,
                OptimizedSize = variantFileInfo.Length,
                Width = width,
                Height = height
            });
        }

        return variants;
    }

    private static (int width, int height) CalculateOptimizedDimensions(
        int originalWidth,
        int originalHeight,
        int maxWidth,
        int maxHeight)
    {
        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var widthRatio = (double)maxWidth / originalWidth;
        var heightRatio = (double)maxHeight / originalHeight;
        var ratio = Math.Min(widthRatio, heightRatio);

        return (
            width: (int)(originalWidth * ratio),
            height: (int)(originalHeight * ratio)
        );
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

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):F1} MB",
        _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
    };
}
