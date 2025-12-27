# Photos Directory - CLAUDE.md

This directory contains photos for the 2025 Australia trip that will be published on the web.

## Purpose

Process raw travel photos for web publication by:
1. Extracting metadata (date, GPS coordinates)
2. Converting GPS coordinates to human-readable locations
3. Optimizing images for web display
4. Generating a metadata file for easy integration

## Directory Structure

```
Photos/
├── originals/          # Backup of original unprocessed photos
├── optimized/          # Web-optimized versions (max 1920px width)
├── metadata.json       # Extracted metadata for all photos
├── process_photos.py   # Photo processing script
└── CLAUDE.md          # This file
```

## Photo Processing Specifications

### Image Optimization
- **Max width**: 1920px (full web size)
- **Format**: Progressive JPEG
- **Quality**: 85%
- **HEIC handling**: Automatically convert to JPEG
- **Metadata stripping**: Remove EXIF from optimized versions (metadata preserved in JSON)

### Metadata Extraction
- Date/time photo was taken
- GPS coordinates (latitude, longitude)
- Original image dimensions and file size
- Optimized image dimensions and file size

### Location Resolution
- **Service**: OpenStreetMap Nominatim API
- **Rate limiting**: 1 request per second (API requirement)
- **Output**: City, state, country, formatted display name
- **Caching**: Avoid duplicate geocoding requests

## Metadata File Format

The `metadata.json` file contains:
```json
{
  "photos": [
    {
      "original_filename": "IMG_2581.jpeg",
      "optimized_filename": "IMG_2581.jpeg",
      "date_taken": "2024-12-16T21:22:00",
      "gps": {
        "latitude": -37.8136,
        "longitude": 144.9631
      },
      "location": {
        "city": "Melbourne",
        "state": "Victoria",
        "country": "Australia",
        "display_name": "Melbourne, Victoria, Australia"
      },
      "original_size": {
        "width": 4032,
        "height": 3024,
        "file_size_mb": 3.0
      },
      "optimized_size": {
        "width": 1920,
        "height": 1440,
        "file_size_mb": 0.4
      }
    }
  ],
  "summary": {
    "total_photos": 90,
    "processing_date": "2025-12-16",
    "total_size_saved_mb": 150.5
  }
}
```

## Dependencies

Python packages required:
- `Pillow` - Image processing and EXIF extraction
- `piexif` - Enhanced EXIF handling
- `requests` - API calls to OpenStreetMap
- `pillow-heif` - HEIC format support

Install with:
```bash
pip install Pillow piexif requests pillow-heif
```

## Usage

```bash
# Process all photos in current directory
python process_photos.py

# Output:
# - Original photos moved to originals/
# - Optimized photos saved to optimized/
# - Metadata saved to metadata.json
# - Summary report printed to console
```

## Integration with Trip Itinerary

The `metadata.json` file can be used to:
- Display photos on interactive maps using GPS coordinates
- Sort photos chronologically by date taken
- Show location names alongside photos
- Create photo galleries organized by location or date

## Notes

- Original photos are preserved in `originals/` directory
- Processing is non-destructive - can be re-run if needed
- Photos without GPS data will have `null` location information
- OpenStreetMap Nominatim has usage limits - script includes rate limiting
- HEIC files are automatically detected and converted to JPEG
