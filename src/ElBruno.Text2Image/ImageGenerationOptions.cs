namespace ElBruno.Text2Image;

/// <summary>
/// Configuration options for image generation.
/// </summary>
public sealed class ImageGenerationOptions
{
    /// <summary>
    /// Local directory to store downloaded models. Defaults to a subfolder in the user's local app data.
    /// </summary>
    public string? ModelDirectory { get; set; }

    /// <summary>
    /// The execution provider to use for ONNX Runtime inference.
    /// Defaults to Auto (probes CUDA → DirectML → CPU).
    /// </summary>
    public ExecutionProvider ExecutionProvider { get; set; } = ExecutionProvider.Auto;

    /// <summary>
    /// Number of denoising steps. More steps = better quality but slower. Default is 20.
    /// </summary>
    public int NumInferenceSteps { get; set; } = 20;

    /// <summary>
    /// Classifier-free guidance scale. Higher values follow the prompt more closely. Default is 7.5.
    /// </summary>
    public double GuidanceScale { get; set; } = 7.5;

    /// <summary>
    /// Image width in pixels. Must be a multiple of 8. Default is 512.
    /// </summary>
    public int Width { get; set; } = 512;

    /// <summary>
    /// Image height in pixels. Must be a multiple of 8. Default is 512.
    /// </summary>
    public int Height { get; set; } = 512;

    /// <summary>
    /// Random seed for reproducible generation. If null, a random seed is used.
    /// </summary>
    public int? Seed { get; set; }

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
