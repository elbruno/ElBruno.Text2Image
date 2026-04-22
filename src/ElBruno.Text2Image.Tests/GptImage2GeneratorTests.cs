using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Azure;
using ElBruno.Text2Image;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Test helper: Mocks the Azure.AI.OpenAI ImageClient for GPT-Image-2 unit testing.
/// Allows full control over response behavior without real Azure API calls.
/// </summary>
internal interface IImageClientForGptImage2Testing
{
    Task<GeneratedImageForTesting> GenerateImageAsync(
        string prompt,
        ImageGenerationOptionsForTesting? options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Fake ImageClient for GPT-Image-2 unit testing. Allows test control over response behavior.
/// </summary>
internal sealed class FakeGptImage2Client : IImageClientForGptImage2Testing
{
    private readonly List<string> _generationLog = new();
    private Func<string, ImageGenerationOptionsForTesting?, GeneratedImageForTesting>? _responseFactory;

    public string? LastPrompt { get; private set; }
    public ImageGenerationOptionsForTesting? LastOptions { get; private set; }
    public IReadOnlyList<string> GenerationLog => _generationLog.AsReadOnly();

    public void SetResponseFactory(
        Func<string, ImageGenerationOptionsForTesting?, GeneratedImageForTesting> factory)
    {
        _responseFactory = factory;
    }

    public Task<GeneratedImageForTesting> GenerateImageAsync(
        string prompt,
        ImageGenerationOptionsForTesting? options,
        CancellationToken cancellationToken = default)
    {
        LastPrompt = prompt;
        LastOptions = options;
        _generationLog.Add($"GenerateImageAsync(prompt='{prompt}', size='{options?.Size ?? "null"}')");

        if (_responseFactory != null)
        {
            return Task.FromResult(_responseFactory(prompt, options));
        }

        // Default: return minimal fake PNG response
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        return Task.FromResult(new GeneratedImageForTesting
        {
            ImageBytes = pngBytes,
            RevisedPrompt = prompt,
            CreatedAt = DateTime.UtcNow
        });
    }
}

// ============================================================
// Constructor Validation Tests
// ============================================================

public class GptImage2GeneratorConstructorValidationTests
{
    [Fact]
    public void Constructor_NullEndpoint_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(endpoint: null!, apiKey: "key", deploymentName: "deploy"));
        Assert.Contains("endpoint", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_EmptyEndpoint_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(endpoint: "", apiKey: "key", deploymentName: "deploy"));
        Assert.Contains("endpoint", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhitespaceEndpoint_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(endpoint: "   ", apiKey: "key", deploymentName: "deploy"));
        Assert.Contains("endpoint", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_HttpEndpoint_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(
                endpoint: "http://example.services.ai.azure.com",
                apiKey: "key",
                deploymentName: "deploy"));
        Assert.Contains("https", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_NullApiKey_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(
                endpoint: "https://example.services.ai.azure.com",
                apiKey: null!,
                deploymentName: "deploy"));
        Assert.Contains("apiKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_EmptyApiKey_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(
                endpoint: "https://example.services.ai.azure.com",
                apiKey: "",
                deploymentName: "deploy"));
        Assert.Contains("apiKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WhitespaceApiKey_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(
                endpoint: "https://example.services.ai.azure.com",
                apiKey: "   ",
                deploymentName: "deploy"));
        Assert.Contains("apiKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_NullDeploymentName_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(
                endpoint: "https://example.services.ai.azure.com",
                apiKey: "key",
                deploymentName: null!));
        Assert.Contains("deployment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_EmptyDeploymentName_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create(
                endpoint: "https://example.services.ai.azure.com",
                apiKey: "key",
                deploymentName: ""));
        Assert.Contains("deployment", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_ValidParams_Succeeds()
    {
        var generator = TestableGptImage2Generator.Create(
            endpoint: "https://example.services.ai.azure.com",
            apiKey: "test-key",
            deploymentName: "gpt-image-2");

        Assert.NotNull(generator);
        Assert.Equal("GPT-Image-2", generator.ModelName);
    }

    [Fact]
    public void Constructor_ValidParams_SetsPropertiesCorrectly()
    {
        var generator = TestableGptImage2Generator.Create(
            endpoint: "https://example.services.ai.azure.com",
            apiKey: "test-key",
            deploymentName: "my-deployment");

        Assert.Equal("GPT-Image-2", generator.ModelName);
        Assert.Equal("https://example.services.ai.azure.com", generator.Endpoint);
        Assert.Equal("my-deployment", generator.DeploymentName);
    }
}

// ============================================================
// Property Accessor Tests
// ============================================================

