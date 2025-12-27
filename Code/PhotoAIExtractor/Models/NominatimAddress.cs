namespace PhotoAIExtractor.Models;

/// <summary>
/// Address components from Nominatim API response
/// </summary>
public sealed record NominatimAddress
{
    public string? city { get; init; }
    public string? town { get; init; }
    public string? village { get; init; }
    public string? suburb { get; init; }
    public string? state { get; init; }
    public string? country { get; init; }
    public string? country_code { get; init; }

    /// <summary>
    /// Gets the best available city name from available address components
    /// </summary>
    public string? BestCityName => city ?? town ?? village ?? suburb;
}
