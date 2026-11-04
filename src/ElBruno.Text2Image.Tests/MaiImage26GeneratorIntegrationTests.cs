using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

/// <summary>
/// Live integration tests for MAI-Image-2.6 Foundry deployments.
/// Tests are skipped unless the matching endpoint and API key environment variables are set.
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class MaiImage26GeneratorIntegrationTests
{
    [SkippableFact]
    public async Task GenerateAsync_MaiImage26Deployment_ReturnsPngImage()
    {
        var endpoint = Environment.GetEnvironmentVariable("MAI_IMAGE_26_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("MAI_IMAGE_26_API_KEY");
        var model = Environment.GetEnvironmentVariable("MAI_IMAGE_26_MODEL");
        Skip.IfNot(!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey),
            "Set MAI_IMAGE_26_ENDPOINT and MAI_IMAGE_26_API_KEY to run the MAI-Image-2.6 live test.");

        await AssertGeneratesPngAsync(endpoint!, apiKey!, model ?? "MAI-Image-2.6");
    }

    [SkippableFact]
    public async Task GenerateAsync_MaiImage26FlashDeployment_ReturnsPngImage()
    {
        var endpoint = Environment.GetEnvironmentVariable("MAI_IMAGE_26_FLASH_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("MAI_IMAGE_26_FLASH_API_KEY");
        var model = Environment.GetEnvironmentVariable("MAI_IMAGE_26_FLASH_MODEL");
        Skip.IfNot(!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey),
            "Set MAI_IMAGE_26_FLASH_ENDPOINT and MAI_IMAGE_26_FLASH_API_KEY to run the MAI-Image-2.6-Flash live test.");

        await AssertGeneratesPngAsync(endpoint!, apiKey!, model ?? "MAI-Image-2.6-Flash");
    }

    private static async Task AssertGeneratesPngAsync(string endpoint, string apiKey, string model)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        using var generator = new MaiImage26Generator(
            endpoint,
            apiKey,
            httpClient,
            modelName: model,
            modelId: model);

        var result = await generator.GenerateAsync(
            "A simple bright red apple centered on a plain white background",
            new ImageGenerationOptions { Width = 768, Height = 768 });

        Assert.True(result.ImageBytes.Length > 8, "The model should return non-empty PNG image data.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, result.ImageBytes[..8]);
        Assert.Equal(768, result.Width);
        Assert.Equal(768, result.Height);
    }
}
