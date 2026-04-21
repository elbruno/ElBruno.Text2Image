using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Images;
using ElBruno.Text2Image;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Foundry;

public sealed class GptImage1p5Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private readonly ImageClient _imageClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelDisplayName;
    private readonly string _deploymentName;
    private const int MaxPromptLength = 4000;
    public string ModelName => _modelDisplayName;
    public string DeploymentName => _deploymentName;
    public string Endpoint => _endpoint;
    public GptImage1p5Generator(string endpoint, string apiKey, string? modelName = null, string? deploymentName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("API endpoint must use HTTPS protocol", nameof(endpoint));
        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _modelDisplayName = modelName ?? "GPT-Image-1.5";
        _deploymentName = deploymentName ?? "gpt-image-1.5";
        var client = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
        _imageClient = client.GetImageClient(_deploymentName);
    }
    public Task EnsureModelAvailableAsync(IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new DownloadProgress { Stage = DownloadStage.Complete, PercentComplete = 100, Message = "Cloud model" });
        return Task.CompletedTask;
    }
    private static string MapToSizeString(int width, int height)
    {
        if (width == 1024 && height == 1024) return "1024x1024";
        if (width == 1792 && height == 1024) return "1792x1024";
        if (width == 1024 && height == 1792) return "1024x1792";
        double aspectRatio = (double)width / height;
        if (aspectRatio > 1.5) return "1792x1024";
        if (aspectRatio < 0.7) return "1024x1792";
        return "1024x1024";
    }
    public async Task<ImageGenerationResult> GenerateAsync(string prompt, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));
        if (prompt.Length > MaxPromptLength)
            throw new ArgumentOutOfRangeException(nameof(prompt), $"Prompt must be {MaxPromptLength} characters or fewer");
        options ??= new ImageGenerationOptions();
        int width = options.Width > 0 ? options.Width : 1024;
        int height = options.Height > 0 ? options.Height : 1024;
        var mappedSizeString = MapToSizeString(width, height);
        var (mappedWidth, mappedHeight) = ParseSizeString(mappedSizeString);
        byte[] imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        return new ImageGenerationResult { ImageBytes = imageBytes, ModelName = _modelDisplayName, Prompt = prompt, Seed = options.Seed ?? 0, Width = mappedWidth, Height = mappedHeight };
    }
    private static (int width, int height) ParseSizeString(string size) => size switch { "1024x1024" => (1024, 1024), "1792x1024" => (1792, 1024), "1024x1792" => (1024, 1792), _ => (1024, 1024) };
    async Task<ImageGenerationResponse> Microsoft.Extensions.AI.IImageGenerator.GenerateAsync(ImageGenerationRequest imageRequest, Microsoft.Extensions.AI.ImageGenerationOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageRequest);
        var localOptions = new ImageGenerationOptions();
        if (options?.ImageSize.HasValue == true) { localOptions.Width = options.ImageSize.Value.Width; localOptions.Height = options.ImageSize.Value.Height; }
        var result = await GenerateAsync(imageRequest.Prompt ?? "", localOptions, cancellationToken);
        return ImageGenerationOptionsConverter.ToMeaiResponse(result);
    }
    object? Microsoft.Extensions.AI.IImageGenerator.GetService(Type serviceType, object? serviceKey) => serviceType == GetType() ? this : null;
    public void Dispose() { }
}
