using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElBruno.Text2Image;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image.Foundry;

/// <summary>
/// FLUX.2 text-to-image generator using the Microsoft Foundry BFL Native API.
/// Supports both FLUX.2 [pro] (photorealistic) and FLUX.2 [flex] (text-heavy design).
/// This is a cloud API model — no local ONNX models are needed.
/// Uses the Black Forest Labs provider endpoint at .services.ai.azure.com.
/// Handles both synchronous (200) and asynchronous (202 + polling) API patterns.
/// </summary>
public sealed class Flux2Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _modelDisplayName;
    private readonly string _modelId;
    private readonly bool _ownsHttpClient;

    private const int MaxErrorBodyLength = 1024;
    private const int MaxPollAttempts = 120;
    private const long MaxResponseSizeBytes = 50 * 1024 * 1024; // 50MB limit for image responses
    private static readonly TimeSpan InitialPollDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan MaxPollDelay = TimeSpan.FromSeconds(5);
    private const double PollBackoffMultiplier = 1.5;

    /// <inheritdoc />
    public string ModelName => _modelDisplayName;

    /// <summary>
    /// The model/deployment name sent in the API request body (e.g., "FLUX.2-pro", "FLUX.2-flex").
    /// </summary>
    public string ModelId => _modelId;

    /// <summary>
    /// The resolved API endpoint URL (may differ from the input if a base URL was auto-expanded).
    /// </summary>
    public string Endpoint => _endpoint;

    /// <summary>
    /// Creates a new FLUX.2 generator targeting a Microsoft Foundry deployment.
    /// </summary>
    /// <param name="endpoint">
    /// The endpoint URL. Can be either:
    /// <list type="bullet">
    /// <item><description>A .services.ai.azure.com base URL (e.g., "https://myresource.services.ai.azure.com") — BFL API path appended automatically.</description></item>
    /// <item><description>A .openai.azure.com base URL (e.g., "https://myresource.openai.azure.com") — auto-converted to .services.ai.azure.com.</description></item>
    /// <item><description>A full BFL API URL (e.g., "https://myresource.services.ai.azure.com/providers/blackforestlabs/v1/flux-2-pro?api-version=preview") — used as-is.</description></item>
    /// </list>
    /// </param>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="modelName">Display name for the model (for logging/UI). Defaults to "FLUX.2-pro".</param>
    /// <param name="modelId">
    /// The model/deployment name sent in the API request body (e.g., "FLUX.2-pro", "FLUX.2-flex").
    /// This matches the deployment name you created in Microsoft Foundry. Defaults to "FLUX.2-pro".
    /// </param>
    /// <param name="httpClient">HttpClient instance for making HTTP requests. Use IHttpClientFactory for production to enable connection pooling.</param>
    public Flux2Generator(
        string endpoint,
        string apiKey,
        HttpClient httpClient,
        string? modelName = null,
        string? modelId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentNullException.ThrowIfNull(httpClient);

        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("API endpoint must use HTTPS protocol", nameof(endpoint));

        _endpoint = BuildEndpointUrl(endpoint, modelId ?? "FLUX.2-pro");
        _apiKey = apiKey;
        _modelDisplayName = modelName ?? "FLUX.2-pro";
        _modelId = modelId ?? "FLUX.2-pro";
        _httpClient = httpClient;
        _ownsHttpClient = false;
    }

    /// <summary>
    /// Builds the full API endpoint URL for the BFL Native API.
    /// FLUX.2 models use the Black Forest Labs provider path, not the OpenAI-compatible API.
    /// </summary>
    private static string BuildEndpointUrl(string endpoint, string modelId)
    {
        endpoint = endpoint.TrimEnd('/');
        var uri = new Uri(endpoint);

        // If the URL already contains the BFL provider path, use as-is
        if (uri.AbsolutePath.Contains("/providers/blackforestlabs/", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        // Map model ID to BFL API path segment
        var bflModelPath = MapModelToBflPath(modelId);

        // If base URL (path is empty or just "/"), build the full BFL path
        if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
        {
            // Auto-convert .openai.azure.com to .services.ai.azure.com
            var baseUrl = ConvertToServicesEndpoint(endpoint);
            return $"{baseUrl}/providers/blackforestlabs/v1/{bflModelPath}?api-version=preview";
        }

        // If the path contains /openai/, this is the wrong endpoint type for FLUX.2
        // Auto-convert to .services.ai.azure.com
        if (uri.AbsolutePath.Contains("/openai/", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            baseUrl = ConvertToServicesEndpoint(baseUrl);
            return $"{baseUrl}/providers/blackforestlabs/v1/{bflModelPath}?api-version=preview";
        }

        // Otherwise use as-is (user provided a complete custom URL)
        return endpoint;
    }

    /// <summary>
    /// Converts an .openai.azure.com hostname to .services.ai.azure.com,
    /// which is required for the BFL Native API used by FLUX.2 models.
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
    /// Maps a FLUX model ID (e.g., "FLUX.2-pro") to the BFL API path segment.
    /// </summary>
    private static string MapModelToBflPath(string modelId)
    {
        return modelId.ToUpperInvariant() switch
        {
            "FLUX.2-PRO" => "flux-2-pro",
            "FLUX.2-FLEX" => "flux-2-flex",
            "FLUX-1.1-PRO" or "FLUX.1-PRO" => "flux-pro-1.1",
            "FLUX.1-KONTEXT-PRO" => "flux-1-kontext-pro",
            _ => modelId.ToLowerInvariant().Replace(".", "-")
        };
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
            // Debug mode: include full endpoint details for troubleshooting
            return "\n\nHint: The endpoint URL may be incorrect. FLUX.2 models use the BFL Native API, not the OpenAI-compatible API.\n" +
                   $"The resolved endpoint was: {_endpoint}\n" +
                   "Ensure you provide either:\n" +
                   "  - A base URL (e.g., https://your-resource.services.ai.azure.com)\n" +
                   "  - A .openai.azure.com URL (auto-converted to .services.ai.azure.com)\n" +
                   "  - A full BFL API URL (e.g., https://your-resource.services.ai.azure.com/providers/blackforestlabs/v1/flux-2-pro?api-version=preview)";
        }

        // Production mode: generic error without exposing infrastructure details
        return "\n\nHint: Failed to connect to FLUX.2 API. " +
               "FLUX.2 models use the BFL Native API endpoint, not the OpenAI-compatible API. " +
               "Verify your endpoint configuration. " +
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

    /// <inheritdoc />
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));
        if (prompt.Length > 1000)
            throw new ArgumentOutOfRangeException(nameof(prompt), "Prompt must be 1000 characters or fewer");

        options ??= new ImageGenerationOptions();

        var sw = Stopwatch.StartNew();
        var seed = options.Seed ?? Random.Shared.Next();

        var requestBody = new Flux2Request
        {
            Prompt = prompt,
            Model = _modelId,
            N = 1,
            Width = options.Width,
            Height = options.Height,
            OutputFormat = "png",
            ReferenceImages = options.ReferenceImages
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
        request.Headers.TryAddWithoutValidation("api-key", _apiKey);

        // Serialize to bytes so Content-Length is set explicitly.
        // The BFL API rejects requests without a Content-Length header.
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(requestBody, Flux2JsonContext.Default.Flux2Request);
        request.Content = new ByteArrayContent(jsonBytes);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        // Validate response size before reading content
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
                $"FLUX.2 API returned {response.StatusCode}: {errorBody}{hint}");
        }

        // Read the response body once
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        // Handle async API pattern:
        // - 202 Accepted with operation-location header
        // - 200 OK with empty body and operation-location header
        // - 200 OK with operation status JSON (id + status fields)
        var hasOperationLocation = response.Headers.Contains("operation-location")
            || response.Headers.Location != null;
        var bodyIsEmpty = string.IsNullOrWhiteSpace(responseBody);

        Flux2Response? result;
        if (response.StatusCode == HttpStatusCode.Accepted || (hasOperationLocation && bodyIsEmpty))
        {
            result = await PollForResultAsync(response, cancellationToken).ConfigureAwait(false);
        }
        else if (!bodyIsEmpty)
        {
            // Try to detect if this is an async operation status response
            var maybeOperation = JsonSerializer.Deserialize(responseBody, Flux2JsonContext.Default.Flux2AsyncOperation);
            if (maybeOperation?.Status != null && maybeOperation.Status.ToLowerInvariant() != "succeeded")
            {
                // This is an async operation — need to poll
                result = await PollForResultAsync(response, cancellationToken).ConfigureAwait(false);
            }
            else if (maybeOperation?.Result?.Data?.Count > 0)
            {
                result = maybeOperation.Result;
            }
            else
            {
                result = JsonSerializer.Deserialize(responseBody, Flux2JsonContext.Default.Flux2Response)
                    ?? throw new InvalidOperationException(
                        $"Failed to parse FLUX.2 API response (status {response.StatusCode}). Body: {responseBody[..Math.Min(responseBody.Length, 200)]}");
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"FLUX.2 API returned {response.StatusCode} with empty body and no operation-location header");
        }

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
            var imageResponse = await _httpClient.SendAsync(imageRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            
            // Validate image response size before downloading
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
    /// Generates an image using a text prompt and a reference image file.
    /// The file is read, converted to a base64 Data URI, and passed as a reference image.
    /// </summary>
    /// <param name="prompt">The text description of the image to generate.</param>
    /// <param name="referenceImagePath">Path to a reference image file (PNG, JPEG, or WebP).</param>
    /// <param name="options">Optional generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generation result containing the image data.</returns>
    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        string referenceImagePath,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceImagePath, nameof(referenceImagePath));
        if (!File.Exists(referenceImagePath))
            throw new FileNotFoundException("Reference image file not found.", referenceImagePath);

        var imageBytes = await File.ReadAllBytesAsync(referenceImagePath, cancellationToken).ConfigureAwait(false);
        var mimeType = Path.GetExtension(referenceImagePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";

        options ??= new ImageGenerationOptions();
        options.ReferenceImages ??= [];
        options.ReferenceImages.Add(dataUri);

        return await GenerateAsync(prompt, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Polls the operation-location URL until the async operation completes.
    /// </summary>
    private async Task<Flux2Response> PollForResultAsync(
        HttpResponseMessage submitResponse,
        CancellationToken cancellationToken)
    {
        // Get the polling URL from operation-location or location header
        var operationUrl = submitResponse.Headers.GetValues("operation-location").FirstOrDefault()
            ?? submitResponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException(
                "FLUX.2 API returned 202 Accepted but no operation-location or Location header for polling");

        var currentDelay = InitialPollDelay;
        
        for (var attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            await Task.Delay(currentDelay, cancellationToken).ConfigureAwait(false);

            using var pollRequest = new HttpRequestMessage(HttpMethod.Get, operationUrl);
            pollRequest.Headers.TryAddWithoutValidation("api-key", _apiKey);

            var pollResponse = await _httpClient.SendAsync(pollRequest, cancellationToken).ConfigureAwait(false);

            if (!pollResponse.IsSuccessStatusCode)
            {
                var errorBody = await pollResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (errorBody.Length > MaxErrorBodyLength)
                    errorBody = errorBody[..MaxErrorBodyLength] + "... (truncated)";
                throw new HttpRequestException(
                    $"FLUX.2 polling returned {pollResponse.StatusCode}: {errorBody}");
            }

            var pollBody = await pollResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(pollBody))
                continue;

            // Parse the async operation status
            var operation = JsonSerializer.Deserialize(pollBody, Flux2JsonContext.Default.Flux2AsyncOperation);

            if (operation is null)
                continue;

            var status = operation.Status?.ToLowerInvariant();

            if (status == "failed" || status == "canceled" || status == "cancelled")
            {
                var errorMsg = operation.Error?.Message ?? "Unknown error";
                throw new InvalidOperationException(
                    $"FLUX.2 async operation {status}: {errorMsg}");
            }

            if (status == "succeeded" || status == "complete" || status == "completed")
            {
                // Result may be embedded in the operation response or in a nested "result" property
                if (operation.Result?.Data?.Count > 0)
                    return operation.Result;

                // Try parsing the entire body as a Flux2Response (some API versions embed data at top level)
                var directResult = JsonSerializer.Deserialize(pollBody, Flux2JsonContext.Default.Flux2Response);
                if (directResult?.Data?.Count > 0)
                    return directResult;

                throw new InvalidOperationException(
                    "FLUX.2 operation succeeded but no image data found in response");
            }

            // Still running (status: "running", "notStarted", "inProgress", etc.) — keep polling
            // Apply exponential backoff for next attempt
            currentDelay = TimeSpan.FromMilliseconds(
                Math.Min(currentDelay.TotalMilliseconds * PollBackoffMultiplier, MaxPollDelay.TotalMilliseconds));
        }

        // Calculate approximate total wait time (sum of geometric series)
        var totalWaitSeconds = InitialPollDelay.TotalSeconds * 
            (1 - Math.Pow(PollBackoffMultiplier, MaxPollAttempts)) / 
            (1 - PollBackoffMultiplier);
        throw new TimeoutException(
            $"FLUX.2 async operation did not complete within approximately {totalWaitSeconds:F0} seconds ({MaxPollAttempts} poll attempts)");
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

        if (options?.AdditionalProperties?.TryGetValue(Text2ImagePropertyNames.ReferenceImages, out var refImages) == true
            && refImages is List<string> refList)
        {
            localOptions.ReferenceImages = refList;
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

internal sealed class Flux2Request
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; set; }

    [JsonPropertyName("model")]
    public required string Model { get; set; }

    [JsonPropertyName("n")]
    public int N { get; set; } = 1;

    [JsonPropertyName("width")]
    public int Width { get; set; } = 1024;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 1024;

    [JsonPropertyName("output_format")]
    public string OutputFormat { get; set; } = "png";

    [JsonPropertyName("referenceImages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ReferenceImages { get; set; }
}

internal sealed class Flux2Response
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<Flux2ImageData>? Data { get; set; }
}

/// <summary>
/// Represents the status of an async image generation operation (202 polling pattern).
/// </summary>
internal sealed class Flux2AsyncOperation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("result")]
    public Flux2Response? Result { get; set; }

    [JsonPropertyName("error")]
    public Flux2OperationError? Error { get; set; }
}

internal sealed class Flux2OperationError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
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
[JsonSerializable(typeof(Flux2AsyncOperation))]
internal sealed partial class Flux2JsonContext : JsonSerializerContext
{
}
