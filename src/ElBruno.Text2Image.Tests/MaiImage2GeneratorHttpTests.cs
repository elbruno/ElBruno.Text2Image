using System.Net;
using System.Text;
using System.Text.Json;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Helper to create a minimal successful MAI-Image-2 API response with a tiny PNG base64 payload.
/// </summary>
internal static class MaiImage2FakeResponses
{
    public static HttpResponseMessage CreateMaiImage2SuccessResponse()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var b64 = Convert.ToBase64String(pngBytes);
        var json = $$"""{"data":[{"b64_json":"{{b64}}"}]}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

// ============================================================
// Content-Length Fix — Verify ByteArrayContent usage
// ============================================================

public class MaiImage2GeneratorContentLengthTests
{
    [Fact]
    public async Task GenerateAsync_Request_HasContentLengthHeader()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        Assert.NotNull(handler.LastRequest?.Content?.Headers.ContentLength);
        Assert.True(handler.LastRequest!.Content!.Headers.ContentLength > 0,
            "Content-Length must be a positive value");
    }

    [Fact]
    public async Task GenerateAsync_Request_ContentLengthMatchesBodySize()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var declaredLength = handler.LastRequest!.Content!.Headers.ContentLength;
        var actualBytes = Encoding.UTF8.GetBytes(handler.LastRequestBody!);
        Assert.Equal(actualBytes.Length, declaredLength);
    }

    [Fact]
    public async Task GenerateAsync_Request_BodyIsValidJson()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        Assert.NotNull(handler.LastRequestBody);
        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.NotEqual(default, doc.RootElement);
    }

    [Fact]
    public async Task GenerateAsync_Request_ContainsExpectedFields()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1024, Height = 768 };
        await generator.GenerateAsync("a beautiful sunset", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("a beautiful sunset", root.GetProperty("prompt").GetString());
        Assert.Equal("mai-image-2", root.GetProperty("model").GetString());
        Assert.Equal(1024, root.GetProperty("width").GetInt32());
        Assert.Equal(768, root.GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_Request_ContentTypeIsJsonUtf8()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        var contentType = handler.LastRequest!.Content!.Headers.ContentType!;
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
    }

    [Fact]
    public async Task GenerateAsync_Request_HasApiKeyHeader()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "my-secret-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.True(handler.LastRequest!.Headers.TryGetValues("api-key", out var values));
        Assert.Contains("my-secret-key", values);
    }

    [Fact]
    public async Task GenerateAsync_Request_UsesPostMethod()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    }
}

// ============================================================
// Response handling
// ============================================================

public class MaiImage2GeneratorResponseTests
{
    [Fact]
    public async Task GenerateAsync_SuccessfulResponse_ReturnsResult()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var result = await generator.GenerateAsync("test prompt");

        Assert.NotNull(result);
        Assert.NotEmpty(result.ImageBytes);
        Assert.Equal("test prompt", result.Prompt);
        Assert.Equal("MAI-Image-2", result.ModelName);
    }

    [Fact]
    public async Task GenerateAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request body", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test"));
        Assert.Contains("BadRequest", ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_NotFoundResponse_IncludesHint()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Not found", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test"));
        Assert.Contains("Hint", ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_WithDefaultOptions_UsesDefaultDimensions()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(1024, doc.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(1024, doc.RootElement.GetProperty("height").GetInt32());
    }
}

// ============================================================
// Input validation
// ============================================================

public class MaiImage2GeneratorValidationTests
{
    [Fact]
    public void Constructor_NullEndpoint_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage2Generator(null!, "test-key", new HttpClient()));
    }

    [Fact]
    public void Constructor_EmptyEndpoint_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage2Generator("", "test-key", new HttpClient()));
    }

    [Fact]
    public void Constructor_NullApiKey_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage2Generator("https://example.services.ai.azure.com", null!, new HttpClient()));
    }

    [Fact]
    public void Constructor_EmptyApiKey_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage2Generator("https://example.services.ai.azure.com", "", new HttpClient()));
    }

    [Fact]
    public void Constructor_HttpEndpoint_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage2Generator("http://example.services.ai.azure.com", "test-key", new HttpClient()));
    }

    [Fact]
    public async Task GenerateAsync_NullPrompt_Throws()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => generator.GenerateAsync(null!));
    }

    [Fact]
    public async Task GenerateAsync_EmptyPrompt_Throws()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => generator.GenerateAsync(""));
    }

    [Fact]
    public async Task GenerateAsync_PromptExceedsMaxLength_Throws()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var longPrompt = new string('a', 32_001);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => generator.GenerateAsync(longPrompt));
    }
}

// ============================================================
// Endpoint URL building
// ============================================================

public class MaiImage2GeneratorEndpointTests
{
    [Fact]
    public void Constructor_BaseUrl_AppendsApiPath()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://myresource.services.ai.azure.com", "test-key", httpClient);

        Assert.Contains("/mai/v1/images/generations", generator.Endpoint);
    }

    [Fact]
    public void Constructor_FullUrl_UsesAsIs()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://myresource.services.ai.azure.com/mai/v1/images/generations", "test-key", httpClient);

        Assert.Equal("https://myresource.services.ai.azure.com/mai/v1/images/generations", generator.Endpoint);
    }

    [Fact]
    public void Constructor_OpenAiUrl_ConvertsToServicesUrl()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://myresource.openai.azure.com", "test-key", httpClient);

        Assert.Contains(".services.ai.azure.com", generator.Endpoint);
        Assert.DoesNotContain(".openai.azure.com", generator.Endpoint);
    }

    [Fact]
    public void Constructor_TrailingSlash_HandlesCorrectly()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://myresource.services.ai.azure.com/", "test-key", httpClient);

        // Trailing slash should not cause double slashes or path issues
        Assert.DoesNotContain("//mai", generator.Endpoint);
        Assert.Contains("/mai/v1/images/generations", generator.Endpoint);
    }
}

// ============================================================
// Width/Height validation
// ============================================================

public class MaiImage2GeneratorDimensionTests
{
    [Fact]
    public async Task GenerateAsync_ValidSquareDimensions_Succeeds()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1024, Height = 1024 };
        var result = await generator.GenerateAsync("test prompt", options);

        Assert.NotNull(result);
        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(1024, doc.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(1024, doc.RootElement.GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_ValidRectangularDimensions_Succeeds()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 768, Height = 1024 };
        var result = await generator.GenerateAsync("test prompt", options);

        Assert.NotNull(result);
        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(768, doc.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(1024, doc.RootElement.GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_DimensionsBelowMinimum_UsesOptionsAsIs()
    {
        // The generator passes option values through to the request body;
        // server-side validation may reject invalid dimensions.
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator("https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 512, Height = 512 };
        await generator.GenerateAsync("test prompt", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(512, doc.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(512, doc.RootElement.GetProperty("height").GetInt32());
    }
}

// ============================================================
// Default model values
// ============================================================

public class MaiImage2GeneratorModelDefaultsTests
{
    [Fact]
    public void Constructor_DefaultModelName()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        Assert.Equal("MAI-Image-2", generator.ModelName);
    }

    [Fact]
    public void Constructor_DefaultModelId()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        Assert.Equal("mai-image-2", generator.ModelId);
    }

    [Fact]
    public void Constructor_CustomModelName()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient,
            modelName: "Custom-MAI-Image");

        Assert.Equal("Custom-MAI-Image", generator.ModelName);
    }

    [Fact]
    public void Constructor_CustomModelId()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient,
            modelId: "custom-mai-image-2");

        Assert.Equal("custom-mai-image-2", generator.ModelId);
    }
}

// ============================================================
// HTTP body — model name flows through
// ============================================================

public class MaiImage2GeneratorModelHttpTests
{
    [Fact]
    public async Task GenerateAsync_CustomModelMAIImage2e_AppearsInRequestBody()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key",
            modelId: "MAI-Image-2e", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("MAI-Image-2e", doc.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task GenerateAsync_DefaultModel_AppearsInRequestBody()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("mai-image-2", doc.RootElement.GetProperty("model").GetString());
    }
}

// ============================================================
// JSON serialization of internal request type
// ============================================================

public class MaiImage2RequestSerializationTests
{
    [Fact]
    public async Task Serialize_ContainsAllRequiredFields()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 1024, Height = 768 };
        await generator.GenerateAsync("hello world", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("hello world", root.GetProperty("prompt").GetString());
        Assert.Equal("mai-image-2", root.GetProperty("model").GetString());
        Assert.Equal(1024, root.GetProperty("width").GetInt32());
        Assert.Equal(768, root.GetProperty("height").GetInt32());
    }

    [Fact]
    public async Task Serialize_DoesNotContainExtraFields()
    {
        var handler = new FakeHttpHandler(_ => MaiImage2FakeResponses.CreateMaiImage2SuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage2Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        // MAI-Image-2 request should NOT contain FLUX-specific fields
        Assert.False(root.TryGetProperty("n", out _),
            "MAI-Image-2 request should not contain 'n' field");
        Assert.False(root.TryGetProperty("output_format", out _),
            "MAI-Image-2 request should not contain 'output_format' field");
        Assert.False(root.TryGetProperty("referenceImages", out _),
            "MAI-Image-2 request should not contain 'referenceImages' field");
    }
}