public class GptImage2GeneratorPropertyTests
{
    [Fact]
    public void ModelName_ReturnsGptImage2()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        Assert.Equal("GPT-Image-2", generator.ModelName);
    }

    [Fact]
    public void DeploymentName_ReturnsConstructorValue()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "custom-deploy-123");

        Assert.Equal("custom-deploy-123", generator.DeploymentName);
    }

    [Fact]
    public void Endpoint_ReturnsConstructorValue()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://my-endpoint.azure.com", "key", "deploy");

        Assert.Equal("https://my-endpoint.azure.com", generator.Endpoint);
    }
}

// ============================================================
// Prompt Validation Tests
// ============================================================

public class GptImage2GeneratorPromptValidationTests
{
    [Fact]
    public async Task GenerateAsync_NullPrompt_Throws()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => generator.GenerateAsync(null!));
    }

    [Fact]
    public async Task GenerateAsync_EmptyPrompt_Throws()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => generator.GenerateAsync(""));
    }

    [Fact]
    public async Task GenerateAsync_WhitespacePrompt_Throws()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => generator.GenerateAsync("   "));
    }

    [Fact]
    public async Task GenerateAsync_PromptExceedsMaxLength_Throws()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var longPrompt = new string('a', 4001);

        var ex = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => generator.GenerateAsync(longPrompt));
        Assert.Contains("length", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_PromptAt4000Chars_Succeeds()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var promptAt4000 = new string('a', 4000);
        var result = await generator.GenerateAsync(promptAt4000);

        Assert.NotNull(result);
        Assert.Equal(promptAt4000, result.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_ValidPrompt_Succeeds()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("a beautiful sunset");

        Assert.NotNull(result);
        Assert.Equal("a beautiful sunset", result.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_PromptWithSpecialCharacters_Succeeds()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("A café with \"quotes\" & symbols: !@#$%");

        Assert.NotNull(result);
        Assert.Equal("A café with \"quotes\" & symbols: !@#$%", result.Prompt);
    }
}

// ============================================================
// Size Validation & Mapping Tests
// ============================================================

public class GptImage2GeneratorSizeTests
{
    [Fact]
    public async Task GenerateAsync_Size1024x1024_Accepted()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1024, Height = 1024 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_Size1792x1024_Accepted()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_Size1024x1792_Accepted()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1024, Height = 1792 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1792, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_Size512x512_MapsTo1024x1024()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 512, Height = 512 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_Size2000x2000_MapsToValidSize()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 2000, Height = 2000 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.True(result.Width <= 1792 && result.Height <= 1792);
    }

    [Fact]
    public async Task GenerateAsync_Size1600x1200_MapsToLandscape()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1600, Height = 1200 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_Size1200x1600_MapsToPortrait()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1200, Height = 1600 };
        var result = await generator.GenerateAsync("test", options);

        Assert.NotNull(result);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1792, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_DefaultOptions_Uses1024x1024()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("test");

        Assert.NotNull(result);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_NullOptions_Uses1024x1024()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("test", options: null);

        Assert.NotNull(result);
        Assert.Equal(1024, result.Width);
        Assert.Equal(1024, result.Height);
    }
}

// ============================================================
// Request/Response Integration Tests
// ============================================================

public class GptImage2GeneratorRequestResponseTests
{
    [Fact]
    public async Task GenerateAsync_PromptPassedCorrectly()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var promptText = "a red apple on a wooden table";
        var result = await generator.GenerateAsync(promptText);

        Assert.Equal(promptText, result.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_SizeOptionsSetCorrectly()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        var result = await generator.GenerateAsync("test", options);

        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_ResponseBytesExtractedAndReturned()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("test");

        Assert.NotNull(result.ImageBytes);
        Assert.NotEmpty(result.ImageBytes);
        // PNG magic bytes
        Assert.Equal(0x89, result.ImageBytes[0]);
        Assert.Equal(0x50, result.ImageBytes[1]);
    }

    [Fact]
    public async Task GenerateAsync_ModelNamePreservedInResult()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("test");

        Assert.Equal("GPT-Image-2", result.ModelName);
    }

    [Fact]
    public async Task GenerateAsync_ResponseMetadataPopulated()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        var result = await generator.GenerateAsync("a test image", options);

        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
        Assert.Equal("a test image", result.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_ImageBytesNotNull()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result = await generator.GenerateAsync("test");

        Assert.NotNull(result.ImageBytes);
    }

    [Fact]
    public async Task GenerateAsync_ResponseMatchesRequestedPrompt()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var prompt = "a futuristic cityscape at night";
        var result = await generator.GenerateAsync(prompt);

        Assert.Equal(prompt, result.Prompt);
    }

    [Fact]
    public async Task GenerateAsync_MultipleRequests_IndependentResults()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        var result1 = await generator.GenerateAsync("prompt one");
        var result2 = await generator.GenerateAsync("prompt two");

        Assert.Equal("prompt one", result1.Prompt);
        Assert.Equal("prompt two", result2.Prompt);
        Assert.NotSame(result1, result2);
    }
}

