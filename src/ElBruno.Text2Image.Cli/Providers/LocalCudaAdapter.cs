namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for local CUDA GPU inference.
/// Uses ElBruno.Text2Image with CUDA execution provider.
/// </summary>
internal sealed class LocalCudaAdapter : IProviderAdapter
{
    // TODO(River): implement CUDA provider wrapping Pipeline/StableDiffusionPipeline with CUDA EP
    public string Id => "cuda";
    public string DisplayName => "CUDA (Local GPU)";
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
