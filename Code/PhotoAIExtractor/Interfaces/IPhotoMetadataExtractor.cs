using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Interfaces;

/// <summary>
/// Defines contract for extracting photo metadata
/// </summary>
public interface IPhotoMetadataExtractor
{
    /// <summary>
    /// Extracts metadata from a photo file
    /// </summary>
    /// <param name="filePath">Path to the photo file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted photo data</returns>
    Task<PhotoData> ExtractPhotoDataAsync(string filePath, CancellationToken cancellationToken = default);
}
