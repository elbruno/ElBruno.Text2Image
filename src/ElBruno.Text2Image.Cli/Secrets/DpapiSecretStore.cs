namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Secret store using Windows DPAPI (Data Protection API).
/// Only available on Windows.
/// </summary>
internal sealed class DpapiSecretStore : ISecretStore
{
    // TODO(Wash): implement DPAPI encryption/decryption
    public string Name => "dpapi";
    public bool IsAvailable => OperatingSystem.IsWindows();

    public Task<string?> GetAsync(string provider, string field, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
