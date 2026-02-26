using ElBruno.Text2Image.Pipeline;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Models;

/// <summary>
/// Stable Diffusion 1.5 text-to-image generator using ONNX Runtime.
/// Uses pre-exported ONNX models from onnx-community/stable-diffusion-v1-5-ONNX on HuggingFace.
/// Implements both the ElBruno.Text2Image and Microsoft.Extensions.AI image generation interfaces.
/// </summary>
public sealed class StableDiffusion15 : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private const string HuggingFaceRepo = "onnx-community/stable-diffusion-v1-5-ONNX";
    private const string ModelSubfolder = "stable-diffusion-v1-5-onnx";
    private const int EmbeddingDim = 768;

    private static readonly string[] RequiredFiles = new[]
    {
        "text_encoder/model.onnx",
        "unet/model.onnx",
        "unet/weights.pb",
        "vae_decoder/model.onnx",
        "tokenizer/vocab.json",
        "tokenizer/merges.txt",
        "scheduler/scheduler_config.json"
    };

    private static readonly string[] OptionalFiles = new[]
    {
        "vae_encoder/model.onnx",
        "safety_checker/model.onnx",
        "tokenizer/special_tokens_map.json",
        "tokenizer/tokenizer_config.json"
    };

    private readonly ImageGenerationOptions _defaultOptions;
    private StableDiffusionPipeline? _pipeline;

    /// <inheritdoc />
    public string ModelName => "Stable Diffusion 1.5";

    /// <summary>
    /// Creates a new Stable Diffusion 1.5 generator with optional default options.
    /// </summary>
    public StableDiffusion15(ImageGenerationOptions? defaultOptions = null)
    {
        _defaultOptions = defaultOptions ?? new ImageGenerationOptions();
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

    /// <summary>
    /// Generates an image using the Microsoft.Extensions.AI interface.
    /// </summary>
    async Task<ImageGenerationResponse> Microsoft.Extensions.AI.IImageGenerator.GenerateAsync(
        ImageGenerationRequest request,
        Microsoft.Extensions.AI.ImageGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var localOptions = ImageGenerationOptionsConverter.FromMeaiOptions(options);
        var result = await GenerateAsync(request.Prompt ?? "", localOptions, cancellationToken);
        return ImageGenerationOptionsConverter.ToMeaiResponse(result);
    }

    /// <inheritdoc />
    object? Microsoft.Extensions.AI.IImageGenerator.GetService(Type serviceType, object? serviceKey)
        => serviceType == GetType() ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
        _pipeline?.Dispose();
        _pipeline = null;
    }
}
