using System.Diagnostics;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Http;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for Microsoft Foundry FLUX.2 cloud API.
/// </summary>
internal sealed class FoundryFlux2Adapter : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    public string Id => "foundry-flux2";
    public string DisplayName => "FLUX.2 Pro (Cloud)";
    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
    public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "model" };

    public FoundryFlux2Adapter(
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
        var modelName = providerCfg?.Model ?? "FLUX.2-pro";
        var modelId = modelName;
        
        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Missing endpoint/apiKey — run: t2i config");
        }

        var sw = Stopwatch.StartNew();

        var httpClient = _httpClientFactory.CreateClient();
        using var generator = new Flux2Generator(
            endpoint,
            apiKey,
            modelName: modelName,
            modelId: modelId,
            httpClient: httpClient);

        progress?.Report(new GenerationProgress(0, 1, "Calling Microsoft Foundry API..."));

        var result = await generator.GenerateAsync(req.Prompt, new ImageGenerationOptions
        {
            Width = req.Width > 0 ? req.Width : 512,
            Height = req.Height > 0 ? req.Height : 512,
            NumInferenceSteps = req.Steps > 0 ? req.Steps : 20
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
                ["provider"] = "foundry-flux2",
                ["endpoint"] = endpoint
            });
    }
}
