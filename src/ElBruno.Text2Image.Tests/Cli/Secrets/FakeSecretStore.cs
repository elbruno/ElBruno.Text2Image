#if NET10_0_OR_GREATER
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Cli.Secrets;

public sealed class FakeSecretStore : ISecretStore
{
    public string Name { get; set; } = "fake";
    public bool IsAvailable { get; set; } = true;

    public Dictionary<(string Provider, string Field), string> Data { get; } = new();

    public Task<string?> GetAsync(string provider, string field, CancellationToken ct)
    {
        if (!IsAvailable)
            return Task.FromResult<string?>(null);

        Data.TryGetValue((provider, field), out var value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Store is not available");

        Data[(provider, field)] = value;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Store is not available");

        Data.Remove((provider, field));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct)
    {
        if (!IsAvailable)
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var fields = Data.Keys
            .Where(k => k.Provider == provider)
            .Select(k => k.Field)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(fields);
    }
}
#endif
