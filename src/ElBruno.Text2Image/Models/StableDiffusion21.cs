using ElBruno.Text2Image.Pipeline;

namespace ElBruno.Text2Image.Models;

/// <summary>
/// Stable Diffusion 2.1 text-to-image generator using ONNX Runtime.
/// Uses OpenCLIP ViT-H text encoder (1024 embedding dimension).
/// ONNX models exported and hosted at elbruno/stable-diffusion-2-1-ONNX.
/// </summary>
public sealed class StableDiffusion21 : IImageGenerator
{
    private const string HuggingFaceRepo = "elbruno/stable-diffusion-2-1-ONNX";
    private const string ModelSubfolder = "stable-diffusion-2-1-onnx";
    private const int EmbeddingDim = 1024; // SD 2.1 uses OpenCLIP with 1024-dim embeddings

    private static readonly string[] RequiredFiles = new[]
    {
        "text_encoder/model.onnx",
        "unet/model.onnx",
        "unet/model.onnx_data",
        "vae_decoder/model.onnx",
        "tokenizer/vocab.json",
        "tokenizer/merges.txt"
    };

    private static readonly string[] OptionalFiles = new[]
    {
        "scheduler/scheduler_config.json",
        "vae_encoder/model.onnx",
        "text_encoder/config.json",
        "unet/config.json",
        "vae_decoder/config.json",
        "vae_encoder/config.json",
        "tokenizer/special_tokens_map.json",
        "tokenizer/tokenizer_config.json",
        "model_index.json"
    };

    private readonly ImageGenerationOptions _defaultOptions;
    private StableDiffusionPipeline? _pipeline;

    /// <inheritdoc />
    public string ModelName => "Stable Diffusion 2.1";

    /// <summary>
    /// Creates a new Stable Diffusion 2.1 generator.
    /// </summary>
    public StableDiffusion21(ImageGenerationOptions? defaultOptions = null)
    {
        _defaultOptions = defaultOptions ?? new ImageGenerationOptions
        {
            Width = 768,
            Height = 768
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
            _pipeline = new StableDiffusionPipeline(modelPath, sessionOptions, EmbeddingDim);
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
