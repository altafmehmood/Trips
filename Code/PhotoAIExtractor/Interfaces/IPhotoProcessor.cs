using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Interfaces;

/// <summary>
/// Defines contract for processing multiple photos
/// </summary>
public interface IPhotoProcessor
{
    /// <summary>
    /// Processes all photos in a folder
    /// </summary>
    /// <param name="folderPath">Path to folder containing photos</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of extracted photo data</returns>
    Task<IReadOnlyCollection<PhotoData>> ProcessPhotosAsync(string folderPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes images for web rendering
    /// </summary>
    /// <param name="folderPath">Path to folder containing photos</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of optimization results</returns>
    Task<IReadOnlyCollection<OptimizationResult>> OptimizePhotosAsync(string folderPath, CancellationToken cancellationToken = default);
}
