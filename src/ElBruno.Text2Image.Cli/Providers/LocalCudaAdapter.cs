using System.Diagnostics;
using ElBruno.Text2Image.Models;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for local CUDA GPU inference.
/// Uses ElBruno.Text2Image with CUDA execution provider.
/// </summary>
internal sealed class LocalCudaAdapter : IProviderAdapter
{
    public string Id => "cuda";
    public string DisplayName => "CUDA (Local GPU)";
    public ProviderKind Kind => ProviderKind.Local;
    public IReadOnlyList<string> RequiredSecrets => Array.Empty<string>();

    public Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        try
        {
            var resolved = SessionOptionsHelper.DetectBestProvider();
            if (resolved == ExecutionProvider.Cuda)
            {
                using var _ = SessionOptionsHelper.Create(ExecutionProvider.Cuda);
                return Task.FromResult(new ProviderHealth(Ok: true, Reason: null));
            }
            
            return Task.FromResult(new ProviderHealth(
                Ok: false, 
                Reason: "CUDA runtime not detected (install CUDA 12.x and required libraries)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ProviderHealth(
                Ok: false, 
                Reason: $"CUDA initialization failed: {ex.Message}"));
        }
    }

    public async Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct)
    {
        var health = await CheckAsync(ct);
        if (!health.Ok)
        {
            throw new InvalidOperationException(
                $"CUDA not available — see `t2i doctor`. {health.Reason}");
        }

        var sw = Stopwatch.StartNew();

        using var generator = new StableDiffusion15(new ImageGenerationOptions
        {
            ExecutionProvider = ExecutionProvider.Cuda,
            Width = req.Width > 0 ? req.Width : 512,
            Height = req.Height > 0 ? req.Height : 512,
            NumInferenceSteps = req.Steps > 0 ? req.Steps : 20
        });

        progress?.Report(new GenerationProgress(0, 1, "Downloading model (first run only)..."));
        await generator.EnsureModelAvailableAsync(cancellationToken: ct);

        progress?.Report(new GenerationProgress(0, 1, "Generating image with CUDA..."));
        var result = await generator.GenerateAsync(req.Prompt, cancellationToken: ct);

        await result.SaveAsync(req.OutputPath);
        sw.Stop();

        return new GenerationResult(
            OutputPath: req.OutputPath,
            Duration: sw.Elapsed,
            ActualWidth: result.Width,
            ActualHeight: result.Height,
            Metadata: new Dictionary<string, string>
            {
                ["model"] = "Stable Diffusion 1.5",
                ["seed"] = result.Seed.ToString(),
                ["provider"] = "cuda"
            });
    }
}
