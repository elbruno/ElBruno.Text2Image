using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElBruno.Text2Image;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Foundry;

/// <summary>
/// FLUX.2 text-to-image generator using the Microsoft Foundry REST API.
/// Supports both FLUX.2 [pro] (photorealistic) and FLUX.2 [flex] (text-heavy design).
/// This is a cloud API model — no local ONNX models are needed.
/// </summary>
public sealed class Flux2Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelDisplayName;
    private readonly string? _modelId;
    private readonly bool _ownsHttpClient;

    private const int MaxErrorBodyLength = 1024;

    /// <inheritdoc />
    public string ModelName => _modelDisplayName;

    /// <summary>
    /// The model identifier sent in the API request body (e.g., "FLUX.2-pro", "FLUX.2-flex").
    /// Null if the model is determined by the endpoint URL (deployment-based routing).
    /// </summary>
    public string? ModelId => _modelId;

    /// <summary>
    /// Creates a new FLUX.2 generator targeting a Microsoft Foundry deployment.
    /// </summary>
    /// <param name="endpoint">The full Microsoft Foundry endpoint URL for the FLUX.2 deployment.</param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="modelName">Display name for the model. Defaults to "FLUX.2".</param>
    /// <param name="modelId">
    /// Optional model identifier to include in the API request body (e.g., "FLUX.2-pro", "FLUX.2-flex").
    /// Required for model-based endpoints. Not needed for deployment-based endpoints where the model is
    /// embedded in the URL path.
    /// </param>
    /// <param name="httpClient">Optional HttpClient instance. The API key is sent per-request, not added to DefaultRequestHeaders.</param>
    public Flux2Generator(string endpoint, string apiKey, string? modelName = null, string? modelId = null, HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        _endpoint = endpoint.TrimEnd('/');
        _apiKey = apiKey;
        _modelDisplayName = modelName ?? "FLUX.2";
        _modelId = modelId;

        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
    }

    /// <summary>
    /// No-op for cloud models. The model is always available on the server.
    /// </summary>
    public Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new DownloadProgress
        {
            Stage = DownloadStage.Complete,
            PercentComplete = 100,
            Message = "Cloud model — no download required"
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        options ??= new ImageGenerationOptions();

        var sw = Stopwatch.StartNew();
        var seed = options.Seed ?? Random.Shared.Next();

        var requestBody = new Flux2Request
        {
            Prompt = prompt,
            Model = _modelId,
            N = 1,
            Size = $"{options.Width}x{options.Height}",
            ResponseFormat = "b64_json"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);
        request.Content = JsonContent.Create(requestBody, Flux2JsonContext.Default.Flux2Request);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (errorBody.Length > MaxErrorBodyLength)
                errorBody = errorBody[..MaxErrorBodyLength] + "... (truncated)";
            throw new HttpRequestException(
                $"FLUX.2 API returned {response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync(Flux2JsonContext.Default.Flux2Response, cancellationToken)
            ?? throw new InvalidOperationException("Failed to parse FLUX.2 API response");

        byte[] imageBytes;
        var imageData = result.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException("FLUX.2 API returned no image data");

        if (!string.IsNullOrEmpty(imageData.B64Json))
        {
            imageBytes = Convert.FromBase64String(imageData.B64Json);
        }
        else if (!string.IsNullOrEmpty(imageData.Url))
        {
            // Use a separate request WITHOUT the API key to avoid credential leakage (SSRF mitigation)
            using var imageRequest = new HttpRequestMessage(HttpMethod.Get, imageData.Url);
            var imageResponse = await _httpClient.SendAsync(imageRequest, cancellationToken);
            imageResponse.EnsureSuccessStatusCode();
            imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("FLUX.2 API response contains neither base64 data nor URL");
        }

        sw.Stop();

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            ModelName = _modelDisplayName,
            Prompt = prompt,
            Seed = seed,
            InferenceTimeMs = sw.ElapsedMilliseconds,
            Width = options.Width,
            Height = options.Height
        };
    }

    /// <summary>
    /// Generates an image using the Microsoft.Extensions.AI interface.
    /// </summary>
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
        var result = await GenerateAsync(imageRequest.Prompt ?? "", localOptions, cancellationToken);
        return ImageGenerationOptionsConverter.ToMeaiResponse(result);
    }

    /// <inheritdoc />
    object? Microsoft.Extensions.AI.IImageGenerator.GetService(Type serviceType, object? serviceKey)
        => serviceType == GetType() ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}

// --- JSON serialization types ---

internal sealed class Flux2Request
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("model")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Model { get; set; }

    [JsonPropertyName("n")]
    public int N { get; set; } = 1;

    [JsonPropertyName("size")]
    public string Size { get; set; } = "1024x1024";

    [JsonPropertyName("response_format")]
    public string ResponseFormat { get; set; } = "b64_json";
}

internal sealed class Flux2Response
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<Flux2ImageData>? Data { get; set; }
}

internal sealed class Flux2ImageData
{
    [JsonPropertyName("b64_json")]
    public string? B64Json { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("revised_prompt")]
    public string? RevisedPrompt { get; set; }
}

[JsonSerializable(typeof(Flux2Request))]
[JsonSerializable(typeof(Flux2Response))]
internal sealed partial class Flux2JsonContext : JsonSerializerContext
{
}
