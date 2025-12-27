using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Interfaces;

/// <summary>
/// Defines contract for optimizing images for web rendering
/// </summary>
public interface IImageOptimizer
{
    /// <summary>
    /// Optimizes an image for web rendering
    /// </summary>
    /// <param name="imagePath">Path to the source image</param>
    /// <param name="outputDirectory">Directory for optimized images</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimization result with details</returns>
    Task<OptimizationResult> OptimizeAsync(
        string imagePath,
        string outputDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes multiple images for web rendering
    /// </summary>
    /// <param name="imagePaths">Collection of image paths</param>
    /// <param name="outputDirectory">Directory for optimized images</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of optimization results</returns>
    Task<IReadOnlyCollection<OptimizationResult>> OptimizeBatchAsync(
        IEnumerable<string> imagePaths,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
