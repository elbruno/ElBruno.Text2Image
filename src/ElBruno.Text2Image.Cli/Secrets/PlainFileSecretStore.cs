namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Plaintext secret store (opt-in, with warning).
/// Stores secrets in a JSON file on disk.
/// </summary>
internal sealed class PlainFileSecretStore : ISecretStore
{
    // TODO(Wash): implement plaintext file storage
    public string Name => "file";
    public bool IsAvailable => true;

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
