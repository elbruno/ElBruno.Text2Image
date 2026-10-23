using System.Text.Json;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

public sealed class MaiImage26GeneratorHttpTests
{
    [Theory]
    [InlineData("MAI-Image-2.6")]
    [InlineData("MAI-Image-2.6-Flash")]
    public async Task GenerateAsync_Sends26RequestContract(string modelId)
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var client = new HttpClient(handler);
        using var generator = new MaiImage26Generator(
            "https://example.services.ai.azure.com",
            "test-key",
            client,
            modelName: modelId,
            modelId: modelId);

        var result = await generator.GenerateAsync("a detailed image", new ImageGenerationOptions
        {
            Width = 1536,
            Height = 1024
        });

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        var root = document.RootElement;
        Assert.Equal(modelId, root.GetProperty("model").GetString());
        Assert.Equal("a detailed image", root.GetProperty("prompt").GetString());
        Assert.Equal(1536, root.GetProperty("width").GetInt32());
        Assert.Equal(1024, root.GetProperty("height").GetInt32());
        Assert.False(root.TryGetProperty("size", out _));
        Assert.False(root.TryGetProperty("output_format", out _));
        Assert.Equal(1536, result.Width);
        Assert.Equal(1024, result.Height);
    }

    [Fact]
    public async Task GenerateAsync_SendsOptInControlsOnlyWhenSpecified()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var client = new HttpClient(handler);
        using var generator = new MaiImage26Generator(
            "https://example.services.ai.azure.com", "test-key", client);

        await generator.GenerateAsync("default controls");
        using (var defaultDocument = JsonDocument.Parse(handler.LastRequestBody!))
        {
            Assert.False(defaultDocument.RootElement.TryGetProperty("auto_aspect_ratio", out _));
            Assert.False(defaultDocument.RootElement.TryGetProperty("web_grounding", out _));
        }

        await generator.GenerateAsync("a grounded image", new ImageGenerationOptions
        {
            Width = 768,
            Height = 768,
            AutoAspectRatio = true,
            WebGrounding = false
        });

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        var root = document.RootElement;
        Assert.True(root.GetProperty("auto_aspect_ratio").GetBoolean());
        Assert.False(root.GetProperty("web_grounding").GetBoolean());
    }

    [Fact]
    public async Task GenerateAsync_AdjustsDimensionsBelowMinimum()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var client = new HttpClient(handler);
        using var generator = new MaiImage26Generator(
            "https://example.services.ai.azure.com", "test-key", client);

        var result = await generator.GenerateAsync("too small", new ImageGenerationOptions
        {
            Width = 760,
            Height = 768
        });

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(768, document.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(768, document.RootElement.GetProperty("height").GetInt32());
        Assert.Equal(768, result.Width);
    }

    [Fact]
    public async Task GenerateAsync_RejectsDimensionsAbovePixelLimit()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var client = new HttpClient(handler);
        using var generator = new MaiImage26Generator(
            "https://example.services.ai.azure.com", "test-key", client);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            generator.GenerateAsync("too large", new ImageGenerationOptions
            {
                Width = 1600,
                Height = 1480
            }));
    }

    [Fact]
    public async Task GenerateAsync_AcceptsMinimumAndMaximumPixelBoundaries()
    {
        var handler = new FakeHttpHandler(_ => MaiImage25FakeResponses.CreateSuccessResponse());
        using var client = new HttpClient(handler);
        using var generator = new MaiImage26Generator(
            "https://example.services.ai.azure.com", "test-key", client);

        await generator.GenerateAsync("minimum", new ImageGenerationOptions
        {
            Width = 768,
            Height = 768
        });
        await generator.GenerateAsync("maximum", new ImageGenerationOptions
        {
            Width = 1536,
            Height = 1536
        });

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal(1536, document.RootElement.GetProperty("width").GetInt32());
        Assert.Equal(1536, document.RootElement.GetProperty("height").GetInt32());
    }
}
