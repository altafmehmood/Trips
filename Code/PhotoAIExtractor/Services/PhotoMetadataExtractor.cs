using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for extracting metadata from photo files
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class PhotoMetadataExtractor(IGeocodingService geocodingService) : IPhotoMetadataExtractor
{
    public async Task<PhotoData> ExtractPhotoDataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var photoData = new PhotoData
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length
        };

        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            ExtractGpsData(directories, photoData);
            ExtractExifData(directories, photoData);
            ExtractCameraInfo(directories, photoData);
            ExtractImageDimensions(directories, photoData);

            // Reverse geocode if GPS data is available
            if (photoData.HasGpsData)
            {
                await geocodingService.ReverseGeocodeAsync(photoData, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            photoData.Error = ex.Message;
        }

        return photoData;
    }

    private static void ExtractGpsData(IEnumerable<MetadataExtractor.Directory> directories, PhotoData photoData)
    {
        var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
        if (gpsDirectory is null)
            return;

        if (gpsDirectory.TryGetGeoLocation(out var location))
        {
            photoData.Latitude = location.Latitude;
            photoData.Longitude = location.Longitude;
        }

        photoData.Altitude = gpsDirectory.GetDescription(GpsDirectory.TagAltitude);
    }

    private static void ExtractExifData(IEnumerable<MetadataExtractor.Directory> directories, PhotoData photoData)
    {
        var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
        if (exifSubIfdDirectory is null)
            return;

        photoData.DateTaken = exifSubIfdDirectory.GetDescription(ExifSubIfdDirectory.TagDateTimeOriginal);
        photoData.ExposureTime = exifSubIfdDirectory.GetDescription(ExifSubIfdDirectory.TagExposureTime);
        photoData.FNumber = exifSubIfdDirectory.GetDescription(ExifSubIfdDirectory.TagFNumber);
        photoData.ISO = exifSubIfdDirectory.GetDescription(ExifSubIfdDirectory.TagIsoEquivalent);
        photoData.FocalLength = exifSubIfdDirectory.GetDescription(ExifSubIfdDirectory.TagFocalLength);
        photoData.LensModel = exifSubIfdDirectory.GetDescription(ExifSubIfdDirectory.TagLensModel);
    }

    private static void ExtractCameraInfo(IEnumerable<MetadataExtractor.Directory> directories, PhotoData photoData)
    {
        var exifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
        if (exifIfd0Directory is null)
            return;

        photoData.CameraMake = exifIfd0Directory.GetDescription(ExifIfd0Directory.TagMake);
        photoData.CameraModel = exifIfd0Directory.GetDescription(ExifIfd0Directory.TagModel);
        photoData.Orientation = exifIfd0Directory.GetDescription(ExifIfd0Directory.TagOrientation);
        photoData.Software = exifIfd0Directory.GetDescription(ExifIfd0Directory.TagSoftware);
    }

    private static void ExtractImageDimensions(IEnumerable<MetadataExtractor.Directory> directories, PhotoData photoData)
    {
        const int widthTag = 0x0100;
        const int heightTag = 0x0101;

        foreach (var directory in directories)
        {
            if (directory.ContainsTag(widthTag) && directory.ContainsTag(heightTag))
            {
                photoData.Width = directory.GetDescription(widthTag);
                photoData.Height = directory.GetDescription(heightTag);
                break;
            }
        }
    }
}
