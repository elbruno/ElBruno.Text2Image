namespace ElBruno.Text2Image;

/// <summary>
/// Interface for image captioning models.
/// </summary>
public interface IImageCaptioner : IDisposable
{
    /// <summary>
    /// Gets the name of this captioning model.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Generates a caption for the specified image file.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="options">Optional captioning options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captioning result.</returns>
    Task<ImageCaptionResult> CaptionAsync(
        string imagePath,
        ImageCaptionerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a caption for the specified image stream.
    /// </summary>
    /// <param name="imageStream">Stream containing image data.</param>
    /// <param name="options">Optional captioning options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The captioning result.</returns>
    Task<ImageCaptionResult> CaptionAsync(
        Stream imageStream,
        ImageCaptionerOptions? options = null,
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
