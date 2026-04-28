#if NET10_0_OR_GREATER
using System.Text.Json;
using Xunit;
using ElBruno.Text2Image.Cli.Config;

namespace ElBruno.Text2Image.Tests.Cli.Config;

/// <summary>
/// Config validation tests for schema validation, migration, and roundtrip.
/// Tests config file format validation and backward compatibility.
/// Uses isolated temp directories to prevent pollution of actual user config.
/// </summary>
[Collection("ConfigValidation")]
public class ConfigValidationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalAppData;
    private readonly string _originalXdgConfig;

    public ConfigValidationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"t2i-config-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? string.Empty;
        _originalXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
        
        // Set environment variables to point to temp directory for this test
        Environment.SetEnvironmentVariable("APPDATA", _tempDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("APPDATA", _originalAppData);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _originalXdgConfig);
        
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region Schema Validation Tests

    [Fact]
    public async Task ConfigStore_LoadsValidConfig()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            DefaultProvider = "foundry-flux2",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["foundry-flux2"] = new ProviderConfig
                {
                    Endpoint = "https://test.example.com",
                    Model = "flux-2-pro"
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("foundry-flux2", loaded.DefaultProvider);
        Assert.Single(loaded.Providers);
        Assert.Equal("https://test.example.com", loaded.Providers["foundry-flux2"].Endpoint);
        Assert.Equal("flux-2-pro", loaded.Providers["foundry-flux2"].Model);
    }

    [Fact]
    public async Task ConfigStore_HandlesNullDefaultProvider()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            DefaultProvider = null,
            Providers = new Dictionary<string, ProviderConfig>()
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Null(loaded.DefaultProvider);
    }

    [Fact]
    public async Task ConfigStore_HandlesEmptyProviders()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            DefaultProvider = "foundry-flux2",
            Providers = new Dictionary<string, ProviderConfig>()
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Empty(loaded.Providers);
    }

    [Fact]
    public async Task ProviderConfig_AllowsNullEndpoint()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test-provider"] = new ProviderConfig
                {
                    Endpoint = null,
                    Model = "test-model"
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Null(loaded.Providers["test-provider"].Endpoint);
    }

    [Fact]
    public async Task ProviderConfig_AllowsNullModel()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test-provider"] = new ProviderConfig
                {
                    Endpoint = "https://test.example.com",
                    Model = null
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Null(loaded.Providers["test-provider"].Model);
    }

    #endregion

    #region Config Roundtrip Tests

    [Fact]
    public async Task ConfigStore_Roundtrip_PreservesAllFields()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            DefaultProvider = "foundry-mai2",
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["foundry-flux2"] = new ProviderConfig
                {
                    Endpoint = "https://flux.example.com",
                    Model = "flux-2-pro",
                    Extras = new Dictionary<string, string>
                    {
                        ["customParam"] = "value1"
                    }
                },
                ["foundry-mai2"] = new ProviderConfig
                {
                    Endpoint = "https://mai.example.com",
                    Model = "MAI-Image-2",
                    Extras = new Dictionary<string, string>
                    {
                        ["timeout"] = "30"
                    }
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("foundry-mai2", loaded.DefaultProvider);
        Assert.Equal(2, loaded.Providers.Count);
        Assert.Equal("https://flux.example.com", loaded.Providers["foundry-flux2"].Endpoint);
        Assert.Equal("flux-2-pro", loaded.Providers["foundry-flux2"].Model);
        Assert.Equal("value1", loaded.Providers["foundry-flux2"].Extras["customParam"]);
        Assert.Equal("https://mai.example.com", loaded.Providers["foundry-mai2"].Endpoint);
        Assert.Equal("MAI-Image-2", loaded.Providers["foundry-mai2"].Model);
        Assert.Equal("30", loaded.Providers["foundry-mai2"].Extras["timeout"]);
    }

    [Fact]
    public async Task ConfigStore_Roundtrip_HandlesEmptyExtras()
    {
        var store = new ConfigStore();
        var config = new AppConfig
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["test-provider"] = new ProviderConfig
                {
                    Endpoint = "https://test.example.com",
                    Model = "test-model",
                    Extras = new Dictionary<string, string>()
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Empty(loaded.Providers["test-provider"].Extras);
    }

    #endregion

    #region Invalid Config Handling Tests

    [Fact]
    public async Task ConfigStore_LoadAsync_ReturnsDefault_WhenFileDoesNotExist()
    {
        var store = new ConfigStore();
        
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Null(loaded.DefaultProvider);
        Assert.Empty(loaded.Providers);
    }

    [Fact]
    public async Task ConfigStore_LoadAsync_RecorversFromCorruptedJson_AndBacksItUp()
    {
        var store = new ConfigStore();
        var configPath = ConfigPaths.ConfigFilePath;
        var dir = Path.GetDirectoryName(configPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Write invalid JSON to config file
        await File.WriteAllTextAsync(configPath, "{ invalid json }", CancellationToken.None);
        
        // Load should NOT throw - should return default config and backup the corrupted file
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Null(loaded.DefaultProvider);
        Assert.Empty(loaded.Providers);
        
        // Verify the corrupted file was backed up
        if (dir != null)
        {
            var backupFiles = Directory.GetFiles(dir, "config.json.bak-*");
            Assert.NotEmpty(backupFiles);
            
            // Verify the backup file contains the corrupted content
            var backupContent = await File.ReadAllTextAsync(backupFiles[0]);
            Assert.Equal("{ invalid json }", backupContent);
        }
        
        // Verify the original config file was removed
        Assert.False(File.Exists(configPath));
    }

    [Fact]
    public async Task ConfigStore_LoadAsync_HandlesEmptyConfigFile()
    {
        var store = new ConfigStore();
        var configPath = ConfigPaths.ConfigFilePath;
        var dir = Path.GetDirectoryName(configPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Write empty file
        await File.WriteAllTextAsync(configPath, "", CancellationToken.None);
        
        // Load should recover gracefully
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Null(loaded.DefaultProvider);
        Assert.Empty(loaded.Providers);
    }

    [Fact]
    public async Task ConfigStore_LoadAsync_HandlesMalformedJsonWithValidStructure()
    {
        var store = new ConfigStore();
        var configPath = ConfigPaths.ConfigFilePath;
        var dir = Path.GetDirectoryName(configPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Write JSON that looks valid but has syntax errors
        await File.WriteAllTextAsync(configPath, "{\"defaultProvider\": \"flux2\", invalid}", CancellationToken.None);
        
        // Load should recover gracefully
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        // Backup should exist
        if (dir != null)
        {
            var backupFiles = Directory.GetFiles(dir, "config.json.bak-*");
            Assert.NotEmpty(backupFiles);
        }
    }

    #endregion
}

[CollectionDefinition("ConfigValidation")]
public class ConfigValidationCollection : ICollectionFixture<object>
{
}
#endif
