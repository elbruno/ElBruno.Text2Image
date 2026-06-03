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
    private readonly string _configDirectory;
    private bool _hasWarnedOnce;
    private bool _hasWarnedOnStartup;

    public PlainFileSecretStore()
    {
        _configDirectory = ConfigPaths.ConfigDirectory;
        _filePath = Path.Combine(_configDirectory, "secrets.json");
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

            WarnOnStartup();
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
        WarnOnWrite();

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
        // Prevent directory traversal when saving secrets
        var fullPath = Path.GetFullPath(_filePath);
        var configDir = Path.GetFullPath(_configDirectory);
        
        if (!fullPath.StartsWith(configDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Secret store path traversal detected: '{_filePath}' is outside the config directory.");
        }

        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(store, PlainFileStoreJsonContext.Default.DictionaryStringString);
        var tempPath = fullPath + ".tmp";

        await File.WriteAllTextAsync(tempPath, json, ct);

        if (!OperatingSystem.IsWindows())
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }

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

    private void WarnOnStartup()
    {
        if (!_hasWarnedOnStartup && File.Exists(_filePath))
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.Error.WriteLine("║                 ⚠ SECURITY WARNING ⚠                        ║");
            Console.Error.WriteLine("║                                                              ║");
            Console.Error.WriteLine("║  Secrets are stored in PLAINTEXT on disk                     ║");
            Console.Error.WriteLine("║  Location: ~/.config/t2i/secrets.json                        ║");
            Console.Error.WriteLine("║                                                              ║");
            Console.Error.WriteLine("║  Recommended: Use DPAPI on Windows for encrypted storage     ║");
            Console.Error.WriteLine("║  Command: t2i secrets set <provider> --store dpapi           ║");
            Console.Error.WriteLine("║                                                              ║");
            Console.Error.WriteLine("║  Or use environment variables for CI/CD:                     ║");
            Console.Error.WriteLine("║  T2I_<PROVIDER>_APIKEY=your-key                              ║");
            Console.Error.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.Error.WriteLine();
            _hasWarnedOnStartup = true;
        }
    }

    private void WarnOnWrite()
    {
        if (!_hasWarnedOnce)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("⚠ WARNING: Saving secret in PLAINTEXT to disk");
            Console.Error.WriteLine("  Location: " + _filePath);
            Console.Error.WriteLine("  This file is NOT encrypted and may be readable by other users.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("  For better security on Windows, use:");
            Console.Error.WriteLine("    t2i secrets set <provider> --store dpapi");
            Console.Error.WriteLine();
            _hasWarnedOnce = true;
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class PlainFileStoreJsonContext : JsonSerializerContext
{
}
