namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Resolves secrets using the 5-layer chain:
/// 1. CLI flags
/// 2. Environment variables
/// 3. OS native store (DPAPI on Windows)
/// 4. Plaintext config file
/// 5. Interactive wizard (if TTY)
/// </summary>
public sealed class SecretResolver
{
    private readonly IReadOnlyList<ISecretStore> _stores;

    public SecretResolver(IEnumerable<ISecretStore> stores)
    {
        _stores = stores.ToList();
    }

    public async Task<string?> ResolveAsync(
        string provider,
        string field,
        IDictionary<string, string?>? cliOverrides,
        CancellationToken ct)
    {
        if (cliOverrides?.TryGetValue(field, out var overrideValue) == true && overrideValue != null)
        {
            return overrideValue;
        }

        foreach (var store in _stores.Where(s => s.IsAvailable))
        {
            var value = await store.GetAsync(provider, field, ct);
            if (value != null)
            {
                return value;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<(string Store, string Field)>> InspectAsync(string provider, CancellationToken ct)
    {
        var results = new List<(string Store, string Field)>();

        foreach (var store in _stores.Where(s => s.IsAvailable))
        {
            var fields = await store.ListFieldsAsync(provider, ct);
            foreach (var field in fields)
            {
                results.Add((store.Name, field));
            }
        }

        return results;
    }

    public async Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        await SetAsync(provider, field, value, null, ct);
    }

    public async Task SetAsync(string provider, string field, string value, string? preferredStoreName, CancellationToken ct)
    {
        ISecretStore? targetStore;

        if (!string.IsNullOrEmpty(preferredStoreName))
        {
            targetStore = _stores.FirstOrDefault(s => s.Name == preferredStoreName && s.IsAvailable);
            if (targetStore == null)
            {
                throw new InvalidOperationException($"Secret store '{preferredStoreName}' is not available");
            }
        }
        else
        {
            targetStore = _stores.FirstOrDefault(s => s.Name == "dpapi" && s.IsAvailable)
                          ?? _stores.FirstOrDefault(s => s.Name == "file" && s.IsAvailable);

            if (targetStore == null)
            {
                throw new InvalidOperationException("No writable secret store is available");
            }
        }

        await targetStore.SetAsync(provider, field, value, ct);
    }

    public async Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        // Delete from all stores to ensure complete removal
        foreach (var store in _stores.Where(s => s.IsAvailable))
        {
            try
            {
                await store.DeleteAsync(provider, field, ct);
            }
            catch
            {
                // Ignore failures - field might not exist in this store
            }
        }
    }
}
