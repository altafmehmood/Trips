using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for writing photo data to JSON files
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class JsonOutputWriter(
    OutputSettings outputSettings,
    ILogger<JsonOutputWriter> logger) : IOutputWriter
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public async Task WriteAsync(
        IReadOnlyCollection<PhotoData> photoDataList,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photoDataList);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var options = GetJsonSerializerOptions();

        var json = JsonSerializer.Serialize(photoDataList, options);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);

        logger.LogInformation("Wrote {Count} photo records to {Path}", photoDataList.Count, outputPath);
    }

    public async Task AppendAsync(
        PhotoData photoData,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photoData);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            List<PhotoData> existingData;

            // Read existing file if it exists
            if (File.Exists(outputPath))
            {
                try
                {
                    var existingJson = await File.ReadAllTextAsync(outputPath, cancellationToken);
                    existingData = JsonSerializer.Deserialize<List<PhotoData>>(existingJson) ?? [];
                    logger.LogDebug("Read {Count} existing records from {Path}", existingData.Count, outputPath);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to parse existing JSON file, starting fresh");
                    existingData = [];
                }
            }
            else
            {
                existingData = [];
                logger.LogDebug("Creating new output file at {Path}", outputPath);
            }

            // Add new entry
            existingData.Add(photoData);

            // Write back to file
            var options = GetJsonSerializerOptions();
            var json = JsonSerializer.Serialize(existingData, options);
            await File.WriteAllTextAsync(outputPath, json, cancellationToken);

            logger.LogDebug("Appended photo data for {FileName} (total: {Count})", photoData.FileName, existingData.Count);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task UpdateAsync(
        PhotoData photoData,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photoData);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(outputPath))
            {
                logger.LogWarning("Cannot update - file does not exist: {Path}", outputPath);
                return;
            }

            // Read existing data
            var existingJson = await File.ReadAllTextAsync(outputPath, cancellationToken);
            var existingData = JsonSerializer.Deserialize<List<PhotoData>>(existingJson) ?? [];

            // Find and update the matching entry (by FileName)
            var index = existingData.FindIndex(p => p.FileName == photoData.FileName);
            if (index >= 0)
            {
                existingData[index] = photoData;

                // Write back to file
                var options = GetJsonSerializerOptions();
                var json = JsonSerializer.Serialize(existingData, options);
                await File.WriteAllTextAsync(outputPath, json, cancellationToken);

                logger.LogDebug("Updated photo data for {FileName}", photoData.FileName);
            }
            else
            {
                logger.LogWarning("Photo {FileName} not found in output file for update", photoData.FileName);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private JsonSerializerOptions GetJsonSerializerOptions() => new()
    {
        WriteIndented = outputSettings.WriteIndented,
        DefaultIgnoreCondition = outputSettings.IgnoreNullValues
            ? JsonIgnoreCondition.WhenWritingNull
            : JsonIgnoreCondition.Never
    };
}
