using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Images;
using ElBruno.Text2Image;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Foundry;

/// <summary>
/// GPT-Image-2 Azure OpenAI image generator.
/// Generates images via Azure OpenAI Service with support for fixed sizes: 1024×1024, 1024×1536, 1536×1024.
/// </summary>
public sealed class GptImage2Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private readonly ImageClient _imageClient;
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelDisplayName;
    private readonly string _deploymentName;
    private readonly bool _ownsHttpClient;
    private const int MaxPromptLength = 4000;
    
    /// <summary>
    /// Gets the model display name (e.g., "GPT-Image-2").
    /// </summary>
    public string ModelName => _modelDisplayName;
    
    /// <summary>
    /// Gets the Azure OpenAI deployment name.
    /// </summary>
    public string DeploymentName => _deploymentName;
    
    /// <summary>
    /// Gets the Azure OpenAI endpoint URL.
    /// </summary>
    public string Endpoint => _endpoint;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="GptImage2Generator"/> class.
    /// </summary>
    /// <param name="endpoint">The Azure OpenAI Service endpoint URL (must be HTTPS). Use the bare resource URL; any /openai or /openai/v1 suffix is automatically removed.</param>
    /// <param name="apiKey">The Azure OpenAI Service API key.</param>
    /// <param name="modelName">Optional model display name. Defaults to "GPT-Image-2".</param>
    /// <param name="deploymentName">Optional deployment name. Defaults to "gpt-image-2".</param>
    /// <param name="httpClient">HttpClient instance for making HTTP requests. Use IHttpClientFactory for production to enable connection pooling.</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds for API requests. If not specified, uses HttpClient default (typically 100 seconds).</param>
    /// <exception cref="ArgumentException">Thrown when endpoint is null/empty or doesn't use HTTPS, or when apiKey is null/empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null.</exception>
    public GptImage2Generator(string endpoint, string apiKey, HttpClient httpClient, string? modelName = null, string? deploymentName = null, int? timeoutSeconds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentNullException.ThrowIfNull(httpClient);
        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("API endpoint must use HTTPS protocol", nameof(endpoint));
        _endpoint = NormalizeAzureOpenAiEndpoint(endpoint);
        _apiKey = apiKey;
        _modelDisplayName = modelName ?? "GPT-Image-2";
        _deploymentName = deploymentName ?? "gpt-image-2";
        _httpClient = httpClient;
        _ownsHttpClient = false;
        
        if (timeoutSeconds.HasValue)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
        }
        
        var client = new AzureOpenAIClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
        _imageClient = client.GetImageClient(_deploymentName);
    }

    private static string NormalizeAzureOpenAiEndpoint(string endpoint)
    {
        var normalizedEndpoint = endpoint.TrimEnd('/');
        var uri = new Uri(normalizedEndpoint);
        var path = uri.AbsolutePath;

        if (path.Equals("/openai", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openai/", StringComparison.OrdinalIgnoreCase))
        {
            return $"{uri.Scheme}://{uri.Authority}";
        }

        return normalizedEndpoint;
    }
    /// <summary>
    /// Ensures the cloud model is available. For cloud-based models, this is a no-op and returns a completed task.
    /// </summary>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    public Task EnsureModelAvailableAsync(IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new DownloadProgress { Stage = DownloadStage.Complete, PercentComplete = 100, Message = "Cloud model" });
        return Task.CompletedTask;
    }
    private static string MapToSizeString(int width, int height)
    {
        if (width == 1024 && height == 1024) return "1024x1024";
        if (width == 1024 && height == 1536) return "1024x1536";
        if (width == 1536 && height == 1024) return "1536x1024";
        double aspectRatio = (double)width / height;
        if (aspectRatio > 1.2) return "1536x1024";
        if (aspectRatio < 0.85) return "1024x1536";
        return "1024x1024";
    }
    /// <summary>
    /// Generates an image based on the provided prompt and options.
    /// </summary>
    /// <param name="prompt">The text prompt for image generation. Maximum 4000 characters.</param>
    /// <param name="options">Optional generation options (width, height, seed).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ImageGenerationResult"/> containing the generated image and metadata.</returns>
    /// <exception cref="ArgumentException">Thrown when prompt is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when prompt exceeds 4000 characters.</exception>
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
        
        var generationOptions = new OpenAI.Images.ImageGenerationOptions
        {
            Size = GeneratedImageSize.W1024xH1024
        };
        
        var sw = Stopwatch.StartNew();
        var response = await _imageClient.GenerateImageAsync(prompt, generationOptions, cancellationToken).ConfigureAwait(false);
        sw.Stop();
        
        byte[] imageBytes = response.Value.ImageBytes.ToArray();
        
        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            ModelName = _modelDisplayName,
            Prompt = prompt,
            Seed = options.Seed ?? 0,
            Width = mappedWidth,
            Height = mappedHeight,
            InferenceTimeMs = sw.ElapsedMilliseconds
        };
    }
    private static (int width, int height) ParseSizeString(string size) => size switch { "1024x1024" => (1024, 1024), "1024x1536" => (1024, 1536), "1536x1024" => (1536, 1024), _ => (1024, 1024) };
    async Task<ImageGenerationResponse> Microsoft.Extensions.AI.IImageGenerator.GenerateAsync(ImageGenerationRequest imageRequest, Microsoft.Extensions.AI.ImageGenerationOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageRequest);
        var localOptions = new ImageGenerationOptions();
        if (options?.ImageSize.HasValue == true) { localOptions.Width = options.ImageSize.Value.Width; localOptions.Height = options.ImageSize.Value.Height; }
        var result = await GenerateAsync(imageRequest.Prompt ?? "", localOptions, cancellationToken).ConfigureAwait(false);
        return ImageGenerationOptionsConverter.ToMeaiResponse(result);
    }
    object? Microsoft.Extensions.AI.IImageGenerator.GetService(Type serviceType, object? serviceKey) => serviceType == GetType() ? this : null;
    /// <summary>
    /// Releases all resources used by the generator.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient?.Dispose();
    }
}
