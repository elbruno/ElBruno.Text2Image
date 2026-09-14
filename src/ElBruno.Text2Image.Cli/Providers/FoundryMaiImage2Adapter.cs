using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;
using Microsoft.Extensions.Http;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Compatibility entry for the retired MAI-Image-2 provider.
/// </summary>
internal sealed class FoundryMaiImage2Adapter : IProviderAdapter
{
    public string Id => "foundry-mai2";
    public string DisplayName => "MAI-Image-2 (retired)";
    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
    public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "model" };
    public string DefaultModel => "MAI-Image-2";

    public FoundryMaiImage2Adapter(
        IHttpClientFactory httpClientFactory,
        SecretResolver secretResolver,
        ConfigStore configStore)
    {
    }

    public Task<ProviderHealth> CheckAsync(CancellationToken ct) =>
        Task.FromResult(new ProviderHealth(
            false,
            "MAI-Image-2 and MAI-Image-2e are retired. Migrate to foundry-mai25 or foundry-mai25-flash."));

    public Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct) =>
        throw new NotSupportedException(
            "MAI-Image-2 and MAI-Image-2e are retired and are not used as a fallback. " +
            "Migrate to the foundry-mai25 or foundry-mai25-flash provider.");
}
