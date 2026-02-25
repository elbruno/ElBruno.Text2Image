using Xunit;
using ElBruno.Text2Image.Models;

namespace ElBruno.Text2Image.Tests;

public class ImageGenerationOptionsTests
{
    [Fact]
    public void DefaultOptions_HaveExpectedValues()
    {
        var options = new ImageGenerationOptions();

        Assert.Equal(ExecutionProvider.Cpu, options.ExecutionProvider);
        Assert.Equal(20, options.NumInferenceSteps);
        Assert.Equal(7.5, options.GuidanceScale);
        Assert.Equal(512, options.Width);
        Assert.Equal(512, options.Height);
        Assert.Null(options.Seed);
        Assert.Null(options.ModelDirectory);
    }

    [Fact]
    public void GetModelDirectory_UsesCustomPath()
    {
        var options = new ImageGenerationOptions { ModelDirectory = "/tmp/models" };
        var path = options.GetModelDirectory("test-model");
        Assert.Contains("test-model", path);
    }

    [Fact]
    public void GetModelDirectory_DefaultsToLocalAppData()
    {
        var options = new ImageGenerationOptions();
        var path = options.GetModelDirectory("test-model");
        Assert.Contains("ElBruno", path);
        Assert.Contains("Text2Image", path);
        Assert.Contains("test-model", path);
    }
}

public class ImageGenerationResultTests
{
    [Fact]
    public void Result_HasRequiredProperties()
    {
        var result = new ImageGenerationResult
        {
            ImageBytes = new byte[] { 1, 2, 3 },
            ModelName = "Test Model",
            Prompt = "test prompt",
            Seed = 42,
            InferenceTimeMs = 1000,
            Width = 512,
            Height = 512
        };

        Assert.Equal(3, result.ImageBytes.Length);
        Assert.Equal("Test Model", result.ModelName);
        Assert.Equal("test prompt", result.Prompt);
        Assert.Equal(42, result.Seed);
        Assert.Equal(1000, result.InferenceTimeMs);
        Assert.Equal(512, result.Width);
        Assert.Equal(512, result.Height);
    }

    [Fact]
    public async Task SaveAsync_CreatesFile()
    {
        var result = new ImageGenerationResult
        {
            ImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }, // PNG magic bytes
            ModelName = "Test",
            Prompt = "test"
        };

        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.png");
        try
        {
            await result.SaveAsync(path);
            Assert.True(File.Exists(path));
            Assert.Equal(4, (await File.ReadAllBytesAsync(path)).Length);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

public class ExecutionProviderTests
{
    [Fact]
    public void ExecutionProvider_HasExpectedValues()
    {
        Assert.Equal(0, (int)ExecutionProvider.Cpu);
        Assert.Equal(1, (int)ExecutionProvider.Cuda);
        Assert.Equal(2, (int)ExecutionProvider.DirectML);
    }
}

public class StableDiffusion15Tests
{
    [Fact]
    public void ModelName_IsCorrect()
    {
        using var generator = new StableDiffusion15();
        Assert.Equal("Stable Diffusion 1.5", generator.ModelName);
    }

    [Fact]
    public void Constructor_AcceptsCustomOptions()
    {
        var options = new ImageGenerationOptions
        {
            NumInferenceSteps = 10,
            GuidanceScale = 5.0,
            Seed = 42
        };

        using var generator = new StableDiffusion15(options);
        Assert.NotNull(generator);
    }

    [Fact]
    public void Implements_IImageGenerator()
    {
        using var generator = new StableDiffusion15();
        Assert.IsAssignableFrom<IImageGenerator>(generator);
    }
}

public class DownloadProgressTests
{
    [Fact]
    public void DownloadProgress_CanSetProperties()
    {
        var progress = new DownloadProgress
        {
            Stage = DownloadStage.Downloading,
            PercentComplete = 50.0,
            BytesDownloaded = 1000,
            TotalBytes = 2000,
            CurrentFile = "model.onnx",
            Message = "Downloading..."
        };

        Assert.Equal(DownloadStage.Downloading, progress.Stage);
        Assert.Equal(50.0, progress.PercentComplete);
        Assert.Equal(1000, progress.BytesDownloaded);
        Assert.Equal(2000, progress.TotalBytes);
        Assert.Equal("model.onnx", progress.CurrentFile);
        Assert.Equal("Downloading...", progress.Message);
    }
}
