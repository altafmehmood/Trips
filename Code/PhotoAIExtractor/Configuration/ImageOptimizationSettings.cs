namespace PhotoAIExtractor.Configuration;

/// <summary>
/// Configuration for image optimization
/// </summary>
public sealed record ImageOptimizationSettings
{
    public required int Quality { get; init; }
    public required string OutputFormat { get; init; }
    public required string OutputSuffix { get; init; }
    public required bool PreserveOriginal { get; init; }
    public required string[] SupportedFormats { get; init; }

    public static ImageOptimizationSettings Default => new()
    {
        Quality = 75,
        OutputFormat = "webp",
        OutputSuffix = "_optimized",
        PreserveOriginal = true,
        // Supported formats for optimization
        // SkiaSharp handles: JPG, PNG, TIFF
        // Magick.NET handles: HEIC, HEIF (converts to WebP for consistency)
        SupportedFormats = [".jpg", ".jpeg", ".png", ".tiff", ".tif", ".heic", ".heif"]
    };
}
