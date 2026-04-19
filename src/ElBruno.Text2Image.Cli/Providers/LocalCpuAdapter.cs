using System.Diagnostics;
using ElBruno.Text2Image.Models;

namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Provider adapter for local CPU inference.
/// Uses ElBruno.Text2Image with CPU execution provider.
/// </summary>
internal sealed class LocalCpuAdapter : IProviderAdapter
{
    public string Id => "cpu";
    public string DisplayName => "CPU (Local)";
    public ProviderKind Kind => ProviderKind.Local;
    public IReadOnlyList<string> RequiredSecrets => Array.Empty<string>();

    public Task<ProviderHealth> CheckAsync(CancellationToken ct)
    {
        return Task.FromResult(new ProviderHealth(Ok: true, Reason: null));
    }

    public async Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var generator = new StableDiffusion15(new ImageGenerationOptions
        {
            ExecutionProvider = ExecutionProvider.Cpu,
            Width = req.Width > 0 ? req.Width : 512,
            Height = req.Height > 0 ? req.Height : 512,
            NumInferenceSteps = req.Steps > 0 ? req.Steps : 20
        });

        progress?.Report(new GenerationProgress(0, 1, "Downloading model (first run only)..."));
        await generator.EnsureModelAvailableAsync(cancellationToken: ct);

        progress?.Report(new GenerationProgress(0, 1, "Generating image..."));
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
                ["provider"] = "cpu"
            });
    }
}
