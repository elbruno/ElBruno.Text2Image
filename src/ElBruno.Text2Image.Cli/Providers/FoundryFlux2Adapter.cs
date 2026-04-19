using ElBruno.Text2Image.Foundry;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for Microsoft Foundry FLUX.2 cloud API.
/// </summary>
internal sealed class FoundryFlux2Adapter : IProviderAdapter
{
    // TODO(River): implement Foundry FLUX.2 provider wrapping Flux2Generator
    public string Id => "foundry-flux2";
    public string DisplayName => "FLUX.2 Pro (Cloud)";
    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "endpoint", "apiKey" };

    public Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
