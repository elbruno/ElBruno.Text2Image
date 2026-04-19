namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Central registry for all available provider adapters.
/// </summary>
public sealed class ProviderRegistry
{
    private readonly Dictionary<string, IProviderAdapter> _providers;

    public ProviderRegistry(IEnumerable<IProviderAdapter> adapters)
    {
        _providers = adapters.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// All registered providers.
    /// </summary>
    public IEnumerable<IProviderAdapter> All => _providers.Values;

    /// <summary>
    /// Gets a provider by ID (case-insensitive), or null if not found.
    /// </summary>
    public IProviderAdapter? Get(string id) => _providers.GetValueOrDefault(id);
}
