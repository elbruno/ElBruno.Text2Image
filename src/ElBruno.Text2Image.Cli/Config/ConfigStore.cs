using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace ElBruno.Text2Image.Cli.Config;

/// <summary>
/// Persists and loads AppConfig from disk with atomic operations and corruption recovery.
/// </summary>
public sealed class ConfigStore
{
    private static readonly SemaphoreSlim StaticFileLock = new(1, 1);
    private readonly SemaphoreSlim _fileLock = StaticFileLock;
    private readonly string _filePath;
    private readonly ILogger<ConfigStore>? _logger;

    public ConfigStore(ILogger<ConfigStore>? logger = null)
    {
        _filePath = ConfigPaths.ConfigFilePath;
        _logger = logger;
    }

    /// <summary>
    /// Loads configuration from disk. If the config file is corrupted or missing, returns a default config.
    /// Corrupted files are automatically backed up to config.json.bak-{timestamp}.
    /// </summary>
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
            
            try
            {
                return JsonSerializer.Deserialize(json, AppConfigJsonContext.Default.AppConfig)
                       ?? new AppConfig();
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "Config file is corrupted and cannot be parsed: {ConfigPath}", _filePath);
                
                // Rotate corrupted file to backup with timestamp
                var backupPath = $"{_filePath}.bak-{DateTime.UtcNow:yyyyMMddTHHmmssZ}";
                try
                {
                    File.Move(_filePath, backupPath, overwrite: false);
                    _logger?.LogInformation("Corrupted config backed up to: {BackupPath}", backupPath);
                }
                catch (Exception backupEx)
                {
                    _logger?.LogWarning(backupEx, "Failed to backup corrupted config to {BackupPath}", backupPath);
                }
                
                return new AppConfig();
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Saves configuration to disk atomically using a temporary file + rename pattern.
    /// </summary>
    public async Task SaveAsync(AppConfig config, CancellationToken ct)
    {
        // Prevent directory traversal when saving config
        var fullPath = Path.GetFullPath(_filePath);
        var configDir = Path.GetFullPath(ConfigPaths.ConfigDirectory);
        
        if (!fullPath.StartsWith(configDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException(
                $"Config path traversal detected: '{_filePath}' is outside the config directory.");
        }

        await _fileLock.WaitAsync(ct);
        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(config, AppConfigJsonContext.Default.AppConfig);
            var tempPath = fullPath + ".tmp";

            // Clean up any stale temp file
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Ignore if we can't delete stale temp file
                }
            }

            // Write to temp file
            await File.WriteAllTextAsync(tempPath, json, ct);

            try
            {
                // Atomically move temp file to actual config file
                if (File.Exists(fullPath))
                {
                    // On Windows, Replace is more atomic than Move
                    File.Replace(tempPath, fullPath, null);
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }
            }
            catch (Exception ex)
            {
                // If atomic move fails, clean up temp file
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
                
                _logger?.LogError(ex, "Failed to save config file to {Path}", fullPath);
                throw;
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
