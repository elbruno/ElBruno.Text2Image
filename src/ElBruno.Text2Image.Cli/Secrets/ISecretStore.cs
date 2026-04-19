namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Common interface for secret storage backends.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Name of this store (e.g., "env", "dpapi", "file").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this store is available on the current platform.
    /// (e.g., DPAPI is false on Linux/macOS)
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets a secret value, or null if not found.
    /// </summary>
    Task<string?> GetAsync(string provider, string field, CancellationToken ct);

    /// <summary>
    /// Sets a secret value.
    /// </summary>
    Task SetAsync(string provider, string field, string value, CancellationToken ct);

    /// <summary>
    /// Deletes a secret value.
    /// </summary>
    Task DeleteAsync(string provider, string field, CancellationToken ct);

    /// <summary>
    /// Lists all field names for a provider.
    /// </summary>
    Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct);
}
