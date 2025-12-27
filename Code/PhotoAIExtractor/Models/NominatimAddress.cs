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

    // Additional location types for national parks, protected areas, etc.
    public string? tourism { get; init; }
    public string? leisure { get; init; }
    public string? protected_area { get; init; }
    public string? national_park { get; init; }
    public string? nature_reserve { get; init; }
    public string? state_district { get; init; }
    public string? county { get; init; }
    public string? region { get; init; }

    /// <summary>
    /// Gets the best available city name from available address components
    /// </summary>
    public string? BestCityName => city ?? town ?? village ?? suburb;

    /// <summary>
    /// Gets the best available special location (national park, protected area, etc.)
    /// </summary>
    public string? BestSpecialLocation =>
        national_park ??
        protected_area ??
        nature_reserve ??
        tourism ??
        leisure;

    /// <summary>
    /// Gets the best available regional location
    /// </summary>
    public string? BestRegionalLocation =>
        state_district ??
        county ??
        region;
}
