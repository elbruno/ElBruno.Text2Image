namespace ElBruno.Text2Image;

/// <summary>
/// Interface for text-to-image generation models.
/// </summary>
public interface IImageGenerator : IDisposable
{
    /// <summary>
    /// Gets the name of this generation model.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Generates an image from a text prompt.
    /// </summary>
    /// <param name="prompt">The text description of the image to generate.</param>
    /// <param name="options">Optional generation options (steps, guidance, seed, size).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generation result containing the image data.</returns>
    Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the model files are downloaded and available.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
