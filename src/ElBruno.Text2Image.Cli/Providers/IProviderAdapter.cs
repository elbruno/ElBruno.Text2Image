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
    /// List of required secret field names (e.g., ["apiKey"]).
    /// Empty for local providers.
    /// </summary>
    IReadOnlyList<string> RequiredSecrets { get; }

    /// <summary>
    /// List of required config field names (e.g., ["endpoint", "model"]).
    /// Empty for local providers. Default implementation returns empty array.
    /// </summary>
    IReadOnlyList<string> RequiredFields => Array.Empty<string>();

    /// <summary>
    /// Default model/deployment name used when none is configured.
    /// Null for local providers. Cloud providers return their canonical default.
    /// </summary>
    string? DefaultModel => null;

    /// <summary>
    /// Reference-image (image-to-image / edit) capabilities. Null when the provider
    /// does not accept reference images.
    /// </summary>
    ReferenceImageCapabilities? ReferenceImageSupport => null;

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
/// Describes how a provider accepts reference images.
/// </summary>
/// <param name="MaxImages">Maximum number of reference images per request.</param>
/// <param name="SupportsMask">Whether a PNG inpainting mask is supported.</param>
/// <param name="SupportsInputFidelity">Whether the GPT-Image <c>input_fidelity</c> option is supported.</param>
/// <param name="AllowedMediaTypes">Accepted image media types (e.g. image/png, image/jpeg).</param>
public sealed record ReferenceImageCapabilities(
    int MaxImages,
    bool SupportsMask,
    bool SupportsInputFidelity,
    IReadOnlyList<string> AllowedMediaTypes)
{
    internal static readonly IReadOnlyList<string> PngJpegWebp = ["image/png", "image/jpeg", "image/webp"];
    internal static readonly IReadOnlyList<string> PngJpeg = ["image/png", "image/jpeg"];

    /// <summary>Short human-readable summary, e.g. "up to 16 + mask".</summary>
    public string Summary => $"up to {MaxImages}{(SupportsMask ? " + mask" : string.Empty)}";
}

/// <summary>
/// Request for image generation.
/// </summary>
public sealed record GenerationRequest(
    string Prompt,
    int Width,
    int Height,
    int Steps,
    string OutputPath,
    IReadOnlyDictionary<string, string?> ExtraOptions,
    IReadOnlyList<string>? ReferenceImages = null,
    string? MaskImage = null,
    string? InputFidelity = null)
{
    /// <summary>
    /// Copies reference image, mask and input fidelity values onto library generation options.
    /// </summary>
    internal ImageGenerationOptions WithReferenceInputs(ImageGenerationOptions options)
    {
        if (ReferenceImages is { Count: > 0 })
            options.ReferenceImages = [.. ReferenceImages];
        if (!string.IsNullOrWhiteSpace(MaskImage))
            options.MaskImage = MaskImage;
        if (!string.IsNullOrWhiteSpace(InputFidelity))
            options.InputFidelity = InputFidelity;
        return options;
    }
}

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
