using ElBruno.Text2Image.Foundry;
using Xunit;

namespace ElBruno.Text2Image.Tests;

public class GptImageAzureEndpointNormalizationTests
{
    [Fact]
    public void GptImage2Endpoint_WithOpenAiV1Path_NormalizesToBareResourceUrl()
    {
        using var httpClient = new HttpClient();
        using var generator = new GptImage2Generator(
            endpoint: "https://my-resource.services.ai.azure.com/openai/v1",
            apiKey: "test-key",
            httpClient: httpClient,
            deploymentName: "gpt-image-2");

        Assert.Equal("https://my-resource.services.ai.azure.com", generator.Endpoint);
    }

    [Fact]
    public void GptImage2Endpoint_WithOpenAiPath_NormalizesToBareResourceUrl()
    {
        using var httpClient = new HttpClient();
        using var generator = new GptImage2Generator(
            endpoint: "https://my-resource.services.ai.azure.com/openai",
            apiKey: "test-key",
            httpClient: httpClient,
            deploymentName: "gpt-image-2");

        Assert.Equal("https://my-resource.services.ai.azure.com", generator.Endpoint);
    }

    [Fact]
    public void GptImage1p5Endpoint_WithOpenAiV1Path_NormalizesToBareResourceUrl()
    {
        using var httpClient = new HttpClient();
        using var generator = new GptImage1p5Generator(
            endpoint: "https://my-resource.openai.azure.com/openai/v1",
            apiKey: "test-key",
            httpClient: httpClient,
            deploymentName: "gpt-image-15");

        Assert.Equal("https://my-resource.openai.azure.com", generator.Endpoint);
    }
}
