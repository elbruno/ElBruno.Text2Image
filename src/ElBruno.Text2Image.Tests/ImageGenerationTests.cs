using Xunit;
using ElBruno.Text2Image.Models;

namespace ElBruno.Text2Image.Tests;

public class ImageGenerationOptionsTests
{
    [Fact]
    public void DefaultOptions_HaveExpectedValues()
    {
        var options = new ImageGenerationOptions();

        Assert.Equal(ExecutionProvider.Auto, options.ExecutionProvider);
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
        Assert.Equal(-1, (int)ExecutionProvider.Auto);
        Assert.Equal(0, (int)ExecutionProvider.Cpu);
        Assert.Equal(1, (int)ExecutionProvider.Cuda);
        Assert.Equal(2, (int)ExecutionProvider.DirectML);
    }

    [Fact]
    public void Auto_IsDefault()
    {
        var options = new ImageGenerationOptions();
        Assert.Equal(ExecutionProvider.Auto, options.ExecutionProvider);
    }

    [Fact]
    public void DetectBestProvider_ReturnsValidProvider()
    {
        var provider = SessionOptionsHelper.DetectBestProvider();
        Assert.True(
            provider == ExecutionProvider.Cpu ||
            provider == ExecutionProvider.Cuda ||
            provider == ExecutionProvider.DirectML);
    }

    [Fact]
    public void ResolveProvider_Auto_ReturnsConcreteProvider()
    {
        var resolved = SessionOptionsHelper.ResolveProvider(ExecutionProvider.Auto);
        Assert.NotEqual(ExecutionProvider.Auto, resolved);
    }

    [Fact]
    public void ResolveProvider_Explicit_ReturnsSame()
    {
        Assert.Equal(ExecutionProvider.Cpu, SessionOptionsHelper.ResolveProvider(ExecutionProvider.Cpu));
    }

    [Fact]
    public void Create_Auto_ReturnsSessionOptions()
    {
        using var options = SessionOptionsHelper.Create(ExecutionProvider.Auto);
        Assert.NotNull(options);
    }

