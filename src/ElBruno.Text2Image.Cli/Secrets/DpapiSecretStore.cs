using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Secret store using Windows DPAPI (Data Protection API).
/// Only available on Windows.
/// </summary>
internal sealed class DpapiSecretStore : ISecretStore
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;

    public DpapiSecretStore()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDir = Path.Combine(localAppData, "t2i");
        _filePath = Path.Combine(configDir, "secrets.dpapi");
    }

    public string Name => "dpapi";
    public bool IsAvailable => OperatingSystem.IsWindows();

    [SupportedOSPlatform("windows")]
    public async Task<string?> GetAsync(string provider, string field, CancellationToken ct)
    {
        ThrowIfNotWindows();

        await _fileLock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return null;

            var store = await LoadStoreAsync(ct);
            var key = BuildKey(provider, field);

            if (!store.TryGetValue(key, out var encryptedBytes))
                return null;

            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    [SupportedOSPlatform("windows")]
    public async Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        ThrowIfNotWindows();

        await _fileLock.WaitAsync(ct);
        try
        {
            var store = File.Exists(_filePath) ? await LoadStoreAsync(ct) : new Dictionary<string, byte[]>();
            var key = BuildKey(provider, field);

            var valueBytes = Encoding.UTF8.GetBytes(value);
            var encryptedBytes = ProtectedData.Protect(valueBytes, null, DataProtectionScope.CurrentUser);

            store[key] = encryptedBytes;
            await SaveStoreAsync(store, ct);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        ThrowIfNotWindows();

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
        ThrowIfNotWindows();

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

    [SupportedOSPlatform("windows")]
    private async Task<Dictionary<string, byte[]>> LoadStoreAsync(CancellationToken ct)
    {
        var json = await File.ReadAllTextAsync(_filePath, ct);
        return JsonSerializer.Deserialize(json, DpapiStoreJsonContext.Default.DictionaryStringByteArray)
               ?? new Dictionary<string, byte[]>();
    }

    [SupportedOSPlatform("windows")]
    private async Task SaveStoreAsync(Dictionary<string, byte[]> store, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(store, DpapiStoreJsonContext.Default.DictionaryStringByteArray);
        var tempPath = _filePath + ".tmp";

        await File.WriteAllTextAsync(tempPath, json, ct);

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

    private static void ThrowIfNotWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI secret store is only available on Windows");
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, byte[]>))]
internal partial class DpapiStoreJsonContext : JsonSerializerContext
{
}
