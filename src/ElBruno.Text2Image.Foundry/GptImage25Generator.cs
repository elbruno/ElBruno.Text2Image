using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;
using System.ClientModel.Primitives;
using OpenAI.Images;
using ElBruno.Text2Image;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Foundry;

/// <summary>
/// Parameterized Azure OpenAI GPT-Image-2.5 generator.
/// Use <c>GPT-Image-2.5-Sunburst</c> or <c>GPT-Image-2.5-Flare</c> as the
/// model/deployment name. Both variants share the GPT image generation contract.
/// </summary>
public sealed class GptImage25Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private const int MaxPromptLength = 4000;
    private readonly ImageClient _imageClient;
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelDisplayName;
    private readonly string _deploymentName;
    private readonly bool _ownsHttpClient;

    /// <summary>Gets the display name of the configured GPT image model.</summary>
    public string ModelName => _modelDisplayName;
    /// <summary>Gets the Azure OpenAI deployment name.</summary>
    public string DeploymentName => _deploymentName;
    /// <summary>Gets the normalized Azure OpenAI endpoint.</summary>
    public string Endpoint => _endpoint;

    /// <summary>Creates a parameterized GPT-Image-2.5 generator.</summary>
    public GptImage25Generator(
        string endpoint,
        string apiKey,
        HttpClient httpClient,
        string? modelName = null,
        string? deploymentName = null,
        int? timeoutSeconds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentNullException.ThrowIfNull(httpClient);

        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("API endpoint must use HTTPS protocol", nameof(endpoint));

        _endpoint = NormalizeAzureOpenAiEndpoint(endpoint);
        _modelDisplayName = modelName ?? "GPT-Image-2.5-Sunburst";
        _deploymentName = deploymentName ?? "gpt-image-2.5-sunburst";
        _httpClient = httpClient;
        _ownsHttpClient = false;

        if (timeoutSeconds.HasValue)
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);

        var client = new AzureOpenAIClient(
            new Uri(_endpoint),
            new AzureKeyCredential(apiKey),
            new AzureOpenAIClientOptions
            {
                Transport = new HttpClientPipelineTransport(httpClient)
            });
        _imageClient = client.GetImageClient(_deploymentName);
    }

    private static string NormalizeAzureOpenAiEndpoint(string endpoint)
    {
        var normalized = endpoint.TrimEnd('/');
        var uri = new Uri(normalized);
        if (uri.AbsolutePath.Equals("/openai", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.StartsWith("/openai/", StringComparison.OrdinalIgnoreCase))
        {
            return $"{uri.Scheme}://{uri.Authority}";
        }

        return normalized;
    }

    /// <summary>Reports that the cloud model requires no local download.</summary>
    public Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new DownloadProgress
        {
            Stage = DownloadStage.Complete,
            PercentComplete = 100,
            Message = "Cloud model"
        });
        return Task.CompletedTask;
    }

    /// <summary>Generates an image from a prompt.</summary>
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));
        if (prompt.Length > MaxPromptLength)
            throw new ArgumentOutOfRangeException(nameof(prompt), $"Prompt must be {MaxPromptLength} characters or fewer");

        options ??= new ImageGenerationOptions();
        var mapped = GptImageSupport.MapSize(options.Width, options.Height);
        var generationOptions = new OpenAI.Images.ImageGenerationOptions { Size = mapped.Size };

        var stopwatch = Stopwatch.StartNew();
        var response = await _imageClient.GenerateImageAsync(prompt, generationOptions, cancellationToken)
            .ConfigureAwait(false);
        stopwatch.Stop();

        return new ImageGenerationResult
        {
            ImageBytes = response.Value.ImageBytes.ToArray(),
            ModelName = _modelDisplayName,
            Prompt = prompt,
            Seed = options.Seed ?? 0,
            Width = mapped.Width,
            Height = mapped.Height,
            InferenceTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    async Task<ImageGenerationResponse> Microsoft.Extensions.AI.IImageGenerator.GenerateAsync(
        ImageGenerationRequest imageRequest,
        Microsoft.Extensions.AI.ImageGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageRequest);
        var localOptions = new ImageGenerationOptions();
        if (options?.ImageSize is { } size)
        {
            localOptions.Width = size.Width;
            localOptions.Height = size.Height;
        }

        var result = await GenerateAsync(imageRequest.Prompt ?? string.Empty, localOptions, cancellationToken)
            .ConfigureAwait(false);
        return ImageGenerationOptionsConverter.ToMeaiResponse(result);
    }

    object? Microsoft.Extensions.AI.IImageGenerator.GetService(Type serviceType, object? serviceKey)
        => serviceType == GetType() ? this : null;

    /// <summary>Releases owned resources.</summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
