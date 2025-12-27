namespace PhotoAIExtractor.Models;

/// <summary>
/// Represents extracted metadata from a photo file
/// </summary>
public record PhotoData
{
    public required string FileName { get; init; }
    public required string FilePath { get; set; }  // Changed to set for relative path conversion
    public required long FileSize { get; init; }

    // GPS Data
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Altitude { get; set; }

    // Reverse Geocoded Location
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? DisplayName { get; set; }

    // Special locations (national parks, protected areas, etc.)
    public string? NationalPark { get; set; }
    public string? ProtectedArea { get; set; }
    public string? Region { get; set; }

    // Camera Settings
    public string? CameraMake { get; set; }
    public string? CameraModel { get; set; }
    public string? LensModel { get; set; }

    // Photo Settings
    public string? DateTaken { get; set; }
    public string? ExposureTime { get; set; }
    public string? FNumber { get; set; }
    public string? ISO { get; set; }
    public string? FocalLength { get; set; }

    // Image Properties
    public string? Width { get; set; }
    public string? Height { get; set; }
    public string? Orientation { get; set; }
    public string? Software { get; set; }

    // Optimized Images (relative paths)
    public string? OptimizedPath { get; set; }
    public OptimizedImageInfo? OptimizedImages { get; set; }

    // Error handling
    public string? Error { get; set; }

    /// <summary>
    /// Indicates whether the photo has valid GPS coordinates
    /// </summary>
    public bool HasGpsData => Latitude.HasValue && Longitude.HasValue;
}

/// <summary>
/// Information about optimized images
/// </summary>
public sealed record OptimizedImageInfo
{
    public string? WebPImage { get; init; }
    public long? OriginalSize { get; init; }
    public long? OptimizedSize { get; init; }
    public double? CompressionRatio { get; init; }
}
