using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElBruno.Text2Image.Models;

/// <summary>
/// FLUX.2 text-to-image generator using the Microsoft Azure AI Foundry REST API.
/// Supports both FLUX.2 [pro] (photorealistic) and FLUX.2 [flex] (text-heavy design).
/// This is a cloud API model — no local ONNX models are needed.
/// </summary>
/// <remarks>
/// Requires an Azure AI Foundry deployment of a FLUX.2 model.
/// See https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/meet-flux-2-flex-for-text-heavy-design
/// </remarks>
public sealed class Flux2Generator : IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _modelDisplayName;
    private readonly bool _ownsHttpClient;

    /// <inheritdoc />
    public string ModelName => _modelDisplayName;

    /// <summary>
    /// Creates a new FLUX.2 generator targeting a Microsoft Azure AI Foundry deployment.
    /// </summary>
    /// <param name="endpoint">
    /// The full Azure AI Foundry endpoint URL for the FLUX.2 deployment.
    /// Example: "https://myresource.services.ai.azure.com/openai/deployments/flux-2-pro/images/generations?api-version=2024-06-01"
    /// Or a simpler endpoint like: "https://myresource.services.ai.azure.com/api/models/flux-2-pro:generate"
    /// </param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="modelName">Display name for the model. Defaults to "FLUX.2".</param>
    /// <param name="httpClient">Optional HttpClient instance for custom configuration.</param>
    public Flux2Generator(string endpoint, string apiKey, string? modelName = null, HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        _endpoint = endpoint.TrimEnd('/');
        _modelDisplayName = modelName ?? "FLUX.2";

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

        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("api-key", apiKey);
    }

    /// <summary>
    /// No-op for cloud models. The model is always available on the server.
    /// Optionally performs a connectivity check.
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
            N = 1,
            Size = $"{options.Width}x{options.Height}",
            ResponseFormat = "b64_json"
        };

        var response = await _httpClient.PostAsJsonAsync(_endpoint, requestBody, Flux2JsonContext.Default.Flux2Request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
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
            imageBytes = await _httpClient.GetByteArrayAsync(imageData.Url, cancellationToken);
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
