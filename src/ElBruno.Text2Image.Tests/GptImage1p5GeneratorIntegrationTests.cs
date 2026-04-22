using System.Drawing;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Integration tests for GPT-Image-1.5 generator with real Azure OpenAI endpoints.
/// These tests require Azure credentials and will be skipped if not configured.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public class GptImage1p5GeneratorIntegrationTests : IDisposable
{
    private readonly string? _endpoint = Environment.GetEnvironmentVariable("GPT_IMAGE_1P5_ENDPOINT");
    private readonly string? _apiKey = Environment.GetEnvironmentVariable("GPT_IMAGE_1P5_API_KEY");
    private readonly string? _model = Environment.GetEnvironmentVariable("GPT_IMAGE_1P5_MODEL");
    private readonly List<string> _generatedFiles = new();

    public void Dispose()
    {
        // Cleanup: remove generated test images
        foreach (var file in _generatedFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [SkippableFact]
    public async Task GenerateAsync_BasicPrompt_ProducesValidImage()
    {
        Skip.IfNot(_endpoint != null && _apiKey != null,
            "GPT-Image-1.5 credentials not configured (set GPT_IMAGE_1P5_ENDPOINT, GPT_IMAGE_1P5_API_KEY)");

        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(
            endpoint: _endpoint!,
            apiKey: _apiKey!,
            httpClient: httpClient,
            deploymentName: _model ?? "gpt-image-15");

        var result = await generator.GenerateAsync("a serene mountain landscape");

        Assert.NotNull(result);
        Assert.NotEmpty(result.ImageBytes);
        Assert.True(result.ImageBytes.Length > 1024, "Image should be at least 1KB");
        Assert.Equal(0x89, result.ImageBytes[0]); // PNG magic byte
    }

    [SkippableFact]
    public async Task GenerateAsync_Size1792x1024_VerifiesDimensions()
    {
        Skip.IfNot(_endpoint != null && _apiKey != null,
            "GPT-Image-1.5 credentials not configured");

        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(_endpoint!, _apiKey!, httpClient, _model ?? "gpt-image-15");

        var options = new ImageGenerationOptions { Width = 1792, Height = 1024 };
        var result = await generator.GenerateAsync("a cityscape at night", options);

        Assert.NotNull(result);
        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [SkippableFact]
    public async Task GenerateAsync_CompleteWithinTimeout_60Seconds()
    {
        Skip.IfNot(_endpoint != null && _apiKey != null,
            "GPT-Image-1.5 credentials not configured");

        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(_endpoint!, _apiKey!, httpClient, _model ?? "gpt-image-15");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await generator.GenerateAsync("a red apple", cancellationToken: cts.Token);
        sw.Stop();

        Assert.NotNull(result);
        Assert.True(sw.Elapsed.TotalSeconds < 60, "Generation should complete within 60 seconds");
    }

    [SkippableFact]
    public async Task GenerateAsync_InvalidEndpoint_ThrowsClearError()
    {
        Skip.IfNot(_apiKey != null,
            "GPT_IMAGE_1P5_API_KEY not configured");

        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(
            endpoint: "https://invalid-resource.services.ai.azure.com",
            apiKey: _apiKey!,
            httpClient: httpClient,
            deploymentName: _model ?? "gpt-image-15");

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test prompt"));

        Assert.NotNull(ex.Message);
    }

    [SkippableFact]
    public async Task GenerateAsync_InvalidApiKey_ThrowsAuthError()
    {
        Skip.IfNot(_endpoint != null,
            "GPT_IMAGE_1P5_ENDPOINT not configured");

        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(
            endpoint: _endpoint!,
            apiKey: "invalid-key-12345",
            httpClient: httpClient,
            deploymentName: _model ?? "gpt-image-15");

        var ex = await Assert.ThrowsAnyAsync<Exception>(
            () => generator.GenerateAsync("test prompt"));

        Assert.NotNull(ex.Message);
    }

    [SkippableFact]
    public async Task GenerateAsync_SaveResult_WritesValidPngFile()
    {
        Skip.IfNot(_endpoint != null && _apiKey != null,
            "GPT-Image-1.5 credentials not configured");

        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(_endpoint!, _apiKey!, httpClient, _model ?? "gpt-image-15");
        var outputPath = Path.Combine(Path.GetTempPath(), $"gpt-image-1p5-test-{Guid.NewGuid()}.png");
        _generatedFiles.Add(outputPath);

        var result = await generator.GenerateAsync("a beautiful sunset");
        await result.SaveAsync(outputPath);

        Assert.True(File.Exists(outputPath), "Image file should be written");
        Assert.True(new FileInfo(outputPath).Length > 0, "Image file should not be empty");

        var fileBytes = await File.ReadAllBytesAsync(outputPath);
        Assert.Equal(0x89, fileBytes[0]); // PNG magic byte validation
    }
}
