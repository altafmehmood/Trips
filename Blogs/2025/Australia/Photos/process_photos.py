#!/usr/bin/env python3
"""
Photo Processing Script for Web Publication
Extracts metadata, optimizes images, and resolves GPS locations.
"""

import os
import json
import shutil
import time
from pathlib import Path
from datetime import datetime
from typing import Dict, List, Optional, Tuple

try:
    from PIL import Image
    from PIL.ExifTags import TAGS, GPSTAGS
    import piexif
    import requests
    from pillow_heif import register_heif_opener
except ImportError as e:
    print(f"Error: Missing required package. Please run:")
    print("pip install Pillow piexif requests pillow-heif")
    exit(1)

# Register HEIF opener for HEIC support
register_heif_opener()

# Configuration
MAX_WIDTH = 1920
JPEG_QUALITY = 85
NOMINATIM_DELAY = 1.1  # Seconds between API calls (slightly over 1s for safety)
USER_AGENT = "TravelPhotoProcessor/1.0"

class PhotoProcessor:
    def __init__(self, source_dir: str = "."):
        self.source_dir = Path(source_dir)
        self.originals_dir = self.source_dir / "originals"
        self.optimized_dir = self.source_dir / "optimized"
        self.metadata_file = self.source_dir / "metadata.json"

        self.photos_data = []
        self.location_cache = {}  # Cache for GPS coordinates
        self.total_original_size = 0
        self.total_optimized_size = 0

    def setup_directories(self):
        """Create necessary directories."""
        self.originals_dir.mkdir(exist_ok=True)
        self.optimized_dir.mkdir(exist_ok=True)
        print(f"✓ Created directories: originals/ and optimized/")

    def get_image_files(self) -> List[Path]:
        """Get all image files from source directory."""
        extensions = {'.jpg', '.jpeg', '.heic', '.HEIC'}
        files = []

        for ext in extensions:
            files.extend(self.source_dir.glob(f"*{ext}"))

        # Filter out files already in subdirectories
        files = [f for f in files if f.parent == self.source_dir]
        return sorted(files)

    def get_exif_data(self, image_path: Path) -> Dict:
        """Extract EXIF data from image."""
        try:
            img = Image.open(image_path)
            exif_data = img._getexif()

            if not exif_data:
                return {}

            exif = {}
            for tag_id, value in exif_data.items():
                tag = TAGS.get(tag_id, tag_id)
                exif[tag] = value

            return exif
        except Exception as e:
            print(f"  Warning: Could not read EXIF from {image_path.name}: {e}")
            return {}

    def get_gps_coordinates(self, exif: Dict) -> Optional[Tuple[float, float]]:
        """Extract GPS coordinates from EXIF data."""
        if 'GPSInfo' not in exif:
            return None

        gps_info = {}
        for key in exif['GPSInfo'].keys():
            decode = GPSTAGS.get(key, key)
            gps_info[decode] = exif['GPSInfo'][key]

        def convert_to_degrees(value):
            """Convert GPS coordinates to degrees."""
            d, m, s = value
            return d + (m / 60.0) + (s / 3600.0)

        try:
            lat = convert_to_degrees(gps_info['GPSLatitude'])
            lon = convert_to_degrees(gps_info['GPSLongitude'])

            if gps_info['GPSLatitudeRef'] == 'S':
                lat = -lat
            if gps_info['GPSLongitudeRef'] == 'W':
                lon = -lon

            return (lat, lon)
        except (KeyError, TypeError, ZeroDivisionError):
            return None

    def get_datetime_taken(self, exif: Dict) -> Optional[str]:
        """Extract datetime from EXIF data."""
        for key in ['DateTimeOriginal', 'DateTime', 'DateTimeDigitized']:
            if key in exif:
                try:
                    # Format: "2024:12:16 21:22:00" -> "2024-12-16T21:22:00"
                    dt_str = exif[key]
                    dt = datetime.strptime(dt_str, "%Y:%m:%d %H:%M:%S")
                    return dt.strftime("%Y-%m-%dT%H:%M:%S")
                except ValueError:
                    continue
        return None

    def reverse_geocode(self, lat: float, lon: float) -> Optional[Dict]:
        """Get location name from GPS coordinates using Nominatim."""
        cache_key = f"{lat:.6f},{lon:.6f}"

        # Check cache first
        if cache_key in self.location_cache:
            return self.location_cache[cache_key]

        url = "https://nominatim.openstreetmap.org/reverse"
        params = {
            'lat': lat,
            'lon': lon,
            'format': 'json',
            'zoom': 14,
            'addressdetails': 1
        }
        headers = {
            'User-Agent': USER_AGENT
        }

        try:
            time.sleep(NOMINATIM_DELAY)  # Rate limiting
            response = requests.get(url, params=params, headers=headers, timeout=10)

            if response.status_code == 200:
                data = response.json()
                address = data.get('address', {})

                location = {
                    'city': (address.get('city') or
                            address.get('town') or
                            address.get('village') or
                            address.get('suburb')),
                    'state': address.get('state'),
                    'country': address.get('country'),
                    'display_name': data.get('display_name')
                }

                # Cache the result
                self.location_cache[cache_key] = location
                return location
            else:
                print(f"  Warning: Geocoding failed with status {response.status_code}")
                return None

        except Exception as e:
            print(f"  Warning: Geocoding error: {e}")
            return None

    def optimize_image(self, input_path: Path, output_path: Path) -> Tuple[int, int]:
        """Optimize and resize image for web."""
        img = Image.open(input_path)

        # Convert to RGB if necessary (for HEIC, PNG with transparency, etc.)
        if img.mode in ('RGBA', 'LA', 'P'):
            background = Image.new('RGB', img.size, (255, 255, 255))
            if img.mode == 'P':
                img = img.convert('RGBA')
            background.paste(img, mask=img.split()[-1] if img.mode in ('RGBA', 'LA') else None)
            img = background
        elif img.mode != 'RGB':
            img = img.convert('RGB')

        # Resize if width exceeds MAX_WIDTH
        width, height = img.size
        if width > MAX_WIDTH:
            new_height = int((MAX_WIDTH / width) * height)
            img = img.resize((MAX_WIDTH, new_height), Image.Resampling.LANCZOS)

        # Save as progressive JPEG
        img.save(
            output_path,
            'JPEG',
            quality=JPEG_QUALITY,
            optimize=True,
            progressive=True
        )

        return img.size

    def process_photo(self, photo_path: Path, index: int, total: int) -> Dict:
        """Process a single photo."""
        print(f"\n[{index}/{total}] Processing {photo_path.name}")

        # Get original file size
        original_size_bytes = photo_path.stat().st_size
        self.total_original_size += original_size_bytes

        # Extract EXIF data
        exif = self.get_exif_data(photo_path)

        # Get original dimensions
        with Image.open(photo_path) as img:
            original_width, original_height = img.size

        # Extract metadata
        date_taken = self.get_datetime_taken(exif)
        gps_coords = self.get_gps_coordinates(exif)

        print(f"  Date: {date_taken or 'Not found'}")
        print(f"  GPS: {gps_coords or 'Not found'}")

        # Reverse geocode if GPS available
        location = None
        if gps_coords:
            print(f"  Looking up location...")
            location = self.reverse_geocode(gps_coords[0], gps_coords[1])
            if location:
                print(f"  Location: {location.get('display_name', 'Unknown')}")

        # Determine output filename (convert HEIC to JPEG)
        output_filename = photo_path.stem + '.jpeg'
        output_path = self.optimized_dir / output_filename

        # Optimize image
        print(f"  Optimizing image...")
        optimized_width, optimized_height = self.optimize_image(photo_path, output_path)

        optimized_size_bytes = output_path.stat().st_size
        self.total_optimized_size += optimized_size_bytes

        size_saved = original_size_bytes - optimized_size_bytes
        reduction_pct = (size_saved / original_size_bytes) * 100
        print(f"  Size: {original_size_bytes / 1024 / 1024:.2f}MB → {optimized_size_bytes / 1024 / 1024:.2f}MB ({reduction_pct:.1f}% reduction)")

        # Move original to originals directory
        original_dest = self.originals_dir / photo_path.name
        shutil.move(str(photo_path), str(original_dest))

        # Build metadata entry
        metadata = {
            'original_filename': photo_path.name,
            'optimized_filename': output_filename,
            'date_taken': date_taken,
            'gps': {
                'latitude': gps_coords[0] if gps_coords else None,
                'longitude': gps_coords[1] if gps_coords else None
            } if gps_coords else None,
            'location': location,
            'original_size': {
                'width': original_width,
                'height': original_height,
                'file_size_mb': round(original_size_bytes / 1024 / 1024, 2)
            },
            'optimized_size': {
                'width': optimized_width,
                'height': optimized_height,
                'file_size_mb': round(optimized_size_bytes / 1024 / 1024, 2)
            }
        }

        return metadata

    def save_metadata(self):
        """Save metadata to JSON file."""
        output = {
            'photos': self.photos_data,
            'summary': {
                'total_photos': len(self.photos_data),
                'processing_date': datetime.now().strftime("%Y-%m-%d"),
                'total_original_size_mb': round(self.total_original_size / 1024 / 1024, 2),
                'total_optimized_size_mb': round(self.total_optimized_size / 1024 / 1024, 2),
                'total_size_saved_mb': round((self.total_original_size - self.total_optimized_size) / 1024 / 1024, 2)
            }
        }

        with open(self.metadata_file, 'w', encoding='utf-8') as f:
            json.dump(output, f, indent=2, ensure_ascii=False)

        print(f"\n✓ Metadata saved to {self.metadata_file}")

    def print_summary(self):
        """Print processing summary."""
        print("\n" + "="*60)
        print("PROCESSING COMPLETE")
        print("="*60)
        print(f"Total photos processed: {len(self.photos_data)}")
        print(f"Original size: {self.total_original_size / 1024 / 1024:.2f} MB")
        print(f"Optimized size: {self.total_optimized_size / 1024 / 1024:.2f} MB")
        print(f"Total saved: {(self.total_original_size - self.total_optimized_size) / 1024 / 1024:.2f} MB")

        reduction_pct = ((self.total_original_size - self.total_optimized_size) / self.total_original_size) * 100
        print(f"Size reduction: {reduction_pct:.1f}%")

        photos_with_gps = sum(1 for p in self.photos_data if p.get('gps'))
        photos_with_location = sum(1 for p in self.photos_data if p.get('location'))
        photos_with_date = sum(1 for p in self.photos_data if p.get('date_taken'))

        print(f"\nMetadata extracted:")
        print(f"  Photos with dates: {photos_with_date}/{len(self.photos_data)}")
        print(f"  Photos with GPS: {photos_with_gps}/{len(self.photos_data)}")
        print(f"  Photos with location: {photos_with_location}/{len(self.photos_data)}")
        print("\n" + "="*60)

    def run(self):
        """Main processing pipeline."""
        print("Photo Processing Pipeline")
        print("="*60)

        # Setup
        self.setup_directories()

        # Get image files
        image_files = self.get_image_files()
        total_files = len(image_files)

        if total_files == 0:
            print("No image files found to process.")
            return

        print(f"\nFound {total_files} image files to process")

        # Process each photo
        for index, photo_path in enumerate(image_files, 1):
            try:
                metadata = self.process_photo(photo_path, index, total_files)
                self.photos_data.append(metadata)
            except Exception as e:
                print(f"  ERROR processing {photo_path.name}: {e}")
                continue

        # Save metadata
        if self.photos_data:
            self.save_metadata()
            self.print_summary()
        else:
            print("\nNo photos were successfully processed.")

if __name__ == "__main__":
    processor = PhotoProcessor()
    processor.run()
