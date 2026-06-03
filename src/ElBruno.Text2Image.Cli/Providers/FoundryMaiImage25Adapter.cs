using System.Diagnostics;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Http;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Shared base for the Microsoft Foundry MAI-Image-2.5 family of cloud providers.
/// Concrete subclasses select the model variant (standard or Flash) via <see cref="DefaultModel"/>.
/// </summary>
internal abstract class FoundryMaiImage25AdapterBase : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    private const int MaxTotalPixels = 1_572_864; // 1024×1536

    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    protected abstract string DefaultModel { get; }

    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
    public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "model" };

    protected FoundryMaiImage25AdapterBase(
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
            return new ProviderHealth(Ok: true, Reason: null);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            await httpClient.SendAsync(request, ct);

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

        var endpoint = providerCfg?.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = await _secretResolver.ResolveAsync(Id, "endpoint", null, ct);
        }

        var modelName = providerCfg?.Model ?? DefaultModel;
        var modelId = modelName; // For MAI, the request body model matches the model name.

        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Missing endpoint/apiKey — run: t2i config");
        }

        var width = req.Width > 0 ? req.Width : 1024;
        var height = req.Height > 0 ? req.Height : 1024;

        // MAI-Image-2.5 uses fixed sizes with a minimum of 1024px. The CLI's generic default
        // is 512 (suitable for SD/FLUX), so bump small requests up to 1024 and note it.
        if (width < 1024 || height < 1024)
        {
            var originalWidth = width;
            var originalHeight = height;
            width = Math.Max(width, 1024);
            height = Math.Max(height, 1024);
            progress?.Report(new GenerationProgress(
                0, 1,
                $"Adjusted size from {originalWidth}x{originalHeight} to {width}x{height} ({DefaultModel} minimum is 1024px)"));
        }

        if (width * height > MaxTotalPixels)
        {
            throw new ArgumentException(
                $"{DefaultModel} maximum total pixels is {MaxTotalPixels:N0} ({width}x{height} exceeds this)");
        }

        var sw = Stopwatch.StartNew();

        var httpClient = _httpClientFactory.CreateClient();

        var timeoutSeconds = 300;
        if (req.ExtraOptions.TryGetValue("timeout", out var timeoutStr) &&
            int.TryParse(timeoutStr, out var parsed) &&
            parsed > 0)
        {
            timeoutSeconds = parsed;
        }

        using var generator = new MaiImage25Generator(
            endpoint,
            apiKey,
            httpClient,
            modelName: modelName,
            modelId: modelId,
            timeoutSeconds: timeoutSeconds);

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
                ["provider"] = Id,
                ["endpoint"] = endpoint
            });
    }
}

/// <summary>
/// Provider adapter for Microsoft Foundry MAI-Image-2.5 cloud API.
/// </summary>
internal sealed class FoundryMaiImage25Adapter : FoundryMaiImage25AdapterBase
{
    public override string Id => "foundry-mai25";
    public override string DisplayName => "MAI-Image-2.5 (Cloud)";
    protected override string DefaultModel => "MAI-Image-2.5";

    public FoundryMaiImage25Adapter(
        IHttpClientFactory httpClientFactory,
        SecretResolver secretResolver,
        ConfigStore configStore)
        : base(httpClientFactory, secretResolver, configStore)
    {
    }
}

/// <summary>
/// Provider adapter for Microsoft Foundry MAI-Image-2.5-Flash (speed-optimized) cloud API.
/// </summary>
internal sealed class FoundryMaiImage25FlashAdapter : FoundryMaiImage25AdapterBase
{
    public override string Id => "foundry-mai25-flash";
    public override string DisplayName => "MAI-Image-2.5-Flash (Cloud)";
    protected override string DefaultModel => "MAI-Image-2.5-Flash";

    public FoundryMaiImage25FlashAdapter(
        IHttpClientFactory httpClientFactory,
        SecretResolver secretResolver,
        ConfigStore configStore)
        : base(httpClientFactory, secretResolver, configStore)
    {
    }
}
