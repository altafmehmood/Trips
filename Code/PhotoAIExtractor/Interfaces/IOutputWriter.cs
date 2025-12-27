using PhotoAIExtractor.Models;

namespace PhotoAIExtractor.Interfaces;

/// <summary>
/// Defines contract for writing output data
/// </summary>
public interface IOutputWriter
{
    /// <summary>
    /// Writes photo data to output file
    /// </summary>
    /// <param name="photoDataList">Collection of photo data to write</param>
    /// <param name="outputPath">Path to output file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WriteAsync(IReadOnlyCollection<PhotoData> photoDataList, string outputPath, CancellationToken cancellationToken = default);
}
