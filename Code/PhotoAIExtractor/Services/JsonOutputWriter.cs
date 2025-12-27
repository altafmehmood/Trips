using System.Text.Json;
using System.Text.Json.Serialization;
using PhotoAIExtractor.Configuration;
using PhotoAIExtractor.Interfaces;
using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Services;

/// <summary>
/// Service for writing photo data to JSON files
/// Uses primary constructor (C# 12)
/// </summary>
public sealed class JsonOutputWriter(OutputSettings outputSettings) : IOutputWriter
{
    public async Task WriteAsync(
        IReadOnlyCollection<PhotoData> photoDataList,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(photoDataList);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var options = new JsonSerializerOptions
        {
            WriteIndented = outputSettings.WriteIndented,
            DefaultIgnoreCondition = outputSettings.IgnoreNullValues
                ? JsonIgnoreCondition.WhenWritingNull
                : JsonIgnoreCondition.Never
        };

        var json = JsonSerializer.Serialize(photoDataList, options);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }
}
