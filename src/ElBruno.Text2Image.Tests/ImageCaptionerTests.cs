using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;
using Xunit;

namespace ElBruno.Text2Image.Tests;

public class ImageCaptionerOptionsTests
{
    [Fact]
    public void GetModelDirectory_UsesDefaultPath_WhenNoCustomDirectory()
    {
        var options = new ImageCaptionerOptions();
        var path = options.GetModelDirectory("test-model");

        Assert.Contains("ElBruno", path);
        Assert.Contains("Text2Image", path);
        Assert.Contains("test-model", path);
    }

    [Fact]
    public void GetModelDirectory_UsesCustomDirectory_WhenProvided()
    {
        var options = new ImageCaptionerOptions { ModelDirectory = "/tmp/models" };
        var path = options.GetModelDirectory("test-model");

        Assert.StartsWith("/tmp/models", path);
        Assert.Contains("test-model", path);
    }

    [Fact]
    public void DefaultOptions_HasExpectedDefaults()
    {
        var options = new ImageCaptionerOptions();

        Assert.Equal(ExecutionProvider.Cpu, options.ExecutionProvider);
        Assert.Equal(50, options.MaxTokens);
        Assert.False(options.UseQuantized);
        Assert.Null(options.TextPrompt);
    }
}

public class ViTGpt2CaptionerTests
{
    [Fact]
    public void ModelName_ReturnsCorrectName()
    {
        using var captioner = new ViTGpt2Captioner();
        Assert.Equal("ViT-GPT2", captioner.ModelName);
    }

    [Fact]
    public void Constructor_AcceptsNullOptions()
    {
        using var captioner = new ViTGpt2Captioner(null);
        Assert.NotNull(captioner);
    }

    [Fact]
    public void Constructor_AcceptsCustomOptions()
    {
        var options = new ImageCaptionerOptions
        {
            MaxTokens = 30,
            UseQuantized = true
        };
        using var captioner = new ViTGpt2Captioner(options);
        Assert.NotNull(captioner);
    }
}

public class BlipCaptionerTests
{
    [Fact]
    public void ModelName_ReturnsCorrectName()
    {
        using var captioner = new BlipCaptioner();
        Assert.Equal("BLIP", captioner.ModelName);
    }

    [Fact]
    public void Constructor_AcceptsNullOptions()
    {
        using var captioner = new BlipCaptioner(null);
        Assert.NotNull(captioner);
    }
}

public class ImageCaptionResultTests
{
    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var result = new ImageCaptionResult
        {
            Caption = "A cat sitting on a table",
            ModelName = "test-model",
            InferenceTimeMs = 150
        };

        Assert.Equal("A cat sitting on a table", result.Caption);
        Assert.Equal("test-model", result.ModelName);
        Assert.Equal(150, result.InferenceTimeMs);
    }
}
