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
    public int NumInferenceSteps
    {
        get => _numInferenceSteps;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 150);
            _numInferenceSteps = value;
        }
    }
    private int _numInferenceSteps = 20;

    /// <summary>
    /// Classifier-free guidance scale. Higher values follow the prompt more closely. Default is 7.5.
    /// </summary>
    public double GuidanceScale { get; set; } = 7.5;

    /// <summary>
    /// Image width in pixels. Must be a multiple of 8 and between 128 and 2048. Default is 512.
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 128);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2048);
            if (value % 8 != 0) throw new ArgumentException("Width must be a multiple of 8.", nameof(value));
            _width = value;
        }
    }
    private int _width = 512;

    /// <summary>
    /// Image height in pixels. Must be a multiple of 8 and between 128 and 2048. Default is 512.
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 128);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 2048);
            if (value % 8 != 0) throw new ArgumentException("Height must be a multiple of 8.", nameof(value));
            _height = value;
        }
    }
    private int _height = 512;

    /// <summary>
    /// Random seed for reproducible generation. If null, a random seed is used.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Optional reference images for image-to-image generation / image edits.
    /// Each entry can be an HTTPS URL, base64-encoded string, or Data URI.
    /// Supported by FLUX.2 (forwarded as <c>referenceImages</c>, up to 8 for FLUX.2-pro),
    /// GPT-Image-1.5/2/2.5 (Azure OpenAI <c>images/edits</c>, up to 16) and
    /// MAI-Image-2.5/2.5-Flash (<c>/mai/v1/images/edits</c>, up to 5). Defaults to null.
    /// </summary>
    public List<string>? ReferenceImages { get; set; }

    /// <summary>
    /// Optional PNG mask for inpainting/editing (HTTPS URL, base64-encoded string, or Data URI).
    /// Fully transparent areas indicate where the first reference image should be edited.
    /// Only supported by GPT-Image providers and requires at least one entry in <see cref="ReferenceImages"/>.
    /// </summary>
    public string? MaskImage { get; set; }

    /// <summary>
    /// Optional GPT-Image <c>input_fidelity</c> value (<c>low</c> or <c>high</c>) used for image edits.
    /// Ignored by providers that do not support it.
    /// </summary>
    public string? InputFidelity { get; set; }

    /// <summary>
    /// Reads a PNG mask file from disk and stores it in <see cref="MaskImage"/> as a base64 Data URI.
    /// </summary>
    /// <param name="filePath">Path to a PNG mask file.</param>
    public void SetMaskImageFromFile(string filePath)
    {
        MaskImage = ReferenceImageHelper.LoadFileAsDataUri(filePath, ReferenceImageHelper.MaxMaskImageBytes, ["image/png"]);
    }

    /// <summary>
    /// Reads an image file from disk, converts it to a base64 Data URI,
    /// and appends it to <see cref="ReferenceImages"/>.
    /// </summary>
    /// <param name="filePath">Path to an image file (png, jpg, jpeg, gif, webp, bmp).</param>
    public void AddReferenceImageFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var bytes = File.ReadAllBytes(filePath);
        var mimeType = Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };

        var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        ReferenceImages ??= new List<string>();
        ReferenceImages.Add(dataUri);
    }

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
