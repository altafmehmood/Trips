namespace PhotoAIExtractor.Configuration;

/// <summary>
/// Configuration for image optimization
/// </summary>
public sealed record ImageOptimizationSettings
{
    public required int MaxWidth { get; init; }
    public required int MaxHeight { get; init; }
    public required int Quality { get; init; }
    public required string OutputFormat { get; init; }
    public required string OutputSuffix { get; init; }
    public required bool PreserveOriginal { get; init; }
    public required bool CreateResponsiveVariants { get; init; }
    public required ResponsiveVariant[] ResponsiveVariants { get; init; }

    public static ImageOptimizationSettings Default => new()
    {
        MaxWidth = 1920,
        MaxHeight = 1080,
        Quality = 85,
        OutputFormat = "webp",
        OutputSuffix = "_optimized",
        PreserveOriginal = true,
        CreateResponsiveVariants = true,
        ResponsiveVariants =
        [
            new ResponsiveVariant { Name = "thumbnail", MaxWidth = 300, MaxHeight = 300, Quality = 80 },
            new ResponsiveVariant { Name = "small", MaxWidth = 640, MaxHeight = 480, Quality = 82 },
            new ResponsiveVariant { Name = "medium", MaxWidth = 1024, MaxHeight = 768, Quality = 85 },
            new ResponsiveVariant { Name = "large", MaxWidth = 1920, MaxHeight = 1080, Quality = 85 }
        ]
    };
}

/// <summary>
/// Represents a responsive image variant configuration
/// </summary>
public sealed record ResponsiveVariant
{
    public required string Name { get; init; }
    public required int MaxWidth { get; init; }
    public required int MaxHeight { get; init; }
    public required int Quality { get; init; }
}
