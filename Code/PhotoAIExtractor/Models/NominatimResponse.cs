namespace PhotoAIExtractor.Models;

/// <summary>
/// Response from Nominatim reverse geocoding API
/// </summary>
public sealed record NominatimResponse
{
    public string? display_name { get; init; }
    public NominatimAddress? address { get; init; }
}
