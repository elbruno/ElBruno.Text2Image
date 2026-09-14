using System.Diagnostics;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Http;

namespace ElBruno.Text2Image.Cli.Providers;

internal abstract class FoundryGptImage25AdapterBase : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SecretResolver _secretResolver;
    private readonly ConfigStore _configStore;

    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string DefaultModel { get; }
    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
    public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "model" };

    protected FoundryGptImage25AdapterBase(
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
            return new ProviderHealth(false, "Missing endpoint/apiKey — run: t2i config");

        if (!TryValidateEndpoint(endpoint, out var endpointError))
            return new ProviderHealth(false, endpointError);

        var detailedChecks = Environment.GetEnvironmentVariable("T2I_DETAILED_HEALTH_CHECKS");
        if (detailedChecks != "1" && detailedChecks != "true")
            return new ProviderHealth(true, null);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
            await httpClient.SendAsync(request, ct);
            return new ProviderHealth(true, null);
        }
        catch (Exception ex)
        {
            return new ProviderHealth(false, $"Endpoint unreachable: {ex.Message}");
        }
    }

    public async Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct)
    {
        var config = await _configStore.LoadAsync(ct);
        var providerCfg = config.Providers.GetValueOrDefault(Id);
        req.ExtraOptions.TryGetValue("endpoint", out var endpoint);
        endpoint ??= providerCfg?.Endpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = await _secretResolver.ResolveAsync(Id, "endpoint", null, ct);
        }

        var modelName = providerCfg?.Model ?? DefaultModel;
        var apiKey = await _secretResolver.ResolveAsync(Id, "apiKey", null, ct);

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Missing endpoint/apiKey — run: t2i config");
        if (!TryValidateEndpoint(endpoint, out var endpointError))
            throw new ArgumentException(endpointError, nameof(endpoint));
        RejectRetiredModel(modelName);

        var timeoutSeconds = 300;
        if (req.ExtraOptions.TryGetValue("timeout", out var timeout) &&
            int.TryParse(timeout, out var parsed) && parsed > 0)
        {
            timeoutSeconds = parsed;
        }

        var stopwatch = Stopwatch.StartNew();
        using var generator = new GptImage25Generator(
            endpoint,
            apiKey,
            _httpClientFactory.CreateClient(),
            modelName,
            modelName,
            timeoutSeconds);

        progress?.Report(new GenerationProgress(0, 1, "Calling Azure OpenAI API..."));
        var result = await generator.GenerateAsync(req.Prompt, new ImageGenerationOptions
        {
            Width = req.Width > 0 ? req.Width : 1024,
            Height = req.Height > 0 ? req.Height : 1024
        }, ct);
        await result.SaveAsync(req.OutputPath);
        stopwatch.Stop();

        return new GenerationResult(
            req.OutputPath,
            stopwatch.Elapsed,
            result.Width,
            result.Height,
            new Dictionary<string, string>
            {
                ["model"] = modelName,
                ["provider"] = Id,
                ["endpoint"] = endpoint
            });
    }

    private static bool TryValidateEndpoint(string endpoint, out string error)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "Azure OpenAI endpoint must be an absolute HTTPS URL.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static void RejectRetiredModel(string modelName)
    {
        if (modelName.StartsWith("dall-e", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"'{modelName}' is retired. Configure an Azure OpenAI GPT-Image deployment instead.");
        }
    }
}

internal sealed class FoundryGptImage25SunburstAdapter : FoundryGptImage25AdapterBase
{
    public override string Id => "foundry-gpt-image-25-sunburst";
    public override string DisplayName => "GPT-Image-2.5-Sunburst (Azure OpenAI)";
    public override string DefaultModel => "gpt-image-2.5-sunburst";

    public FoundryGptImage25SunburstAdapter(
        IHttpClientFactory httpClientFactory,
        SecretResolver secretResolver,
        ConfigStore configStore)
        : base(httpClientFactory, secretResolver, configStore)
    {
    }
}

internal sealed class FoundryGptImage25FlareAdapter : FoundryGptImage25AdapterBase
{
    public override string Id => "foundry-gpt-image-25-flare";
    public override string DisplayName => "GPT-Image-2.5-Flare (Azure OpenAI)";
    public override string DefaultModel => "gpt-image-2.5-flare";

    public FoundryGptImage25FlareAdapter(
        IHttpClientFactory httpClientFactory,
        SecretResolver secretResolver,
        ConfigStore configStore)
        : base(httpClientFactory, secretResolver, configStore)
    {
    }
}
