using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;
using System.Collections.Concurrent;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for reverse geocoding GPS coordinates using Nominatim API
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class GeocodingService(
    GeocodingSettings settings,
    ILogger<GeocodingService> logger) : IGeocodingService
{
    // Cache for geocoding results to avoid redundant API calls
    // Key: rounded coordinates (to ~100m precision), Value: cached location data
    private readonly ConcurrentDictionary<string, CachedLocation> _geocodeCache = new();
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);

    public async Task ReverseGeocodeAsync(PhotoData photoData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photoData);

        if (!photoData.HasGpsData)
            return;

        // Create cache key by rounding coordinates to ~100m precision (3 decimal places)
        var cacheKey = $"{photoData.Latitude!.Value:F3},{photoData.Longitude!.Value:F3}";

        // Check cache first
        if (_geocodeCache.TryGetValue(cacheKey, out var cachedLocation))
        {
            logger.LogDebug("Using cached geocoding result for: {Lat}, {Lon}", photoData.Latitude, photoData.Longitude);
            ApplyCachedLocation(photoData, cachedLocation);
            return;
        }

        try
        {
            logger.LogDebug("Reverse geocoding coordinates: {Lat}, {Lon}", photoData.Latitude, photoData.Longitude);

            // Use semaphore to ensure rate limiting across parallel requests
            await _rateLimiter.WaitAsync(cancellationToken);
            try
            {
                var geocodeResult = await settings.BaseUrl
                    .AppendPathSegment("reverse")
                    .SetQueryParams(new
                    {
                        lat = photoData.Latitude.Value,
                        lon = photoData.Longitude.Value,
                        format = "json",
                        zoom = settings.ZoomLevel,
                        addressdetails = 1
                    })
                    .WithHeader("User-Agent", settings.UserAgent)
                    .GetJsonAsync<NominatimResponse>(cancellationToken: cancellationToken);

                PopulateLocationData(photoData, geocodeResult);

                // Cache the result
                var cached = new CachedLocation(
                    photoData.City,
                    photoData.State,
                    photoData.Country,
                    photoData.CountryCode,
                    photoData.DisplayName,
                    photoData.NationalPark,
                    photoData.ProtectedArea,
                    photoData.Region);
                _geocodeCache.TryAdd(cacheKey, cached);

                if (!string.IsNullOrEmpty(photoData.City))
                {
                    logger.LogDebug("Geocoded location: {City}, {State}, {Country}",
                        photoData.City, photoData.State, photoData.Country);
                }

                // Rate limiting: Nominatim requires max 1 request per second
                await Task.Delay(settings.RateLimitDelayMs, cancellationToken);
            }
            finally
            {
                _rateLimiter.Release();
            }
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

    private static void ApplyCachedLocation(PhotoData photoData, CachedLocation cached)
    {
        photoData.City = cached.City;
        photoData.State = cached.State;
        photoData.Country = cached.Country;
        photoData.CountryCode = cached.CountryCode;
        photoData.DisplayName = cached.DisplayName;
        photoData.NationalPark = cached.NationalPark;
        photoData.ProtectedArea = cached.ProtectedArea;
        photoData.Region = cached.Region;
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

        // Populate special locations (national parks, protected areas, etc.)
        photoData.NationalPark = geocodeResult.address.national_park;
        photoData.ProtectedArea = geocodeResult.address.protected_area ?? geocodeResult.address.nature_reserve;
        photoData.Region = geocodeResult.address.BestRegionalLocation;
    }
}

/// <summary>
/// Cached geocoding location data
/// </summary>
internal record CachedLocation(
    string? City,
    string? State,
    string? Country,
    string? CountryCode,
    string? DisplayName,
    string? NationalPark,
    string? ProtectedArea,
    string? Region);
