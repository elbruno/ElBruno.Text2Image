using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElBruno.Text2Image.Cli.Config;

namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Plaintext secret store (opt-in, with warning).
/// Stores secrets in a JSON file on disk.
/// </summary>
internal sealed class PlainFileSecretStore : ISecretStore
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;
    private bool _hasWarnedOnce;

    public PlainFileSecretStore()
    {
        _filePath = Path.Combine(ConfigPaths.ConfigDirectory, "secrets.json");
    }

    public string Name => "file";
    public bool IsAvailable => true;

    public async Task<string?> GetAsync(string provider, string field, CancellationToken ct)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var store = await LoadStoreAsync(ct);
            var key = BuildKey(provider, field);

            return store.TryGetValue(key, out var value) ? value : null;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        WarnOnFirstUse();

        await _fileLock.WaitAsync(ct);
        try
        {
            var store = File.Exists(_filePath) ? await LoadStoreAsync(ct) : new Dictionary<string, string>();
            var key = BuildKey(provider, field);

            store[key] = value;
            await SaveStoreAsync(store, ct);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return;

            var store = await LoadStoreAsync(ct);
            var key = BuildKey(provider, field);

            if (store.Remove(key))
            {
                await SaveStoreAsync(store, ct);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return Array.Empty<string>();

            var store = await LoadStoreAsync(ct);
            var providerPrefix = $"{provider}::";

            var fields = store.Keys
                .Where(k => k.StartsWith(providerPrefix, StringComparison.Ordinal))
                .Select(k => k.Substring(providerPrefix.Length))
                .ToList();

            return fields;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<Dictionary<string, string>> LoadStoreAsync(CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(_filePath, ct);
        return JsonSerializer.Deserialize(json, PlainFileStoreJsonContext.Default.DictionaryStringString)
               ?? new Dictionary<string, string>();
    }

    private async Task SaveStoreAsync(Dictionary<string, string> store, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(store, PlainFileStoreJsonContext.Default.DictionaryStringString);
        var tempPath = _filePath + ".tmp";

        await File.WriteAllTextAsync(tempPath, json, ct);

        if (!OperatingSystem.IsWindows())
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }

        if (File.Exists(_filePath))
        {
            File.Replace(tempPath, _filePath, null);
        }
        else
        {
            File.Move(tempPath, _filePath);
        }
    }

    private static string BuildKey(string provider, string field) => $"{provider}::{field}";

    private void WarnOnFirstUse()
    {
        if (!_hasWarnedOnce)
        {
            Console.Error.WriteLine("⚠ Plaintext secrets store — consider using DPAPI on Windows");
            _hasWarnedOnce = true;
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class PlainFileStoreJsonContext : JsonSerializerContext
{
}
