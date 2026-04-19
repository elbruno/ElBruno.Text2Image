namespace ElBruno.Text2Image.Cli.Config;

/// <summary>
/// Application configuration POCO.
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// Default provider ID to use when none is specified.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Per-provider configuration.
    /// </summary>
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}

/// <summary>
/// Configuration for a specific provider.
/// </summary>
public sealed class ProviderConfig
{
    /// <summary>
    /// Cloud provider endpoint URL.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Model/deployment name.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Additional provider-specific settings.
    /// </summary>
    public Dictionary<string, string> Extras { get; set; } = new();
}
