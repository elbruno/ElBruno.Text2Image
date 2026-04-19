namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for local DirectML GPU inference (Windows only).
/// Uses ElBruno.Text2Image with DirectML execution provider.
/// </summary>
internal sealed class LocalDirectMlAdapter : IProviderAdapter
{
    // TODO(River): implement DirectML provider wrapping Pipeline/StableDiffusionPipeline with DirectML EP
    public string Id => "directml";
    public string DisplayName => "DirectML (Local GPU)";
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
