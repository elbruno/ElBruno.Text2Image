using ElBruno.Text2Image.Pipeline;

namespace ElBruno.Text2Image.Models;

/// <summary>
/// LCM Dreamshaper v7 text-to-image generator using ONNX Runtime.
/// Uses Latent Consistency Models for very fast (2-4 step) inference.
/// ONNX models from TheyCallMeHex/LCM-Dreamshaper-V7-ONNX on HuggingFace.
/// </summary>
public sealed class LcmDreamshaperV7 : IImageGenerator
{
    private const string HuggingFaceRepo = "TheyCallMeHex/LCM-Dreamshaper-V7-ONNX";
    private const string ModelSubfolder = "lcm-dreamshaper-v7-onnx";
    private const int EmbeddingDim = 768;

    private static readonly string[] RequiredFiles = new[]
    {
        "text_encoder/model.onnx",
        "unet/model.onnx",
        "vae_decoder/model.onnx",
        "tokenizer/vocab.json",
        "tokenizer/merges.txt",
        "scheduler/scheduler_config.json"
    };

    private static readonly string[] OptionalFiles = new[]
    {
        "unet/weights.pb",
        "vae_encoder/model.onnx",
        "safety_checker/model.onnx",
        "tokenizer/special_tokens_map.json",
        "tokenizer/tokenizer_config.json"
    };

    private readonly ImageGenerationOptions _defaultOptions;
    private StableDiffusionPipeline? _pipeline;

    /// <inheritdoc />
    public string ModelName => "LCM Dreamshaper v7";

    /// <summary>
    /// Creates a new LCM Dreamshaper v7 generator with optional default options.
    /// LCM models work best with 2-8 steps and guidance_scale=1.0 (no CFG).
    /// </summary>
    public LcmDreamshaperV7(ImageGenerationOptions? defaultOptions = null)
    {
        _defaultOptions = defaultOptions ?? new ImageGenerationOptions
        {
            NumInferenceSteps = 4,
            GuidanceScale = 1.0 // LCM doesn't need CFG
        };
    }

    /// <inheritdoc />
    public async Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var modelPath = _defaultOptions.GetModelDirectory(ModelSubfolder);
        await ModelManager.EnsureModelAvailableAsync(
            modelPath, HuggingFaceRepo, RequiredFiles, OptionalFiles, progress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= _defaultOptions;
        var modelPath = options.GetModelDirectory(ModelSubfolder);

        await ModelManager.EnsureModelAvailableAsync(
            modelPath, HuggingFaceRepo, RequiredFiles, OptionalFiles, cancellationToken: cancellationToken);

        if (_pipeline == null)
        {
            var sessionOptions = SessionOptionsHelper.Create(options.ExecutionProvider);
            _pipeline = new StableDiffusionPipeline(modelPath, sessionOptions, EmbeddingDim, useLcmScheduler: true);
        }

        return await Task.Run(() => _pipeline.Generate(prompt, options, ModelName), cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _pipeline?.Dispose();
        _pipeline = null;
    }
}
