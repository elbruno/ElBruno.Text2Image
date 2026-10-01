#if NET10_0_OR_GREATER
using ElBruno.Text2Image.Cli.Commands;
using ElBruno.Text2Image.Cli.Providers;
using Xunit;

namespace ElBruno.Text2Image.Tests.Cli;

public class ReferenceImageInputsTests : IDisposable
{
    private readonly string _tempDir;

    public ReferenceImageInputsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"t2i-refimg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    private sealed class StubProvider(string id, ReferenceImageCapabilities? caps) : IProviderAdapter
    {
        public string Id => id;
        public string DisplayName => id;
        public ProviderKind Kind => ProviderKind.Cloud;
        public IReadOnlyList<string> RequiredSecrets => [];
        public ReferenceImageCapabilities? ReferenceImageSupport => caps;
        public Task<ProviderHealth> CheckAsync(CancellationToken ct) => Task.FromResult(new ProviderHealth(true, null));
        public Task<GenerationResult> GenerateAsync(GenerationRequest req, IProgress<GenerationProgress>? progress, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private static readonly StubProvider Gpt = new("foundry-gpt-image-2", new(16, true, true, ReferenceImageCapabilities.PngJpegWebp));
    private static readonly StubProvider Mai = new("foundry-mai25", new(5, false, false, ReferenceImageCapabilities.PngJpeg));
    private static readonly StubProvider None = new("foundry-mai2", null);

    private string WriteFile(string name, byte[] bytes)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [Fact]
    public void NoInputs_ReturnsEmpty()
    {
        var (result, error) = ReferenceImageInputs.Prepare(None, null, null, null);
        Assert.Null(error);
        Assert.Null(result!.Images);
    }

    [Fact]
    public void UnsupportedProvider_FailsFast()
    {
        var file = WriteFile("a.png", ReferenceImageTestData.Png);
        var (_, error) = ReferenceImageInputs.Prepare(None, [file], null, null);
        Assert.Contains("does not support reference images", error);
        Assert.Contains("foundry-mai2", error);
    }

    [Fact]
    public void LocalFile_IsConvertedToDataUri()
    {
        var file = WriteFile("a.jpg", ReferenceImageTestData.Jpeg);
        var (result, error) = ReferenceImageInputs.Prepare(Gpt, [file], null, null);
        Assert.Null(error);
        Assert.StartsWith("data:image/jpeg;base64,", result!.Images![0]);
    }

    [Fact]
    public void HttpsUrl_PassesThrough()
    {
        var (result, error) = ReferenceImageInputs.Prepare(Gpt, ["https://example.com/a.png"], null, null);
        Assert.Null(error);
        Assert.Equal("https://example.com/a.png", result!.Images![0]);
    }

    [Fact]
    public void HttpUrl_IsRejected()
    {
        var (_, error) = ReferenceImageInputs.Prepare(Gpt, ["http://example.com/a.png"], null, null);
        Assert.Contains("HTTPS", error);
    }

    [Fact]
    public void MissingFile_ReturnsError()
    {
        var (_, error) = ReferenceImageInputs.Prepare(Gpt, [Path.Combine(_tempDir, "missing.png")], null, null);
        Assert.Contains("not found", error);
    }

    [Fact]
    public void InvalidFormat_ReturnsError()
    {
        var file = WriteFile("a.png", "not an image"u8.ToArray());
        var (_, error) = ReferenceImageInputs.Prepare(Gpt, [file], null, null);
        Assert.Contains("Unsupported image format", error);
    }

    [Fact]
    public void WebpRejectedForMai()
    {
        var file = WriteFile("a.webp", ReferenceImageTestData.Webp);
        var (_, error) = ReferenceImageInputs.Prepare(Mai, [file], null, null);
        Assert.NotNull(error);
        Assert.Contains("image/webp", error);
    }

    [Fact]
    public void TooManyImages_ReturnsError()
    {
        var file = WriteFile("a.png", ReferenceImageTestData.Png);
        var (_, error) = ReferenceImageInputs.Prepare(Mai, Enumerable.Repeat(file, 6).ToArray(), null, null);
        Assert.Contains("at most 5", error);
    }

    [Fact]
    public void MaskWithoutImage_ReturnsError()
    {
        var mask = WriteFile("m.png", ReferenceImageTestData.Png);
        var (_, error) = ReferenceImageInputs.Prepare(Gpt, null, mask, null);
        Assert.Contains("--mask requires", error);
    }

    [Fact]
    public void MaskOnUnsupportedProvider_ReturnsError()
    {
        var file = WriteFile("a.png", ReferenceImageTestData.Png);
        var (_, error) = ReferenceImageInputs.Prepare(Mai, [file], file, null);
        Assert.Contains("does not support --mask", error);
    }

    [Fact]
    public void NonPngMask_ReturnsError()
    {
        var file = WriteFile("a.png", ReferenceImageTestData.Png);
        var mask = WriteFile("m.jpg", ReferenceImageTestData.Jpeg);
        var (_, error) = ReferenceImageInputs.Prepare(Gpt, [file], mask, null);
        Assert.NotNull(error);
        Assert.StartsWith("--mask", error);
    }

    [Fact]
    public void ValidMaskAndFidelity_AreReturned()
    {
        var file = WriteFile("a.png", ReferenceImageTestData.Png);
        var (result, error) = ReferenceImageInputs.Prepare(Gpt, [file], file, "HIGH");
        Assert.Null(error);
        Assert.StartsWith("data:image/png;base64,", result!.Mask);
        Assert.Equal("high", result.InputFidelity);
    }

    [Fact]
    public void InvalidFidelity_ReturnsError()
    {
        var file = WriteFile("a.png", ReferenceImageTestData.Png);
        var (_, error) = ReferenceImageInputs.Prepare(Gpt, [file], null, "medium");
        Assert.Contains("low' or 'high", error);
    }

    [Fact]
    public void GenerationRequest_WithReferenceInputs_CopiesValues()
    {
        var req = new GenerationRequest("p", 1024, 1024, 1, "o.png", new Dictionary<string, string?>(),
            ["data:image/png;base64,AA"], "data:image/png;base64,BB", "low");
        var options = req.WithReferenceInputs(new ImageGenerationOptions());
        Assert.Single(options.ReferenceImages!);
        Assert.Equal("data:image/png;base64,BB", options.MaskImage);
        Assert.Equal("low", options.InputFidelity);
    }

    [Fact]
    public void GenerationRequest_WithoutReferenceInputs_LeavesOptionsUntouched()
    {
        var req = new GenerationRequest("p", 1024, 1024, 1, "o.png", new Dictionary<string, string?>());
        var options = req.WithReferenceInputs(new ImageGenerationOptions());
        Assert.Null(options.ReferenceImages);
        Assert.Null(options.MaskImage);
        Assert.Null(options.InputFidelity);
    }

    [Theory]
    [InlineData("foundry-flux2", 8, false)]
    [InlineData("foundry-gpt-image-1p5", 16, true)]
    [InlineData("foundry-gpt-image-2", 16, true)]
    [InlineData("foundry-gpt-image-25-sunburst", 16, true)]
    [InlineData("foundry-gpt-image-25-flare", 16, true)]
    [InlineData("foundry-mai25", 5, false)]
    [InlineData("foundry-mai25-flash", 5, false)]
    public void RegisteredProviders_DeclareCapabilities(string id, int maxImages, bool mask)
    {
        var provider = CreateRegistry().Get(id);
        Assert.NotNull(provider);
        var caps = provider!.ReferenceImageSupport;
        Assert.NotNull(caps);
        Assert.Equal(maxImages, caps!.MaxImages);
        Assert.Equal(mask, caps.SupportsMask);
    }

    [Fact]
    public void RetiredMai2_HasNoReferenceImageSupport()
    {
        Assert.Null(CreateRegistry().Get("foundry-mai2")!.ReferenceImageSupport);
    }

    private sealed class LocalHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private static ProviderRegistry CreateRegistry()
    {
        var factory = new LocalHttpClientFactory();
        var resolver = new ElBruno.Text2Image.Cli.Secrets.SecretResolver([]);
        var store = new ElBruno.Text2Image.Cli.Config.ConfigStore();
        return new ProviderRegistry(new IProviderAdapter[]
        {
            new FoundryFlux2Adapter(factory, resolver, store),
            new FoundryMaiImage2Adapter(factory, resolver, store),
            new FoundryMaiImage25Adapter(factory, resolver, store),
            new FoundryMaiImage25FlashAdapter(factory, resolver, store),
            new FoundryGptImage1p5Adapter(factory, resolver, store),
            new FoundryGptImage2Adapter(factory, resolver, store),
            new FoundryGptImage25SunburstAdapter(factory, resolver, store),
            new FoundryGptImage25FlareAdapter(factory, resolver, store)
        });
    }
}
#endif
