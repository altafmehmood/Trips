using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for reverse geocoding GPS coordinates using Nominatim API
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class GeocodingService(
    GeocodingSettings settings,
    ILogger<GeocodingService> logger) : IGeocodingService
{
    public async Task ReverseGeocodeAsync(PhotoData photoData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photoData);

        if (!photoData.HasGpsData)
            return;

        try
        {
            logger.LogDebug("Reverse geocoding coordinates: {Lat}, {Lon}", photoData.Latitude, photoData.Longitude);

            var geocodeResult = await settings.BaseUrl
                .AppendPathSegment("reverse")
                .SetQueryParams(new
                {
                    lat = photoData.Latitude!.Value,
                    lon = photoData.Longitude!.Value,
                    format = "json",
                    zoom = settings.ZoomLevel,
                    addressdetails = 1
                })
                .WithHeader("User-Agent", settings.UserAgent)
                .GetJsonAsync<NominatimResponse>(cancellationToken: cancellationToken);

            PopulateLocationData(photoData, geocodeResult);

            if (!string.IsNullOrEmpty(photoData.City))
            {
                logger.LogDebug("Geocoded location: {City}, {State}, {Country}",
                    photoData.City, photoData.State, photoData.Country);
            }

            // Rate limiting: Nominatim requires max 1 request per second
            await Task.Delay(settings.RateLimitDelayMs, cancellationToken);
        }
        catch (FlurlHttpException ex)
        {
            logger.LogWarning(ex, "Geocoding failed (HTTP error)");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Geocoding cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Geocoding failed");
        }
    }

    private static void PopulateLocationData(PhotoData photoData, NominatimResponse? geocodeResult)
    {
        if (geocodeResult?.address is null)
            return;

        photoData.City = geocodeResult.address.BestCityName;
        photoData.State = geocodeResult.address.state;
        photoData.Country = geocodeResult.address.country;
        photoData.CountryCode = geocodeResult.address.country_code?.ToUpper();
        photoData.DisplayName = geocodeResult.display_name;
    }
}
