namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Common interface for all provider adapters (local and cloud).
/// Provides a unified abstraction over different image generation backends.
/// </summary>
public interface IProviderAdapter
{
    /// <summary>
    /// Unique provider identifier (e.g., "cpu", "cuda", "foundry-flux2").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable display name for UI (e.g., "CPU (Local)", "FLUX.2 Pro (Cloud)").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Provider category.
    /// </summary>
    ProviderKind Kind { get; }

    /// <summary>
    /// List of required secret field names (e.g., ["endpoint", "apiKey"]).
    /// Empty for local providers.
    /// </summary>
    IReadOnlyList<string> RequiredSecrets { get; }

    /// <summary>
    /// Checks if the provider is ready to use (e.g., GPU available, API reachable).
    /// </summary>
    Task<ProviderHealth> CheckAsync(CancellationToken ct);

    /// <summary>
    /// Generates an image from a prompt using this provider.
    /// </summary>
    Task<GenerationResult> GenerateAsync(
        GenerationRequest req,
        IProgress<GenerationProgress>? progress,
        CancellationToken ct);
}

/// <summary>
/// Provider category.
/// </summary>
public enum ProviderKind
{
    Local,
    Cloud
}

/// <summary>
/// Result of a provider health check.
/// </summary>
public sealed record ProviderHealth(bool Ok, string? Reason);

/// <summary>
/// Request for image generation.
/// </summary>
public sealed record GenerationRequest(
    string Prompt,
    int Width,
    int Height,
    int Steps,
    string OutputPath,
    IReadOnlyDictionary<string, string?> ExtraOptions);

/// <summary>
/// Progress update during image generation.
/// </summary>
public sealed record GenerationProgress(int Step, int TotalSteps, string? Message);

/// <summary>
/// Result of image generation.
/// </summary>
public sealed record GenerationResult(
    string OutputPath,
    TimeSpan Duration,
    int ActualWidth,
    int ActualHeight,
    IReadOnlyDictionary<string, string> Metadata);
