using System.Net;
using System.Text;
using System.Text.Json;
using ElBruno.Text2Image;
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Helper to create fake HTTP responses for GPT-Image-2 API responses.
/// </summary>
internal static class GptImage2FakeResponses
{
    public static HttpResponseMessage CreateGptImage2SuccessResponse()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var b64 = Convert.ToBase64String(pngBytes);
        var json = $$"""{"created":1234,"data":[{"b64_json":"{{b64}}","revised_prompt":"test prompt"}]}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    public static HttpResponseMessage CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        var json = $"{{\"error\":{{\"message\":\"{message}\",\"code\":\"{statusCode}\"}}}}";
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

// ============================================================
// Content-Length Tests — Verify ByteArrayContent usage
// ============================================================

public class GptImage2GeneratorContentLengthTests
{
    [Fact]
    public async Task GenerateAsync_Request_HasContentLengthHeader()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        Assert.NotNull(handler.LastRequest?.Content?.Headers.ContentLength);
        Assert.True(handler.LastRequest!.Content!.Headers.ContentLength > 0,
            "Content-Length must be a positive value (API rejects chunked encoding)");
    }

    [Fact]
    public async Task GenerateAsync_Request_ContentLengthMatchesBodySize()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var declaredLength = handler.LastRequest!.Content!.Headers.ContentLength;
        var actualBytes = Encoding.UTF8.GetBytes(handler.LastRequestBody!);
        Assert.Equal(actualBytes.Length, declaredLength);
    }

    [Fact]
    public async Task GenerateAsync_Request_BodyIsValidJson()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        Assert.NotNull(handler.LastRequestBody);
        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.NotEqual(default, doc.RootElement);
    }

    [Fact]
    public async Task GenerateAsync_Request_ContainsExpectedFields()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator(
            "https://example.services.ai.azure.com", "test-key", "gpt-image-2-deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        await generator.GenerateAsync("a beautiful sunset", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("a beautiful sunset", root.GetProperty("prompt").GetString());
        Assert.Equal("1792x1024", root.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Request_ContentTypeIsJsonUtf8()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.NotNull(handler.LastRequest?.Content?.Headers.ContentType);
        Assert.Equal("application/json", handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", handler.LastRequest.Content.Headers.ContentType.CharSet);
    }
}

// ============================================================
// HTTP Request Structure Tests
// ============================================================

public class GptImage2GeneratorHttpRequestTests
{
    [Fact]
    public async Task GenerateAsync_Request_UsesPostMethod()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
    }

    [Fact]
    public async Task GenerateAsync_Request_HasAuthorizationHeader()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "my-api-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.True(handler.LastRequest!.Headers.Contains("api-key"));
        var apiKeyValues = handler.LastRequest.Headers.GetValues("api-key").ToList();
        Assert.Contains("my-api-key", apiKeyValues);
    }

    [Fact]
    public async Task GenerateAsync_Request_IncludesPromptInBody()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("a serene mountain landscape");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("a serene mountain landscape", doc.RootElement.GetProperty("prompt").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Request_IncludesSizeInBody()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        await generator.GenerateAsync("test", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("1792x1024", doc.RootElement.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Request_DefaultSizeIs1024x1024()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("1024x1024", doc.RootElement.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Request_IncludesDeploymentNameInUrl()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator(
            "https://example.services.ai.azure.com", "test-key", "my-deployment", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.NotNull(handler.LastRequest?.RequestUri);
        Assert.Contains("my-deployment", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GenerateAsync_Request_UsesCorrectApiVersion()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.NotNull(handler.LastRequest?.RequestUri);
        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("api-version", query);
    }
}

// ============================================================
// HTTP Response Parsing Tests
// ============================================================

public class GptImage2GeneratorHttpResponseTests
{
    [Fact]
    public async Task GenerateAsync_SuccessResponse_ParsesImageBytes()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var result = await generator.GenerateAsync("test");

        Assert.NotNull(result.ImageBytes);
        Assert.NotEmpty(result.ImageBytes);
        Assert.Equal(0x89, result.ImageBytes[0]); // PNG magic
    }

    [Fact]
    public async Task GenerateAsync_SuccessResponse_SetsModelName()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var result = await generator.GenerateAsync("test");

        Assert.Equal("GPT-Image-2", result.ModelName);
    }

    [Fact]
    public async Task GenerateAsync_SuccessResponse_PreservesPrompt()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var result = await generator.GenerateAsync("original prompt text");

        Assert.Equal("original prompt text", result.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_SuccessResponse_SetsDimensions()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        var result = await generator.GenerateAsync("test", options);

        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_EmptyResponseData_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"data":[]}""", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_MalformedJson_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }
}