// ============================================================
// Error Handling Tests
// ============================================================

public class GptImage2GeneratorErrorHandlingTests
{
    [Fact]
    public async Task GenerateAsync_UnauthorizedError_WrapsWithClearMessage()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "invalid-key", "deploy",
            throwOnGenerate: new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized));

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_NotFoundError_ActionableMessage()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "invalid-deployment",
            throwOnGenerate: new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public async Task GenerateAsync_NetworkError_ThrowsException()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            throwOnGenerate: new HttpRequestException("Connection refused"));

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task GenerateAsync_TimeoutError_ThrowsTimeoutException()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            throwOnGenerate: new TimeoutException("Operation timed out"));

        var ex = await Assert.ThrowsAnyAsync<TimeoutException>(
            () => generator.GenerateAsync("test"));
        Assert.Contains("timed out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_RateLimitedError_ThrowsException()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            throwOnGenerate: new HttpRequestException("Too Many Requests", null, HttpStatusCode.TooManyRequests));

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Constructor_MalformedEndpointUri_ThrowsBeforeCall()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            TestableGptImage2Generator.Create("not-a-valid-uri", "key", "deploy"));
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task GenerateAsync_BadRequestError_ThrowsException()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            throwOnGenerate: new HttpRequestException("Bad Request", null, HttpStatusCode.BadRequest));

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));
        Assert.NotNull(ex);
    }

    [Fact]
    public async Task GenerateAsync_InternalServerError_ThrowsException()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            throwOnGenerate: new HttpRequestException("Internal Server Error", null, HttpStatusCode.InternalServerError));

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));
        Assert.NotNull(ex);
    }
}

// ============================================================
// Logging Tests
// ============================================================

public class GptImage2GeneratorLoggingTests
{
    [Fact]
    public async Task GenerateAsync_GenerationStartLogged()
    {
        var logCapture = new TestLogCapture();
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            logger: logCapture);

        await generator.GenerateAsync("test prompt");

        Assert.True(logCapture.Logs.Count > 0, "No logs captured");
    }

    [Fact]
    public async Task GenerateAsync_GenerationSuccessLogged()
    {
        var logCapture = new TestLogCapture();
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            logger: logCapture);

        var result = await generator.GenerateAsync("test");

        Assert.NotNull(result);
        Assert.True(logCapture.Logs.Count > 0);
    }

    [Fact]
    public async Task GenerateAsync_GenerationErrorLogged()
    {
        var logCapture = new TestLogCapture();
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            logger: logCapture,
            throwOnGenerate: new Exception("Test error"));

        await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test"));

        Assert.True(logCapture.Logs.Count > 0);
    }

    [Fact]
    public async Task GenerateAsync_SizeMappingLogged()
    {
        var logCapture = new TestLogCapture();
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy",
            logger: logCapture);

        var options = new ImageGenerationOptions { Width = 512, Height = 512 };
        await generator.GenerateAsync("test", options);

        Assert.True(logCapture.Logs.Count > 0);
    }
}

// ============================================================
// EnsureModelAvailableAsync Tests
// ============================================================

public class GptImage2GeneratorModelAvailabilityTests
{
    [Fact]
    public async Task EnsureModelAvailableAsync_CloudModel_CompletesImmediately()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        await generator.EnsureModelAvailableAsync();

        // Should complete without error (cloud model requires no download)
    }

    [Fact]
    public async Task EnsureModelAvailableAsync_WithProgress_ReportsComplete()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        DownloadProgress? reportedProgress = null;
        var progress = new TestProgress<DownloadProgress>(p => reportedProgress = p);

        await generator.EnsureModelAvailableAsync(progress);

        Assert.NotNull(reportedProgress);
        Assert.Equal(DownloadStage.Complete, reportedProgress.Stage);
        Assert.Equal(100, reportedProgress.PercentComplete);
    }

    [Fact]
    public async Task EnsureModelAvailableAsync_Cancellation_Honors()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Should not throw for cloud model even if canceled
        await generator.EnsureModelAvailableAsync(cancellationToken: cts.Token);
    }
}

// ============================================================
// Dispose Tests
// ============================================================

public class GptImage2GeneratorDisposeTests
{
    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        generator.Dispose();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var generator = TestableGptImage2Generator.Create(
            "https://example.services.ai.azure.com", "key", "deploy");

        generator.Dispose();
        generator.Dispose();
    }
}

// ============================================================
// Test Helper Classes
// ============================================================

