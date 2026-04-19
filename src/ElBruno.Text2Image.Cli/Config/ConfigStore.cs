using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElBruno.Text2Image.Cli.Config;

/// <summary>
/// Persists and loads AppConfig from disk.
/// </summary>
public sealed class ConfigStore
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _filePath;

    public ConfigStore()
    {
        _filePath = ConfigPaths.ConfigFilePath;
    }

    public async Task<AppConfig> LoadAsync(CancellationToken ct)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
            {
                return new AppConfig();
            }

            var json = await File.ReadAllTextAsync(_filePath, ct);
            return JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig)
                   ?? new AppConfig();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAsync(AppConfig config, CancellationToken ct)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
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
        finally
        {
            _fileLock.Release();
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(ProviderConfig))]
[JsonSerializable(typeof(Dictionary<string, ProviderConfig>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class AppConfigJsonContext : JsonSerializerContext
{
}
