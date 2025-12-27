namespace PhotoAIExtractor.Configuration;

/// <summary>
/// Application configuration settings
/// </summary>
public sealed record AppSettings
{
    public required GeocodingSettings Geocoding { get; init; }
    public required FileSettings Files { get; init; }
    public required OutputSettings Output { get; init; }
    public required ImageOptimizationSettings ImageOptimization { get; init; }
}

/// <summary>
/// Geocoding service configuration
/// </summary>
public sealed record GeocodingSettings
{
    public required string BaseUrl { get; init; }
    public required int RateLimitDelayMs { get; init; }
    public required string UserAgent { get; init; }
    public required int ZoomLevel { get; init; }

    public static GeocodingSettings Default => new()
    {
        BaseUrl = "https://nominatim.openstreetmap.org",
        RateLimitDelayMs = 1000,
        UserAgent = "PhotoAIExtractor/1.0",
        ZoomLevel = 18
    };
}

/// <summary>
/// File processing configuration
/// </summary>
public sealed record FileSettings
{
    public required string[] SupportedExtensions { get; init; }

    public static FileSettings Default => new()
    {
        SupportedExtensions = [".jpg", ".jpeg", ".png", ".tiff", ".tif", ".heic", ".heif"]
    };
}

/// <summary>
/// Output configuration
/// </summary>
public sealed record OutputSettings
{
    public required string OutputFileName { get; init; }
    public required bool WriteIndented { get; init; }
    public required bool IgnoreNullValues { get; init; }

    public static OutputSettings Default => new()
    {
        OutputFileName = "photo_metadata.json",
        WriteIndented = true,
        IgnoreNullValues = true
    };
}
