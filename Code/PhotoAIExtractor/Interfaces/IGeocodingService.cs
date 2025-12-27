using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Interfaces;

/// <summary>
/// Defines contract for reverse geocoding operations
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Reverse geocodes GPS coordinates to populate location information
    /// </summary>
    /// <param name="photoData">Photo data containing GPS coordinates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReverseGeocodeAsync(PhotoData photoData, CancellationToken cancellationToken = default);
}
