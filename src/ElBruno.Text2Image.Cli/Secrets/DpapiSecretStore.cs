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
    private readonly string _configDir;

    public DpapiSecretStore()
    {
        var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _configDir = Path.Combine(localAppData, "t2i");
        _filePath = Path.Combine(_configDir, "secrets.dpapi");
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

    [SupportedOSPlatform("windows")]
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

    [SupportedOSPlatform("windows")]
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
        // Prevent directory traversal when saving secrets
        var fullPath = Path.GetFullPath(_filePath);
        var dir = Path.GetDirectoryName(fullPath);
        
        // Validate path is in expected location
        var expectedDir = Path.GetFullPath(_configDir);
        
        if (dir != null && !fullPath.StartsWith(expectedDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"DPAPI secret store path traversal detected: '{_filePath}' is outside the expected directory.");
        }

        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(store, DpapiStoreJsonContext.Default.DictionaryStringByteArray);
        var tempPath = fullPath + ".tmp";

        await File.WriteAllTextAsync(tempPath, json, ct);

        if (File.Exists(fullPath))
        {
            File.Replace(tempPath, fullPath, null);
        }
        else
        {
            File.Move(tempPath, fullPath);
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
