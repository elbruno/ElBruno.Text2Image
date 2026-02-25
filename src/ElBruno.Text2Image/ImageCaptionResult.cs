namespace ElBruno.Text2Image;

/// <summary>
/// Represents the result of an image captioning operation.
/// </summary>
public sealed class ImageCaptionResult
{
    /// <summary>
    /// The generated caption text.
    /// </summary>
    public required string Caption { get; init; }

    /// <summary>
    /// The model used to generate the caption.
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// Time taken for inference in milliseconds.
    /// </summary>
    public long InferenceTimeMs { get; init; }
}
