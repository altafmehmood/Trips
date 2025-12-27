using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Interfaces;

/// <summary>
/// Defines contract for processing multiple photos
/// </summary>
public interface IPhotoProcessor
{
    /// <summary>
    /// Processes all photos in a folder, optionally optimizing each photo immediately after metadata extraction
    /// </summary>
    /// <param name="folderPath">Path to folder containing photos</param>
    /// <param name="shouldOptimize">Whether to optimize each photo for web rendering</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing photo data and optimization results</returns>
    Task<(IReadOnlyCollection<PhotoData> PhotoData, IReadOnlyCollection<OptimizationResult> OptimizationResults)>
        ProcessPhotosAsync(string folderPath, bool shouldOptimize = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes images for web rendering (batch mode for already-processed photos)
    /// </summary>
    /// <param name="folderPath">Path to folder containing photos</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of optimization results</returns>
    Task<IReadOnlyCollection<OptimizationResult>> OptimizePhotosAsync(string folderPath, CancellationToken cancellationToken = default);
}
