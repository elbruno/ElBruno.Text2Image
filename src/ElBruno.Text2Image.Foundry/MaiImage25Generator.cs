using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElBruno.Text2Image;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Foundry;

/// <summary>
/// MAI-Image-2.5 / MAI-Image-2.5-Flash text-to-image generator using the Microsoft Foundry
/// MAI image generations API (<c>/mai/v1/images/generations</c>).
/// This is a cloud API model — no local ONNX models are needed.
/// A single instance targets one model variant, selected via <see cref="ModelId"/>
/// (e.g. <c>MAI-Image-2.5</c> or <c>MAI-Image-2.5-Flash</c>).
/// The model responds synchronously (no 202 polling required).
/// </summary>
public sealed class MaiImage25Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelDisplayName;
    private readonly string _modelId;
    private readonly bool _ownsHttpClient;

    private const int MaxErrorBodyLength = 1024;
    private const int MaxPromptLength = 32_000;
    private const long MaxResponseSizeBytes = 50 * 1024 * 1024; // 50MB limit for image responses

    /// <inheritdoc />
    public string ModelName => _modelDisplayName;

    /// <summary>
    /// The model name sent in the API request body (e.g., "MAI-Image-2.5", "MAI-Image-2.5-Flash").
    /// </summary>
    public string ModelId => _modelId;

    /// <summary>
    /// The resolved API endpoint URL (may differ from the input if a base URL was auto-expanded).
    /// </summary>
    public string Endpoint => _endpoint;

    /// <summary>
    /// Creates a new MAI-Image-2.5 generator targeting a Microsoft Foundry deployment.
    /// </summary>
    /// <param name="endpoint">
    /// The endpoint URL. Can be either:
    /// <list type="bullet">
    /// <item><description>A .services.ai.azure.com base URL (e.g., "https://myresource.services.ai.azure.com") — MAI image generations path appended automatically.</description></item>
    /// <item><description>A .openai.azure.com base URL (e.g., "https://myresource.openai.azure.com") — auto-converted to .services.ai.azure.com.</description></item>
    /// <item><description>A full MAI image generations URL (e.g., "https://myresource.services.ai.azure.com/mai/v1/images/generations") — used as-is.</description></item>
    /// </list>
    /// </param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="httpClient">HttpClient instance for making HTTP requests. Use IHttpClientFactory for production to enable connection pooling.</param>
    /// <param name="modelName">Display name for the model (for logging/UI). Defaults to "MAI-Image-2.5".</param>
    /// <param name="modelId">
    /// The model name sent in the API request body. Defaults to "MAI-Image-2.5".
    /// Use "MAI-Image-2.5-Flash" for the speed-optimized variant.
    /// </param>
    /// <param name="timeoutSeconds">Optional timeout in seconds for API requests. If not specified, uses HttpClient default (typically 100 seconds).</param>
    public MaiImage25Generator(
        string endpoint,
        string apiKey,
        HttpClient httpClient,
        string? modelName = null,
        string? modelId = null,
        int? timeoutSeconds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(httpClient);

        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("API endpoint must use HTTPS protocol", nameof(endpoint));

        _endpoint = BuildEndpointUrl(endpoint);
        _apiKey = apiKey;
        _modelDisplayName = modelName ?? "MAI-Image-2.5";
        _modelId = modelId ?? "MAI-Image-2.5";
        _httpClient = httpClient;
        _ownsHttpClient = false;

        if (timeoutSeconds.HasValue)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds.Value);
        }
    }

    /// <summary>
    /// Builds the full API endpoint URL for the MAI image generations API.
    /// </summary>
    private static string BuildEndpointUrl(string endpoint)
    {
        endpoint = endpoint.TrimEnd('/');
        var uri = new Uri(endpoint);

        // If the URL already contains the MAI image generations path, use as-is.
        if (uri.AbsolutePath.Contains("/mai/v1/images/generations", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        // If base URL (path is empty or just "/"), build the full MAI image generations path.
        if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
        {
            var baseUrl = ConvertToServicesEndpoint(endpoint);
            return $"{baseUrl}/mai/v1/images/generations";
        }

        // If the path is a partial /mai or legacy /openai prefix,
        // strip back to the host and rebuild the full path.
        if (uri.AbsolutePath.Equals("/mai", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.StartsWith("/mai/", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.Equals("/openai", StringComparison.OrdinalIgnoreCase) ||
            uri.AbsolutePath.StartsWith("/openai/", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            baseUrl = ConvertToServicesEndpoint(baseUrl);
            return $"{baseUrl}/mai/v1/images/generations";
        }

        // Otherwise use as-is (user provided a complete custom URL).
        return endpoint;
    }

    /// <summary>
    /// Converts an .openai.azure.com hostname to .services.ai.azure.com,
    /// which hosts the MAI API path used by MAI-Image-2.5.
    /// </summary>
    private static string ConvertToServicesEndpoint(string endpoint)
    {
        if (endpoint.Contains(".openai.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint.Replace(".openai.azure.com", ".services.ai.azure.com", StringComparison.OrdinalIgnoreCase);
        }
        return endpoint;
    }

    /// <summary>
    /// Builds error hint for 404 responses.
    /// In production (default), provides generic guidance.
    /// Set T2I_DETAILED_ERRORS=1 environment variable to include full endpoint URL (for debugging only).
    /// </summary>
    private string BuildErrorHint()
    {
        var detailedErrors = Environment.GetEnvironmentVariable("T2I_DETAILED_ERRORS");
        var includeEndpoint = detailedErrors == "1" || detailedErrors == "true";

        if (includeEndpoint)
        {
            return "\n\nHint: The endpoint URL may be incorrect. MAI-Image-2.5 uses the MAI image generations API at /mai/v1/images/generations.\n" +
                   $"The resolved endpoint was: {_endpoint}\n" +
                   "Ensure you provide either:\n" +
                   "  - A base URL (e.g., https://your-resource.services.ai.azure.com)\n" +
                   "  - A .openai.azure.com URL (auto-converted to .services.ai.azure.com)\n" +
                   "  - A full MAI image generations URL (e.g., https://your-resource.services.ai.azure.com/mai/v1/images/generations)";
        }

        return "\n\nHint: Failed to connect to image generation service. " +
               "Verify your endpoint configuration is correct. " +
               "Set T2I_DETAILED_ERRORS=1 for more diagnostic information.";
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

    /// <summary>
    /// Maps requests to the MAI-Image-2.5 maximum supported size (1024×1024).
    /// </summary>
    private static string MapToSizeString(int width, int height)
    {
        return "1024x1024";
    }

    private static (int width, int height) ParseSizeString(string size) => size switch
    {
        "1024x1024" => (1024, 1024),
        _ => (1024, 1024)
    };

    /// <inheritdoc />
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));
        if (prompt.Length > MaxPromptLength)
            throw new ArgumentOutOfRangeException(nameof(prompt), $"Prompt must be {MaxPromptLength} characters or fewer");

        // Use MAI-Image-2.5 defaults (1024×1024) when caller provides no explicit options.
        // ImageGenerationOptions defaults to 512×512 which is for local ONNX models.
        var width = options?.Width > 0 ? options.Width : 1024;
        var height = options?.Height > 0 ? options.Height : 1024;
        options ??= new ImageGenerationOptions();

        var sizeString = MapToSizeString(width, height);
        var (mappedWidth, mappedHeight) = ParseSizeString(sizeString);

        var sw = Stopwatch.StartNew();
        var seed = options.Seed ?? Random.Shared.Next();

        var requestBody = new MaiImage25Request
        {
            Model = _modelId,
            Prompt = prompt,
            Size = sizeString,
            N = 1,
            OutputFormat = "png",
            OutputCompression = 100
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

        // Serialize to bytes so Content-Length is set explicitly.
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(requestBody, MaiImage25JsonContext.Default.MaiImage25Request);
        request.Content = new ByteArrayContent(jsonBytes);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Validate response size before reading content.
        if (response.Content.Headers.ContentLength.HasValue &&
            response.Content.Headers.ContentLength.Value > MaxResponseSizeBytes)
        {
            throw new InvalidOperationException(
                $"Response size ({response.Content.Headers.ContentLength.Value} bytes) exceeds maximum allowed ({MaxResponseSizeBytes} bytes)");
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (errorBody.Length > MaxErrorBodyLength)
                errorBody = errorBody[..MaxErrorBodyLength] + "... (truncated)";

            var hint = response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? BuildErrorHint()
                : "";

            throw new HttpRequestException(
                $"{_modelDisplayName} API returned {response.StatusCode}: {errorBody}{hint}");
        }

        // MAI-Image-2.5 responds synchronously — parse the response directly.
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var result = JsonSerializer.Deserialize(responseBody, MaiImage25JsonContext.Default.MaiImage25Response)
            ?? throw new InvalidOperationException(
                $"Failed to parse {_modelDisplayName} API response (status {response.StatusCode}). Body: {responseBody[..Math.Min(responseBody.Length, 200)]}");

        byte[] imageBytes;
        var imageData = result.Data?.FirstOrDefault()
            ?? throw new InvalidOperationException($"{_modelDisplayName} API returned no image data");

        if (!string.IsNullOrEmpty(imageData.B64Json))
        {
            imageBytes = Convert.FromBase64String(imageData.B64Json);
        }
        else if (!string.IsNullOrEmpty(imageData.Url))
        {
            // Use a separate request WITHOUT the API key to avoid credential leakage (SSRF mitigation).
            using var imageRequest = new HttpRequestMessage(HttpMethod.Get, imageData.Url);
            var imageResponse = await _httpClient.SendAsync(imageRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (imageResponse.Content.Headers.ContentLength.HasValue &&
                imageResponse.Content.Headers.ContentLength.Value > MaxResponseSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Image response size ({imageResponse.Content.Headers.ContentLength.Value} bytes) exceeds maximum allowed ({MaxResponseSizeBytes} bytes)");
            }

            imageResponse.EnsureSuccessStatusCode();
            imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException($"{_modelDisplayName} API response contains neither base64 data nor URL");
        }

        sw.Stop();

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            ModelName = _modelDisplayName,
            Prompt = prompt,
            Seed = seed,
            InferenceTimeMs = sw.ElapsedMilliseconds,
            Width = mappedWidth,
            Height = mappedHeight
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

        var result = await GenerateAsync(imageRequest.Prompt ?? "", localOptions, cancellationToken).ConfigureAwait(false);
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

internal sealed class MaiImage25Request
{
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("size")]
    public string Size { get; set; } = "1024x1024";

    [JsonPropertyName("n")]
    public int N { get; set; } = 1;

    [JsonPropertyName("output_format")]
    public string OutputFormat { get; set; } = "png";

    [JsonPropertyName("output_compression")]
    public int OutputCompression { get; set; } = 100;
}

internal sealed class MaiImage25Response
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<MaiImage25ImageData>? Data { get; set; }
}

internal sealed class MaiImage25ImageData
{
    [JsonPropertyName("b64_json")]
    public string? B64Json { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("revised_prompt")]
    public string? RevisedPrompt { get; set; }
}

[JsonSerializable(typeof(MaiImage25Request))]
[JsonSerializable(typeof(MaiImage25Response))]
internal sealed partial class MaiImage25JsonContext : JsonSerializerContext
{
}
