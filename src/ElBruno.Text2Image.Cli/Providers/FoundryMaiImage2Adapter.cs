using System.Diagnostics;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Http;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for Microsoft Foundry MAI-Image-2 cloud API.
/// </summary>
internal sealed class FoundryMaiImage2Adapter : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    private const int MinDimension = 768;
    private const int MaxTotalPixels = 1_048_576;

    public string Id => "foundry-mai2";
    public string DisplayName => "MAI-Image-2 (Cloud)";
    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "endpoint", "apiKey" };

    public FoundryMaiImage2Adapter(
        IHttpClientFactory httpClientFactory,
        SecretResolver secretResolver,
        ConfigStore configStore)
    {
        _httpClientFactory = httpClientFactory;
        _secretResolver = secretResolver;
        _configStore = configStore;
    }

    public async Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        var endpoint = await _secretResolver.ResolveAsync(Id, "endpoint", null, ct);
        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new ProviderHealth(
                Ok: false,
                Reason: "Missing endpoint/apiKey — run: t2i secrets set foundry-mai2");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await httpClient.SendAsync(request, ct);
            
            return new ProviderHealth(Ok: true, Reason: null);
        }
        catch (Exception ex)
        {
            return new ProviderHealth(
                Ok: false,
                Reason: $"Endpoint unreachable: {ex.Message}");
        }
    }

    public async Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct)
    {
        var endpoint = await _secretResolver.ResolveAsync(Id, "endpoint", null, ct);
        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Missing endpoint/apiKey — run: t2i secrets set foundry-mai2");
        }

        var width = req.Width > 0 ? req.Width : 1024;
        var height = req.Height > 0 ? req.Height : 1024;

        if (width < MinDimension || height < MinDimension)
        {
            throw new ArgumentException(
                $"MAI-Image-2 requires both dimensions to be at least {MinDimension}px");
        }

        if (width * height > MaxTotalPixels)
        {
            throw new ArgumentException(
                $"MAI-Image-2 maximum total pixels is {MaxTotalPixels:N0} ({width}x{height} exceeds this)");
        }

        var sw = Stopwatch.StartNew();

        var httpClient = _httpClientFactory.CreateClient();
        using var generator = new MaiImage2Generator(
            endpoint,
            apiKey,
            modelName: "MAI-Image-2",
            modelId: "mai-image-2",
            httpClient: httpClient);

        progress?.Report(new GenerationProgress(0, 1, "Calling Microsoft Foundry API..."));

        var result = await generator.GenerateAsync(req.Prompt, new ImageGenerationOptions
        {
            Width = width,
            Height = height
        }, cancellationToken: ct);

        await result.SaveAsync(req.OutputPath);
        sw.Stop();

        return new GenerationResult(
            OutputPath: req.OutputPath,
            Duration: sw.Elapsed,
            ActualWidth: result.Width,
            ActualHeight: result.Height,
            Metadata: new Dictionary<string, string>
            {
                ["model"] = "MAI-Image-2",
                ["provider"] = "foundry-mai2",
                ["endpoint"] = endpoint
            });
    }
}
