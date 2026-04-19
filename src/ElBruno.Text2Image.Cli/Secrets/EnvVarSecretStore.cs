namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Secret store that reads from environment variables.
/// Format: T2I_<PROVIDER>_<FIELD> (e.g., T2I_FOUNDRY_FLUX2_APIKEY)
/// </summary>
internal sealed class EnvVarSecretStore : ISecretStore
{
    // TODO(Wash): implement environment variable reading
    public string Name => "env";
    public bool IsAvailable => true;

    public Task<string?> GetAsync(string provider, string field, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        throw new NotSupportedException("Environment variables cannot be set at runtime");
    }

    public Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        throw new NotSupportedException("Environment variables cannot be deleted at runtime");
    }

    public Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
