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
    public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
    public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "model" };

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
        var config = await _configStore.LoadAsync(ct);
        var providerCfg = config.Providers.GetValueOrDefault(Id);
        
        // Check endpoint from config first, fallback to secrets for backward compat
        var endpoint = providerCfg?.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = await _secretResolver.ResolveAsync(Id, "endpoint", null, ct);
        }

        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            return new ProviderHealth(
                Ok: false,
                Reason: "Missing endpoint/apiKey — run: t2i config");
        }

        // Security: Health checks no longer send credentials over the network.
        // Configuration presence is validated locally. Set T2I_DETAILED_HEALTH_CHECKS=1
        // to enable network connectivity tests (for debugging only).
        var detailedChecks = Environment.GetEnvironmentVariable("T2I_DETAILED_HEALTH_CHECKS");
        if (detailedChecks != "1" && detailedChecks != "true")
        {
            // Configuration is present - consider provider healthy
            return new ProviderHealth(Ok: true, Reason: null);
        }

        // Detailed health check mode (opt-in): Test actual endpoint connectivity
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
        var config = await _configStore.LoadAsync(ct);
        var providerCfg = config.Providers.GetValueOrDefault(Id);
        
        // Read endpoint from config first, fallback to secrets for backward compat
        var endpoint = providerCfg?.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = await _secretResolver.ResolveAsync(Id, "endpoint", null, ct);
        }
        
        // Read model from config, fallback to default
        var modelName = providerCfg?.Model ?? "MAI-Image-2";
        var modelId = modelName;  // For MAI, deployment name matches model name
        
        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Missing endpoint/apiKey — run: t2i config");
        }

        var width = req.Width > 0 ? req.Width : 1024;
        var height = req.Height > 0 ? req.Height : 1024;

        // MAI-Image-2 requires both dimensions ≥ 768px. The CLI's generic default is
        // 512 (suitable for SD/FLUX), so silently bump to MAI's preferred 1024 when
        // the request is below the minimum, surfacing a note via the progress channel.
        if (width < MinDimension || height < MinDimension)
        {
            var originalWidth = width;
            var originalHeight = height;
            width = Math.Max(width, 1024);
            height = Math.Max(height, 1024);
            progress?.Report(new GenerationProgress(
                0, 1,
                $"Adjusted size from {originalWidth}x{originalHeight} to {width}x{height} (MAI-Image-2 minimum is {MinDimension}px)"));
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
            httpClient,
            modelName: modelName,
            modelId: modelId);

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
                ["model"] = modelName,
                ["provider"] = "foundry-mai2",
                ["endpoint"] = endpoint
            });
    }
}
