namespace ElBruno.Text2Image.Cli.Providers;

/// <summary>
/// Central registry for all available provider adapters.
/// </summary>
public sealed class ProviderRegistry
{
    private readonly Dictionary<string, IProviderAdapter> _providers = new();

    public ProviderRegistry(IEnumerable<IProviderAdapter> adapters)
    {
        foreach (var adapter in adapters)
        {
            _providers[adapter.Id] = adapter;
        }
    }

    /// <summary>
    /// All registered providers.
    /// </summary>
    public IEnumerable<IProviderAdapter> All => _providers.Values;

    /// <summary>
    /// Gets a provider by ID, or null if not found.
    /// </summary>
    public IProviderAdapter? Get(string id) => _providers.GetValueOrDefault(id);
}
