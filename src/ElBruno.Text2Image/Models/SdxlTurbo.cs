using ElBruno.Text2Image.Pipeline;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Models;

/// <summary>
/// SDXL Turbo text-to-image generator using ONNX Runtime.
/// Designed for 1-4 inference steps with no classifier-free guidance needed.
/// ONNX models exported and hosted at elbruno/sdxl-turbo-ONNX.
/// Note: SDXL uses a dual text encoder architecture; the pipeline uses the primary encoder.
/// </summary>
public sealed class SdxlTurbo : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private const string HuggingFaceRepo = "elbruno/sdxl-turbo-ONNX";
    private const string ModelSubfolder = "sdxl-turbo-onnx";
    private const int EmbeddingDim = 768; // Primary CLIP text encoder

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
        "text_encoder_2/model.onnx",
        "text_encoder_2/model.onnx_data",
        "text_encoder_2/config.json",
        "tokenizer_2/vocab.json",
        "tokenizer_2/merges.txt",
        "tokenizer_2/special_tokens_map.json",
        "tokenizer_2/tokenizer_config.json",
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
    private readonly object _pipelineLock = new();
    private StableDiffusionPipeline? _pipeline;

    /// <inheritdoc />
    public string ModelName => "SDXL Turbo";

    /// <summary>
    /// Creates a new SDXL Turbo generator. Defaults to 4 steps and no CFG.
    /// </summary>
    public SdxlTurbo(ImageGenerationOptions? defaultOptions = null)
    {
        _defaultOptions = defaultOptions ?? new ImageGenerationOptions
        {
            NumInferenceSteps = 4,
            GuidanceScale = 0.0, // SDXL Turbo doesn't need CFG
            Width = 512,
            Height = 512
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
            lock (_pipelineLock)
            {
                if (_pipeline == null)
                {
                    var sessionOptions = SessionOptionsHelper.Create(options.ExecutionProvider);
                    _pipeline = new StableDiffusionPipeline(modelPath, sessionOptions, EmbeddingDim);
                }
            }
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
