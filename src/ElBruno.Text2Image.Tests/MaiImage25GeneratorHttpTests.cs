using System.Net;
using System.Text;
using System.Text.Json;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Helper to create a minimal successful MAI-Image-2.5 API response with a tiny PNG base64 payload.
/// </summary>
internal static class MaiImage25FakeResponses
{
    public static HttpResponseMessage CreateSuccessResponse()
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
// Request body
// ============================================================

public class MaiImage25GeneratorRequestTests
{
    [Fact]
    public async Task GenerateAsync_Request_ContainsExpectedFields()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        await generator.GenerateAsync("a beautiful sunset");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("a beautiful sunset", root.GetProperty("prompt").GetString());
        Assert.Equal("MAI-Image-2.5", root.GetProperty("model").GetString());
        Assert.Equal("1024x1024", root.GetProperty("size").GetString());
        Assert.Equal(1, root.GetProperty("n").GetInt32());
        Assert.Equal("png", root.GetProperty("output_format").GetString());
        Assert.Equal(100, root.GetProperty("output_compression").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_FlashModelId_SentInBody()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient,
            modelName: "MAI-Image-2.5-Flash", modelId: "MAI-Image-2.5-Flash");

        await generator.GenerateAsync("a fox");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("MAI-Image-2.5-Flash", doc.RootElement.GetProperty("model").GetString());
    }

    [Theory]
    [InlineData(1024, 1024, "1024x1024")]
    [InlineData(1536, 1024, "1024x1024")]
    [InlineData(1024, 1536, "1024x1024")]
    [InlineData(1920, 1080, "1024x1024")]
    [InlineData(1080, 1920, "1024x1024")]
    public async Task GenerateAsync_MapsDimensionsToSupportedSize(int width, int height, string expectedSize)
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        await generator.GenerateAsync("test", new ImageGenerationOptions { Width = width, Height = height });

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(expectedSize, doc.RootElement.GetProperty("size").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Request_HasApiKeyHeaders()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "my-secret-key", httpClient);

        await generator.GenerateAsync("test");

        Assert.True(handler.LastRequest!.Headers.TryGetValues("api-key", out var apiKeyValues));
        Assert.Contains("my-secret-key", apiKeyValues);
        Assert.True(handler.LastRequest!.Headers.TryGetValues("Authorization", out var authValues));
        Assert.Contains("Bearer my-secret-key", authValues);
    }

    [Fact]
    public async Task GenerateAsync_Request_UsesPostAndJsonUtf8()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        await generator.GenerateAsync("test");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        var contentType = handler.LastRequest!.Content!.Headers.ContentType!;
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
        Assert.True(handler.LastRequest!.Content!.Headers.ContentLength > 0);
    }
}

// ============================================================
// Response handling
// ============================================================

public class MaiImage25GeneratorResponseTests
{
    [Fact]
    public async Task GenerateAsync_SuccessfulResponse_ReturnsResult()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        var result = await generator.GenerateAsync("test prompt");

        Assert.NotNull(result);
        Assert.NotEmpty(result.ImageBytes);
        Assert.Equal("test prompt", result.Prompt);
        Assert.Equal("MAI-Image-2.5", result.ModelName);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request body", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

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
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test"));
        Assert.Contains("Hint", ex.Message);
    }
}

// ============================================================
// Input validation
// ============================================================

public class MaiImage25GeneratorValidationTests
{
    [Fact]
    public void Constructor_NullEndpoint_Throws()
        => Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage25Generator(null!, "test-key", new HttpClient()));

    [Fact]
    public void Constructor_EmptyApiKey_Throws()
        => Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage25Generator("https://example.services.ai.azure.com", "", new HttpClient()));

    [Fact]
    public void Constructor_HttpEndpoint_Throws()
        => Assert.ThrowsAny<ArgumentException>(() =>
            new MaiImage25Generator("http://example.services.ai.azure.com", "test-key", new HttpClient()));

    [Fact]
    public async Task GenerateAsync_EmptyPrompt_Throws()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => generator.GenerateAsync(""));
    }

    [Fact]
    public async Task GenerateAsync_PromptExceedsMaxLength_Throws()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new MaiImage25Generator(
            "https://example.services.ai.azure.com", "test-key", httpClient);

        var longPrompt = new string('a', 32_001);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => generator.GenerateAsync(longPrompt));
    }
}

// ============================================================
// Endpoint URL building
// ============================================================

public class MaiImage25GeneratorEndpointTests
{
    [Fact]
    public void Constructor_BaseUrl_AppendsMaiImagesPath()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage25Generator(
            "https://myresource.services.ai.azure.com", "test-key", httpClient);

        Assert.Contains("/mai/v1/images/generations", generator.Endpoint);
    }

    [Fact]
    public void Constructor_FullUrl_UsesAsIs()
    {
        using var httpClient = new HttpClient();
        const string full = "https://myresource.services.ai.azure.com/mai/v1/images/generations";
        using var generator = new MaiImage25Generator(full, "test-key", httpClient);

        Assert.Equal(full, generator.Endpoint);
    }

    [Fact]
    public void Constructor_LegacyOpenAiPath_NormalizesToMaiImagesPath()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage25Generator(
            "https://myresource.services.ai.azure.com/openai/v1/images/generations",
            "test-key",
            httpClient);

        Assert.Equal(
            "https://myresource.services.ai.azure.com/mai/v1/images/generations",
            generator.Endpoint);
    }

    [Fact]
    public void Constructor_OpenAiAzureUrl_ConvertsToServicesUrl()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage25Generator(
            "https://myresource.openai.azure.com", "test-key", httpClient);

        Assert.Contains(".services.ai.azure.com", generator.Endpoint);
        Assert.DoesNotContain(".openai.azure.com", generator.Endpoint);
        Assert.Contains("/mai/v1/images/generations", generator.Endpoint);
    }

    [Fact]
    public void Constructor_TrailingSlash_HandledCorrectly()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage25Generator(
            "https://myresource.services.ai.azure.com/", "test-key", httpClient);

        Assert.Equal(
            "https://myresource.services.ai.azure.com/mai/v1/images/generations",
            generator.Endpoint);
    }

    [Fact]
    public void DefaultModelId_IsMaiImage25()
    {
        using var httpClient = new HttpClient();
        using var generator = new MaiImage25Generator(
            "https://myresource.services.ai.azure.com", "test-key", httpClient);

        Assert.Equal("MAI-Image-2.5", generator.ModelId);
        Assert.Equal("MAI-Image-2.5", generator.ModelName);
    }
}