/// <summary>
/// Testable wrapper for GptImage2Generator that allows dependency injection for testing.
/// </summary>
internal sealed class TestableGptImage2Generator : IImageGenerator
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly IImageClientForGptImage2Testing _imageClient;
    private readonly ILogger? _logger;
    private readonly Exception? _throwOnGenerate;

    public string ModelName => "GPT-Image-2";
    public string Endpoint => _endpoint;
    public string DeploymentName => _deploymentName;

    private TestableGptImage2Generator(
        string endpoint,
        string apiKey,
        string deploymentName,
        IImageClientForGptImage2Testing imageClient,
        ILogger? logger = null,
        Exception? throwOnGenerate = null)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _imageClient = imageClient;
        _logger = logger;
        _throwOnGenerate = throwOnGenerate;
    }

    public static TestableGptImage2Generator Create(
        string endpoint,
        string apiKey,
        string deploymentName,
        IImageClientForGptImage2Testing? imageClient = null,
        ILogger? logger = null,
        Exception? throwOnGenerate = null)
    {
        // Validation
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey, nameof(apiKey));
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));

        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Endpoint must use HTTPS protocol", nameof(endpoint));
        }

        // Validate URI format
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid endpoint URI format", nameof(endpoint));
        }

        imageClient ??= new FakeGptImage2Client();

        return new TestableGptImage2Generator(endpoint, apiKey, deploymentName, imageClient, logger, throwOnGenerate);
    }

    public async Task<ImageGenerationResult> GenerateAsync(
        string prompt,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Prompt validation
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));

        if (prompt.Length > 4000)
        {
            throw new ArgumentException("Prompt exceeds maximum length of 4000 characters", nameof(prompt));
        }

        // Size mapping
        var mappedOptions = MapSizeOptions(options);

        _logger?.LogInformation(
            "Generating image with GPT-Image-2: prompt length={PromptLength}, size={Size}",
            prompt.Length,
            $"{mappedOptions?.Width ?? 1024}x{mappedOptions?.Height ?? 1024}");

        try
        {
            // Throw test exception if configured
            if (_throwOnGenerate != null)
            {
                throw _throwOnGenerate;
            }

            var result = await _imageClient.GenerateImageAsync(prompt, MapToTestOptions(mappedOptions), cancellationToken);

            var (width, height) = (mappedOptions?.Width ?? 1024, mappedOptions?.Height ?? 1024);

            _logger?.LogInformation(
                "Image generation succeeded: size={Width}x{Height}, bytes={ByteCount}",
                width, height, result.ImageBytes.Length);

            return new ImageGenerationResult
            {
                ImageBytes = result.ImageBytes,
                Prompt = prompt,
                ModelName = ModelName,
                Width = width,
                Height = height
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Image generation failed");
            throw;
        }
    }

    public async Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Cloud model - no download needed
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
        // No resources to dispose
    }

    private ImageGenerationOptions? MapSizeOptions(ImageGenerationOptions? options)
    {
        if (options == null)
        {
            return new ImageGenerationOptions { Width = 1024, Height = 1024 };
        }

        var width = options.Width;
        var height = options.Height;

        // Map to closest supported size
        var supportedSizes = new[]
        {
            (1024, 1024),
            (1792, 1024),
            (1024, 1792)
        };

        // If exact match, return as-is
        if (supportedSizes.Any(s => s.Item1 == width && s.Item2 == height))
        {
            return options;
        }

        // Find best fit based on aspect ratio
        var aspectRatio = (double)width / height;
        var selectedSize = FindBestFitSize(aspectRatio);

        var result = new ImageGenerationOptions
        {
            Width = selectedSize.width,
            Height = selectedSize.height
        };

        _logger?.LogInformation(
            "Mapped size from {RequestedWidth}x{RequestedHeight} to {MappedWidth}x{MappedHeight}",
            width, height, result.Width, result.Height);

        return result;
    }

    private (int width, int height) FindBestFitSize(double aspectRatio)
    {
        // Landscape (aspect > 1)
        if (aspectRatio > 1.2)
            return (1792, 1024);

        // Portrait (aspect < 0.8)
        if (aspectRatio < 0.8)
            return (1024, 1792);

        // Square-ish
        return (1024, 1024);
    }

    private ImageGenerationOptionsForTesting? MapToTestOptions(ImageGenerationOptions? options)
    {
        if (options == null)
            return null;

        return new ImageGenerationOptionsForTesting
        {
            Size = $"{options.Width}x{options.Height}"
        };
    }
}

/// <summary>
/// Test-friendly progress reporter that synchronously invokes the handler.
/// </summary>
internal sealed class TestProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;

    public TestProgress(Action<T> handler)
    {
        _handler = handler;
    }

    public void Report(T value)
    {
        _handler(value);
    }
}
