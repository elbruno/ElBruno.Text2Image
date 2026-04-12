using System.Net;
using System.Text;
using System.Text.Json;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Test helper: intercepts HttpClient requests so we can inspect headers, body, etc.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        _responseFactory = responseFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (request.Content != null)
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        return _responseFactory(request);
    }

    /// <summary>
    /// Creates a minimal successful FLUX.2 API response with a tiny PNG base64 payload.
    /// </summary>
    public static HttpResponseMessage CreateSuccessResponse()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG magic bytes
        var b64 = Convert.ToBase64String(pngBytes);
        var json = $$"""{"created":1234,"data":[{"b64_json":"{{b64}}"}]}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }
}

// ============================================================
// Issue #5: Content-Length Fix — Verify ByteArrayContent usage
// ============================================================

public class Flux2GeneratorContentLengthTests
{
    [Fact]
    public async Task GenerateAsync_Request_HasContentLengthHeader()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        Assert.NotNull(handler.LastRequest?.Content?.Headers.ContentLength);
        Assert.True(handler.LastRequest!.Content!.Headers.ContentLength > 0,
            "Content-Length must be a positive value (BFL API rejects chunked encoding)");
    }

    [Fact]
    public async Task GenerateAsync_Request_ContentLengthMatchesBodySize()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var declaredLength = handler.LastRequest!.Content!.Headers.ContentLength;
        var actualBytes = Encoding.UTF8.GetBytes(handler.LastRequestBody!);
        Assert.Equal(actualBytes.Length, declaredLength);
    }

    [Fact]
    public async Task GenerateAsync_Request_BodyIsValidJson()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        Assert.NotNull(handler.LastRequestBody);
        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.NotEqual(default, doc.RootElement);
    }

    [Fact]
    public async Task GenerateAsync_Request_ContainsExpectedFields()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator(
            "https://example.com/api", "test-key", modelId: "FLUX.2-flex", httpClient: httpClient);

        var options = new ImageGenerationOptions { Width = 768, Height = 512 };
        await generator.GenerateAsync("a beautiful sunset", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var root = doc.RootElement;

        Assert.Equal("a beautiful sunset", root.GetProperty("prompt").GetString());
        Assert.Equal("FLUX.2-flex", root.GetProperty("model").GetString());
        Assert.Equal(768, root.GetProperty("width").GetInt32());
        Assert.Equal(512, root.GetProperty("height").GetInt32());
        Assert.Equal(1, root.GetProperty("n").GetInt32());
        Assert.Equal("png", root.GetProperty("output_format").GetString());
    }

    [Fact]
    public async Task GenerateAsync_Request_ContentTypeIsJsonUtf8()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        var contentType = handler.LastRequest!.Content!.Headers.ContentType!;
        Assert.Equal("application/json", contentType.MediaType);
        Assert.Equal("utf-8", contentType.CharSet);
    }

    [Fact]
    public async Task GenerateAsync_Request_HasApiKeyHeader()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "my-secret-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.True(handler.LastRequest!.Headers.TryGetValues("api-key", out var values));
        Assert.Contains("my-secret-key", values);
    }

    [Fact]
    public async Task GenerateAsync_Request_UsesPostMethod()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task GenerateAsync_SuccessfulResponse_ReturnsResult()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var result = await generator.GenerateAsync("test prompt");

        Assert.NotNull(result);
        Assert.NotEmpty(result.ImageBytes);
        Assert.Equal("test prompt", result.Prompt);
        Assert.Equal("FLUX.2-pro", result.ModelName);
    }

    [Fact]
    public async Task GenerateAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request body", Encoding.UTF8, "application/json")
        });
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

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
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => generator.GenerateAsync("test"));
        Assert.Contains("Hint", ex.Message);
        Assert.Contains("BFL Native API", ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_WithDefaultOptions_UsesDefaultDimensions()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(512, doc.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(512, doc.RootElement.GetProperty("height").GetInt32());
    }
}

// ============================================================
// Issue #6: ImageGenerationOptions.ReferenceImages property
// ============================================================

public class ImageGenerationOptionsReferenceImagesTests
{
    [Fact]
    public void ReferenceImages_DefaultIsNull()
    {
        var options = new ImageGenerationOptions();
        Assert.Null(options.ReferenceImages);
    }

    [Fact]
    public void ReferenceImages_CanBeSet()
    {
        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,abc123" }
        };
        Assert.NotNull(options.ReferenceImages);
        Assert.Single(options.ReferenceImages);
    }

    [Fact]
    public void ReferenceImages_RoundTrips()
    {
        var images = new List<string> { "data:image/png;base64,abc", "data:image/jpeg;base64,def" };
        var options = new ImageGenerationOptions { ReferenceImages = images };
        Assert.Same(images, options.ReferenceImages);
    }

    [Fact]
    public void ReferenceImages_EmptyList_IsNotNull()
    {
        var options = new ImageGenerationOptions { ReferenceImages = new List<string>() };
        Assert.NotNull(options.ReferenceImages);
        Assert.Empty(options.ReferenceImages);
    }

    [Fact]
    public void ReferenceImages_SingleImage()
    {
        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,iVBOR" }
        };
        Assert.Single(options.ReferenceImages);
        Assert.Equal("data:image/png;base64,iVBOR", options.ReferenceImages[0]);
    }

    [Fact]
    public void ReferenceImages_MultipleImages()
    {
        var images = new List<string>
        {
            "data:image/png;base64,img1",
            "data:image/jpeg;base64,img2",
            "data:image/webp;base64,img3"
        };
        var options = new ImageGenerationOptions { ReferenceImages = images };
        Assert.Equal(3, options.ReferenceImages!.Count);
    }

    [Fact]
    public void ReferenceImages_CanBeSetToNull()
    {
        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,abc" }
        };
        options.ReferenceImages = null;
        Assert.Null(options.ReferenceImages);
    }

    [Fact]
    public void DefaultOptions_StillHaveExpectedValues_WithReferenceImagesNull()
    {
        var options = new ImageGenerationOptions();
        Assert.Equal(512, options.Width);
        Assert.Equal(512, options.Height);
        Assert.Null(options.Seed);
        Assert.Null(options.ReferenceImages);
    }
}

// ============================================================
// Issue #6: AddReferenceImageFromFile convenience method
// ============================================================

public class AddReferenceImageFromFileTests
{
    [Fact]
    public void ReadsFile_ConvertsToBase64DataUri_Png()
    {
        var options = new ImageGenerationOptions();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        try
        {
            var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            File.WriteAllBytes(tempFile, pngBytes);

            options.AddReferenceImageFromFile(tempFile);

            Assert.NotNull(options.ReferenceImages);
            Assert.Single(options.ReferenceImages);

            var expectedB64 = Convert.ToBase64String(pngBytes);
            Assert.Equal($"data:image/png;base64,{expectedB64}", options.ReferenceImages[0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadsFile_ConvertsToBase64DataUri_Jpeg()
    {
        var options = new ImageGenerationOptions();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jpg");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0xFF, 0xD8, 0xFF });
            options.AddReferenceImageFromFile(tempFile);

            Assert.StartsWith("data:image/jpeg;base64,", options.ReferenceImages![0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadsFile_ConvertsToBase64DataUri_JpegExtension()
    {
        var options = new ImageGenerationOptions();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jpeg");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0xFF, 0xD8, 0xFF });
            options.AddReferenceImageFromFile(tempFile);

            Assert.StartsWith("data:image/jpeg;base64,", options.ReferenceImages![0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadsFile_ConvertsToBase64DataUri_Webp()
    {
        var options = new ImageGenerationOptions();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.webp");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x52, 0x49, 0x46, 0x46 }); // RIFF header
            options.AddReferenceImageFromFile(tempFile);

            Assert.StartsWith("data:image/webp;base64,", options.ReferenceImages![0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void MultipleFiles_AppendsToList()
    {
        var options = new ImageGenerationOptions();
        var tempFile1 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        var tempFile2 = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.jpg");
        try
        {
            File.WriteAllBytes(tempFile1, new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            File.WriteAllBytes(tempFile2, new byte[] { 0xFF, 0xD8, 0xFF });

            options.AddReferenceImageFromFile(tempFile1);
            options.AddReferenceImageFromFile(tempFile2);

            Assert.Equal(2, options.ReferenceImages!.Count);
            Assert.StartsWith("data:image/png;base64,", options.ReferenceImages[0]);
            Assert.StartsWith("data:image/jpeg;base64,", options.ReferenceImages[1]);
        }
        finally
        {
            if (File.Exists(tempFile1)) File.Delete(tempFile1);
            if (File.Exists(tempFile2)) File.Delete(tempFile2);
        }
    }

    [Fact]
    public void AppendsToExistingList()
    {
        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,existing" }
        };
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x89 });
            options.AddReferenceImageFromFile(tempFile);

            Assert.Equal(2, options.ReferenceImages!.Count);
            Assert.Equal("data:image/png;base64,existing", options.ReferenceImages[0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void UnknownExtension_UsesOctetStream()
    {
        var options = new ImageGenerationOptions();
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xyz");
        try
        {
            File.WriteAllBytes(tempFile, new byte[] { 0x00, 0x01, 0x02 });
            options.AddReferenceImageFromFile(tempFile);

            Assert.StartsWith("data:application/octet-stream;base64,", options.ReferenceImages![0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ThrowsOnNullPath()
    {
        var options = new ImageGenerationOptions();
        Assert.ThrowsAny<ArgumentException>(() => options.AddReferenceImageFromFile(null!));
    }

    [Fact]
    public void ThrowsOnEmptyPath()
    {
        var options = new ImageGenerationOptions();
        Assert.Throws<ArgumentException>(() => options.AddReferenceImageFromFile(""));
    }

    [Fact]
    public void ThrowsOnWhitespacePath()
    {
        var options = new ImageGenerationOptions();
        Assert.Throws<ArgumentException>(() => options.AddReferenceImageFromFile("   "));
    }

    [Fact]
    public void ThrowsOnNonExistentFile()
    {
        var options = new ImageGenerationOptions();
        var fakePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.png");
        Assert.ThrowsAny<IOException>(() =>
            options.AddReferenceImageFromFile(fakePath));
    }
}

// ============================================================
// Issue #6: Flux2Request JSON serialization (internal type)
// ============================================================

public class Flux2RequestSerializationTests
{
    [Fact]
    public void Serialize_WithoutReferenceImages_OmitsField()
    {
        var request = new Flux2Request
        {
            Prompt = "test",
            Model = "FLUX.2-pro"
        };

        var json = JsonSerializer.Serialize(request, Flux2JsonContext.Default.Flux2Request);
        var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("referenceImages", out _),
            "referenceImages should be omitted when null");
    }

    [Fact]
    public void Serialize_WithReferenceImages_IncludesArray()
    {
        var request = new Flux2Request
        {
            Prompt = "test",
            Model = "FLUX.2-pro",
            ReferenceImages = new List<string> { "data:image/png;base64,abc" }
        };

        var json = JsonSerializer.Serialize(request, Flux2JsonContext.Default.Flux2Request);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("referenceImages", out var refImages));
        Assert.Equal(JsonValueKind.Array, refImages.ValueKind);
        Assert.Single(refImages.EnumerateArray().ToList());
        Assert.Equal("data:image/png;base64,abc", refImages[0].GetString());
    }

    [Fact]
    public void Serialize_WithEmptyReferenceImages_IncludesEmptyArray()
    {
        var request = new Flux2Request
        {
            Prompt = "test",
            Model = "FLUX.2-pro",
            ReferenceImages = new List<string>()
        };

        var json = JsonSerializer.Serialize(request, Flux2JsonContext.Default.Flux2Request);
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("referenceImages", out var refImages));
        Assert.Equal(JsonValueKind.Array, refImages.ValueKind);
        Assert.Empty(refImages.EnumerateArray().ToList());
    }

    [Fact]
    public void Serialize_WithMultipleReferenceImages_IncludesAll()
    {
        var request = new Flux2Request
        {
            Prompt = "test",
            Model = "FLUX.2-pro",
            ReferenceImages = new List<string>
            {
                "data:image/png;base64,img1",
                "data:image/jpeg;base64,img2"
            }
        };

        var json = JsonSerializer.Serialize(request, Flux2JsonContext.Default.Flux2Request);
        var doc = JsonDocument.Parse(json);

        var items = doc.RootElement.GetProperty("referenceImages").EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("data:image/png;base64,img1", items[0].GetString());
        Assert.Equal("data:image/jpeg;base64,img2", items[1].GetString());
    }

    [Fact]
    public void Serialize_AlwaysIncludesRequiredFields()
    {
        var request = new Flux2Request
        {
            Prompt = "hello world",
            Model = "FLUX.2-flex"
        };

        var json = JsonSerializer.Serialize(request, Flux2JsonContext.Default.Flux2Request);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("hello world", root.GetProperty("prompt").GetString());
        Assert.Equal("FLUX.2-flex", root.GetProperty("model").GetString());
        Assert.True(root.TryGetProperty("n", out _));
        Assert.True(root.TryGetProperty("width", out _));
        Assert.True(root.TryGetProperty("height", out _));
        Assert.True(root.TryGetProperty("output_format", out _));
    }
}

// ============================================================
// Issue #6: GenerateAsync with reference images (integration)
// ============================================================

public class Flux2GeneratorReferenceImagesTests
{
    [Fact]
    public async Task GenerateAsync_WithReferenceImages_IncludesInRequestBody()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,abc" }
        };

        await generator.GenerateAsync("test prompt", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.True(doc.RootElement.TryGetProperty("referenceImages", out var refImages),
            "Request body should contain 'referenceImages' when reference images are provided");
        Assert.Equal(JsonValueKind.Array, refImages.ValueKind);
        Assert.Equal("data:image/png;base64,abc", refImages[0].GetString());
    }

    [Fact]
    public async Task GenerateAsync_WithoutReferenceImages_OmitsFromRequestBody()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        await generator.GenerateAsync("test prompt");

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.False(doc.RootElement.TryGetProperty("referenceImages", out _),
            "Request body should NOT contain 'referenceImages' when none are provided");
    }

    [Fact]
    public async Task GenerateAsync_WithNullReferenceImages_OmitsFromRequestBody()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions { ReferenceImages = null };
        await generator.GenerateAsync("test prompt", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.False(doc.RootElement.TryGetProperty("referenceImages", out _));
    }

    [Fact]
    public async Task GenerateAsync_WithMultipleReferenceImages_IncludesAllInRequestBody()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string>
            {
                "data:image/png;base64,img1",
                "data:image/jpeg;base64,img2",
                "data:image/webp;base64,img3"
            }
        };

        await generator.GenerateAsync("test prompt", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var refImages = doc.RootElement.GetProperty("referenceImages");
        Assert.Equal(3, refImages.GetArrayLength());
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyReferenceImages_IncludesEmptyArrayInRequestBody()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string>()
        };

        await generator.GenerateAsync("test prompt", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.True(doc.RootElement.TryGetProperty("referenceImages", out var refImages));
        Assert.Equal(0, refImages.GetArrayLength());
    }

    [Fact]
    public async Task GenerateAsync_WithReferenceImages_StillIncludesPromptAndModel()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator(
            "https://example.com/api", "test-key", modelId: "FLUX.2-flex", httpClient: httpClient);

        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,abc" }
        };

        await generator.GenerateAsync("describe this image", options);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("describe this image", doc.RootElement.GetProperty("prompt").GetString());
        Assert.Equal("FLUX.2-flex", doc.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task GenerateAsync_WithFileBasedReferenceImage_IncludesDataUri()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        try
        {
            var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            File.WriteAllBytes(tempFile, pngBytes);

            var options = new ImageGenerationOptions();
            options.AddReferenceImageFromFile(tempFile);

            await generator.GenerateAsync("edit this image", options);

            var doc = JsonDocument.Parse(handler.LastRequestBody!);
            var refImages = doc.RootElement.GetProperty("referenceImages");
            Assert.Single(refImages.EnumerateArray().ToList());

            var dataUri = refImages[0].GetString()!;
            Assert.StartsWith("data:image/png;base64,", dataUri);
            Assert.Equal(Convert.ToBase64String(pngBytes), dataUri["data:image/png;base64,".Length..]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task GenerateAsync_ReturnsValidResult_WithReferenceImages()
    {
        var handler = new FakeHttpHandler(_ => FakeHttpHandler.CreateSuccessResponse());
        using var httpClient = new HttpClient(handler);
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);

        var options = new ImageGenerationOptions
        {
            ReferenceImages = new List<string> { "data:image/png;base64,abc" }
        };

        var result = await generator.GenerateAsync("test prompt", options);

        Assert.NotNull(result);
        Assert.NotEmpty(result.ImageBytes);
        Assert.Equal("test prompt", result.Prompt);
    }
}