// ============================================================
// HTTP Error Response Tests
// ============================================================

public class GptImage2GeneratorHttpErrorTests
{
    [Fact]
    public async Task GenerateAsync_401Unauthorized_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(
            HttpStatusCode.Unauthorized, "Invalid API key"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "bad-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_400BadRequest_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(
            HttpStatusCode.BadRequest, "Invalid request format"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_404NotFound_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(
            HttpStatusCode.NotFound, "Deployment not found"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "invalid-deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_500InternalServerError_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(
            HttpStatusCode.InternalServerError, "Internal server error"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_429TooManyRequests_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(
            HttpStatusCode.TooManyRequests, "Rate limit exceeded"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_503ServiceUnavailable_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateErrorResponse(
            HttpStatusCode.ServiceUnavailable, "Service temporarily unavailable"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<Exception>(() => generator.GenerateAsync("test"));
    }
}

// ============================================================
// Size Mapping HTTP Tests
// ============================================================

public class GptImage2GeneratorHttpSizeMappingTests
{
    [Fact]
    public async Task GenerateAsync_Size1024x1024_SendsCorrectSizeString()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1024, Height = 1024 };
        await generator.GenerateAsync("test", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("1024x1024", doc.RootElement.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Size1792x1024_SendsCorrectSizeString()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        await generator.GenerateAsync("test", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("1792x1024", doc.RootElement.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Size1024x1792_SendsCorrectSizeString()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1024, Height = 1792 };
        await generator.GenerateAsync("test", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("1024x1792", doc.RootElement.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_InvalidSize_MapsToSupportedSize()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 512, Height = 512 };
        await generator.GenerateAsync("test", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var size = doc.RootElement.GetProperty("size").GetString();
        
        // Should map to a supported size
        var supportedSizes = new[] { "1024x1024", "1792x1024", "1024x1792" };
        Assert.Contains(size, supportedSizes);
    }
}

// ============================================================
// Edge Case HTTP Tests
// ============================================================

public class GptImage2GeneratorHttpEdgeCaseTests
{
    [Fact]
    public async Task GenerateAsync_VeryLongPrompt_SendsCompletePrompt()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var longPrompt = new string('a', 3999);
        await generator.GenerateAsync(longPrompt);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var sentPrompt = doc.RootElement.GetProperty("prompt").GetString();
        Assert.Equal(longPrompt.Length, sentPrompt!.Length);
    }

    [Fact]
    public async Task GenerateAsync_PromptWithUnicodeCharacters_EncodesCorrectly()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var unicodePrompt = "日本の美しい桜の木 🌸";
        await generator.GenerateAsync(unicodePrompt);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(unicodePrompt, doc.RootElement.GetProperty("prompt").GetString());
    }

    [Fact]
    public async Task GenerateAsync_PromptWithEscapedCharacters_HandlesCorrectly()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var prompt = "A scene with \"quotes\" and \\ backslashes";
        await generator.GenerateAsync(prompt);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(prompt, doc.RootElement.GetProperty("prompt").GetString());
    }

    [Fact]
    public async Task GenerateAsync_NetworkTimeout_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => throw new TimeoutException("Request timed out"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<TimeoutException>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_NetworkError_ThrowsException()
    {
        var handler = new FakeHttpHandler(_ => throw new HttpRequestException("Network error"));
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => generator.GenerateAsync("test"));
    }

    [Fact]
    public async Task GenerateAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var handler = new FakeHttpHandler(_ => throw new OperationCanceledException());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => generator.GenerateAsync("test", cancellationToken: cts.Token));
    }
}

// ============================================================
// Response Format Tests
// ============================================================

public class GptImage2GeneratorHttpResponseFormatTests
{
    [Fact]
    public async Task GenerateAsync_Request_IncludesResponseFormatB64Json()
    {
        var handler = new FakeHttpHandler(_ => GptImage2FakeResponses.CreateGptImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        await generator.GenerateAsync("test");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        if (doc.RootElement.TryGetProperty("response_format", out var format))
        {
            Assert.Equal("b64_json", format.GetString());
        }
    }

    [Fact]
    public async Task GenerateAsync_Response_ParsesBase64ImageData()
    {
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var b64 = Convert.ToBase64String(testImageBytes);
        var json = $$"""{"data":[{"b64_json":"{{b64}}"}]}""";
        
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new TestableGptImage2HttpGenerator("https://example.services.ai.azure.com", "test-key", "deploy", httpClient: httpClient);

        var result = await generator.GenerateAsync("test");

        Assert.Equal(testImageBytes, result.ImageBytes);
    }
}

// ============================================================
// Test Helper Classes
// ============================================================

/// <summary>
/// Testable GptImage2Generator that uses HttpClient for HTTP testing.
/// </summary>
internal sealed class TestableGptImage2HttpGenerator : IImageGenerator
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly HttpClient _httpClient;

