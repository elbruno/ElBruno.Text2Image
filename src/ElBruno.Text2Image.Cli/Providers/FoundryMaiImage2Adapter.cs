using ElBruno.Text2Image.Foundry;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for Microsoft Foundry MAI-Image-2 cloud API.
/// </summary>
internal sealed class FoundryMaiImage2Adapter : IProviderAdapter
{
    // TODO(River): implement Foundry MAI-Image-2 provider wrapping MaiImage2Generator
    public string Id => "foundry-mai2";
    public string DisplayName => "MAI-Image-2 (Cloud)";
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
