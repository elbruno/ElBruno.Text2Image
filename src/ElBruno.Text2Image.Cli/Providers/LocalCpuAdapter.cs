namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for local CPU inference.
/// Uses ElBruno.Text2Image with CPU execution provider.
/// </summary>
internal sealed class LocalCpuAdapter : IProviderAdapter
{
    // TODO(River): implement CPU provider wrapping Pipeline/StableDiffusionPipeline with CPU EP
    public string Id => "cpu";
    public string DisplayName => "CPU (Local)";
    public ProviderKind Kind => ProviderKind.Local;
    public IReadOnlyList<string> RequiredSecrets => Array.Empty<string>();

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
