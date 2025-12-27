namespace PhotoAIExtractor.Models;

/// <summary>
/// Result of an image optimization operation
/// </summary>
public sealed record OptimizationResult
{
    public required string OriginalPath { get; init; }
    public required string OptimizedPath { get; init; }
    public required long OriginalSize { get; init; }
    public required long OptimizedSize { get; init; }
    public required int OriginalWidth { get; init; }
    public required int OriginalHeight { get; init; }
    public required int OptimizedWidth { get; init; }
    public required int OptimizedHeight { get; init; }
    public required string Format { get; init; }
    public required IReadOnlyList<VariantResult> Variants { get; init; }
    public string? Error { get; init; }

    /// <summary>
    /// Compression ratio as a percentage
    /// </summary>
    public double CompressionRatio => OriginalSize > 0
        ? (1 - (double)OptimizedSize / OriginalSize) * 100
        : 0;

    /// <summary>
    /// Total size savings across all variants
    /// </summary>
    public long TotalSizeSaved => OriginalSize - OptimizedSize + Variants.Sum(v => v.OriginalSize - v.OptimizedSize);

    /// <summary>
    /// Indicates whether the optimization was successful
    /// </summary>
    public bool Success => string.IsNullOrEmpty(Error);
}

/// <summary>
/// Result of a responsive variant optimization
/// </summary>
public sealed record VariantResult
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required long OriginalSize { get; init; }
    public required long OptimizedSize { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}