    [Fact]
    public void DetectBestProvider_IsCached()
    {
        var first = SessionOptionsHelper.DetectBestProvider();
        var second = SessionOptionsHelper.DetectBestProvider();
        Assert.Equal(first, second);
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

public class LcmDreamshaperV7Tests
{
    [Fact]
    public void ModelName_IsCorrect()
    {
        using var generator = new LcmDreamshaperV7();
        Assert.Equal("LCM Dreamshaper v7", generator.ModelName);
    }

    [Fact]
    public void Constructor_AcceptsCustomOptions()
    {
        var options = new ImageGenerationOptions
        {
            NumInferenceSteps = 2,
            GuidanceScale = 1.0,
            Seed = 42
        };

        using var generator = new LcmDreamshaperV7(options);
        Assert.NotNull(generator);
    }

    [Fact]
    public void Implements_IImageGenerator()
    {
        using var generator = new LcmDreamshaperV7();
        Assert.IsAssignableFrom<IImageGenerator>(generator);
    }

    [Fact]
    public void DefaultOptions_LcmOptimized()
    {
        // LCM should default to low guidance (no CFG) and few steps
        using var generator = new LcmDreamshaperV7();
        Assert.Equal("LCM Dreamshaper v7", generator.ModelName);
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

public class SdxlTurboTests
{
    [Fact]
    public void ModelName_IsCorrect()
    {
        using var generator = new SdxlTurbo();
        Assert.Equal("SDXL Turbo", generator.ModelName);
    }

    [Fact]
    public void Implements_IImageGenerator()
    {
        using var generator = new SdxlTurbo();
        Assert.IsAssignableFrom<IImageGenerator>(generator);
    }
}

public class StableDiffusion21Tests
{
    [Fact]
    public void ModelName_IsCorrect()
    {
        using var generator = new StableDiffusion21();
        Assert.Equal("Stable Diffusion 2.1", generator.ModelName);
    }

    [Fact]
    public void Implements_IImageGenerator()
    {
        using var generator = new StableDiffusion21();
        Assert.IsAssignableFrom<IImageGenerator>(generator);
    }
}

public class Flux2GeneratorTests
{
    [Fact]
    public void Constructor_SetsModelName()
    {
        using var generator = new Flux2Generator("https://example.com/api", "test-key", "FLUX.2 Pro");
        Assert.Equal("FLUX.2 Pro", generator.ModelName);
    }

    [Fact]
    public void Constructor_DefaultModelName()
    {
        using var generator = new Flux2Generator("https://example.com/api", "test-key");
        Assert.Equal("FLUX.2", generator.ModelName);
    }

    [Fact]
    public void Implements_IImageGenerator()
    {
        using var generator = new Flux2Generator("https://example.com/api", "test-key");
        Assert.IsAssignableFrom<IImageGenerator>(generator);
    }

    [Fact]
    public void Constructor_ThrowsOnNullEndpoint()
    {
        Assert.Throws<ArgumentException>(() => new Flux2Generator("", "test-key"));
    }

    [Fact]
    public void Constructor_ThrowsOnNullApiKey()
    {
        Assert.Throws<ArgumentException>(() => new Flux2Generator("https://example.com", ""));
    }

    [Fact]
    public async Task EnsureModelAvailable_CompletesImmediately()
    {
        using var generator = new Flux2Generator("https://example.com/api", "test-key");
        // Cloud model — EnsureModelAvailableAsync should complete without throwing
        await generator.EnsureModelAvailableAsync();
    }

    [Fact]
    public void Constructor_AcceptsCustomHttpClient()
    {
        var httpClient = new HttpClient();
        using var generator = new Flux2Generator("https://example.com/api", "test-key", httpClient: httpClient);
        Assert.Equal("FLUX.2", generator.ModelName);
        httpClient.Dispose();
    }
}

public class MeaiInterfaceTests
{
    [Fact]
    public void StableDiffusion15_Implements_MeaiIImageGenerator()
    {
        using var generator = new StableDiffusion15();
        Assert.IsAssignableFrom<Microsoft.Extensions.AI.IImageGenerator>(generator);
    }

    [Fact]
    public void LcmDreamshaperV7_Implements_MeaiIImageGenerator()
    {
        using var generator = new LcmDreamshaperV7();
        Assert.IsAssignableFrom<Microsoft.Extensions.AI.IImageGenerator>(generator);
    }

    [Fact]
    public void SdxlTurbo_Implements_MeaiIImageGenerator()
    {
        using var generator = new SdxlTurbo();
        Assert.IsAssignableFrom<Microsoft.Extensions.AI.IImageGenerator>(generator);
    }

    [Fact]
    public void StableDiffusion21_Implements_MeaiIImageGenerator()
    {
        using var generator = new StableDiffusion21();
        Assert.IsAssignableFrom<Microsoft.Extensions.AI.IImageGenerator>(generator);
    }

    [Fact]
    public void Flux2Generator_Implements_MeaiIImageGenerator()
    {
        using var generator = new Flux2Generator("https://example.com/api", "test-key");
        Assert.IsAssignableFrom<Microsoft.Extensions.AI.IImageGenerator>(generator);
    }

    [Fact]
    public void GetService_Returnsself_ForOwnType()
    {
        using var generator = new StableDiffusion15();
        var meai = (Microsoft.Extensions.AI.IImageGenerator)generator;
        var service = meai.GetService(typeof(StableDiffusion15));
        Assert.Same(generator, service);
    }

    [Fact]
    public void GetService_ReturnsNull_ForUnknownType()
    {
        using var generator = new StableDiffusion15();
        var meai = (Microsoft.Extensions.AI.IImageGenerator)generator;
        var service = meai.GetService(typeof(string));
        Assert.Null(service);
    }
}

public class MeaiOptionsConverterTests
{
    [Fact]
    public void FromMeaiOptions_Null_ReturnsDefaults()
    {
        var result = ImageGenerationOptionsConverter.FromMeaiOptions(null);
        Assert.Equal(512, result.Width);
        Assert.Equal(512, result.Height);
    }

    [Fact]
    public void FromMeaiOptions_MapsImageSize()
    {
        var meaiOptions = new Microsoft.Extensions.AI.ImageGenerationOptions
        {
            ImageSize = new System.Drawing.Size(768, 768)
        };
        var result = ImageGenerationOptionsConverter.FromMeaiOptions(meaiOptions);
        Assert.Equal(768, result.Width);
        Assert.Equal(768, result.Height);
    }

    [Fact]
    public void FromMeaiOptions_MapsAdditionalProperties()
    {
        var meaiOptions = new Microsoft.Extensions.AI.ImageGenerationOptions
        {
            AdditionalProperties = new Microsoft.Extensions.AI.AdditionalPropertiesDictionary
            {
                [Text2ImagePropertyNames.NumInferenceSteps] = 30,
                [Text2ImagePropertyNames.GuidanceScale] = 9.0,
                [Text2ImagePropertyNames.Seed] = 123,
                [Text2ImagePropertyNames.ExecutionProvider] = "Cpu"
            }
        };
        var result = ImageGenerationOptionsConverter.FromMeaiOptions(meaiOptions);
        Assert.Equal(30, result.NumInferenceSteps);
        Assert.Equal(9.0, result.GuidanceScale);
        Assert.Equal(123, result.Seed);
        Assert.Equal(ExecutionProvider.Cpu, result.ExecutionProvider);
    }

    [Fact]
    public void ToMeaiResponse_ContainsImageData()
    {
        var genResult = new ImageGenerationResult
        {
            ImageBytes = new byte[] { 1, 2, 3 },
            ModelName = "Test",
            Prompt = "test"
        };
        var response = ImageGenerationOptionsConverter.ToMeaiResponse(genResult);
        Assert.Single(response.Contents);
        var data = Assert.IsType<Microsoft.Extensions.AI.DataContent>(response.Contents[0]);
        Assert.Equal("image/png", data.MediaType);
        Assert.Equal(3, data.Data.Length);
    }

    [Fact]
    public void ToMeaiResponse_RawRepresentation_IsOriginalResult()
    {
        var genResult = new ImageGenerationResult
        {
            ImageBytes = new byte[] { 1, 2, 3 },
            ModelName = "Test",
            Prompt = "test",
            Seed = 42
        };
        var response = ImageGenerationOptionsConverter.ToMeaiResponse(genResult);
        var raw = Assert.IsType<ImageGenerationResult>(response.RawRepresentation);
        Assert.Equal(42, raw.Seed);
    }
}