    public string ModelName => "GPT-Image-2";

    public TestableGptImage2HttpGenerator(
        string endpoint,
        string apiKey,
        string deploymentName,
        HttpClient? httpClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));

        _endpoint = endpoint;
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        if (prompt.Length > 4000)
        {
            throw new ArgumentException("Prompt exceeds maximum length of 4000 characters", nameof(prompt));
        }

        var mappedOptions = MapSizeOptions(options);
        var size = $"{mappedOptions.Width}x{mappedOptions.Height}";

        var requestBody = new
        {
            prompt,
            size,
            response_format = "b64_json"
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(jsonContent));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8"
        };

        var requestUri = $"{_endpoint}/openai/deployments/{_deploymentName}/images/generations?api-version=2024-02-01";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Content = content;
        request.Headers.Add("api-key", _apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(responseJson);
        var data = doc.RootElement.GetProperty("data");
        
        if (data.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("No image data in response");
        }

        var imageData = data[0];
        var b64Json = imageData.GetProperty("b64_json").GetString()!;
        var imageBytes = Convert.FromBase64String(b64Json);

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            Prompt = prompt,
            ModelName = ModelName,
            Width = mappedOptions.Width,
            Height = mappedOptions.Height
        };
    }

    public async Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new DownloadProgress
        {
            Stage = DownloadStage.Complete,
            PercentComplete = 100,
            Message = "Cloud model — no download required"
        });
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        // HttpClient managed externally in tests
    }

    private ImageGenerationOptions MapSizeOptions(ImageGenerationOptions? options)
    {
        if (options == null)
        {
            return new ImageGenerationOptions { Width = 1024, Height = 1024 };
        }

        var width = options.Width;
        var height = options.Height;

        var supportedSizes = new[]
        {
            (1024, 1024),
            (1792, 1024),
            (1024, 1792)
        };

        if (supportedSizes.Any(s => s.Item1 == width && s.Item2 == height))
        {
            return options;
        }

        var aspectRatio = (double)width / height;
        var selectedSize = FindBestFitSize(aspectRatio);

        return new ImageGenerationOptions
        {
            Width = selectedSize.width,
            Height = selectedSize.height
        };
    }

    private (int width, int height) FindBestFitSize(double aspectRatio)
    {
        if (aspectRatio > 1.2)
            return (1792, 1024);

        if (aspectRatio < 0.8)
            return (1024, 1792);

        return (1024, 1024);
    }
}
