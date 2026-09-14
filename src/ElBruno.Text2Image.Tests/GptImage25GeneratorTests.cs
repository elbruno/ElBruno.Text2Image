using ElBruno.Text2Image.Foundry;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ElBruno.Text2Image.Tests;

public sealed class GptImage25GeneratorTests
{
    [Theory]
    [InlineData("GPT-Image-2.5-Sunburst", "gpt-image-2.5-sunburst")]
    [InlineData("GPT-Image-2.5-Flare", "gpt-image-2.5-flare")]
    public void Constructor_UsesConfiguredVariant(string modelName, string deploymentName)
    {
        using var generator = new GptImage25Generator(
            "https://example.openai.azure.com",
            "test-key",
            new HttpClient(),
            modelName,
            deploymentName);

        Assert.Equal(modelName, generator.ModelName);
        Assert.Equal(deploymentName, generator.DeploymentName);
        Assert.Equal("https://example.openai.azure.com", generator.Endpoint);
    }

    [Fact]
    public void Constructor_NormalizesOpenAiPath()
    {
        using var generator = new GptImage25Generator(
            "https://example.openai.azure.com/openai/v1",
            "test-key",
            new HttpClient());

        Assert.Equal("https://example.openai.azure.com", generator.Endpoint);
    }

    [Fact]
    public async Task GenerateAsync_UsesInjectedTransportAndMappedLandscapeSize()
    {
        using var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        using var generator = new GptImage25Generator(
            "https://example.openai.azure.com",
            "test-key",
            httpClient,
            "GPT-Image-2.5-Flare",
            "flare-deployment");

        var result = await generator.GenerateAsync(
            "a wide landscape",
            new ImageGenerationOptions { Width = 1792, Height = 1024 });

        Assert.Equal(1792, result.Width);
        Assert.Equal(1024, result.Height);
        Assert.NotNull(handler.RequestBody);
        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("1792x1024", body.RootElement.GetProperty("size").GetString());
        Assert.Contains("flare-deployment", handler.RequestUri!.ToString());
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            var image = Convert.ToBase64String(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{"created":1234,"data":[{"b64_json":"{{image}}"}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
