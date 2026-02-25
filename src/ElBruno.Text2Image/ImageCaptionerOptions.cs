namespace ElBruno.Text2Image;

/// <summary>
/// Configuration options for image captioning.
/// </summary>
public sealed class ImageCaptionerOptions
{
    /// <summary>
    /// Local directory to store downloaded models. Defaults to a subfolder in the user's local app data.
    /// </summary>
    public string? ModelDirectory { get; set; }

    /// <summary>
    /// The execution provider to use for ONNX Runtime inference.
    /// </summary>
    public ExecutionProvider ExecutionProvider { get; set; } = ExecutionProvider.Cpu;

    /// <summary>
    /// Maximum number of tokens to generate. Default is 50.
    /// </summary>
    public int MaxTokens { get; set; } = 50;

    /// <summary>
    /// Whether to use quantized model variants when available. Default is false.
    /// </summary>
    public bool UseQuantized { get; set; }

    /// <summary>
    /// Optional text prompt for models that support conditional captioning (e.g., BLIP).
    /// </summary>
    public string? TextPrompt { get; set; }

    /// <summary>
    /// Gets the resolved model directory path.
    /// </summary>
    internal string GetModelDirectory(string modelSubfolder)
    {
        var baseDir = ModelDirectory
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ElBruno", "Text2Image");
        return Path.Combine(baseDir, modelSubfolder);
    }
}
