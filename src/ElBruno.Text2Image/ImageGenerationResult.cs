namespace ElBruno.Text2Image;

/// <summary>
/// Represents the result of a text-to-image generation operation.
/// </summary>
public sealed class ImageGenerationResult
{
    /// <summary>
    /// The generated image as PNG bytes.
    /// </summary>
    public required byte[] ImageBytes { get; init; }

    /// <summary>
    /// The model used to generate the image.
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// The prompt used to generate the image.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// The seed used for generation (for reproducibility).
    /// </summary>
    public int Seed { get; init; }

    /// <summary>
    /// Time taken for inference in milliseconds.
    /// </summary>
    public long InferenceTimeMs { get; init; }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Saves the generated image to a file.
    /// </summary>
    /// <param name="filePath">The path to save the image to.</param>
    public async Task SaveAsync(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(filePath, ImageBytes);
    }
}
