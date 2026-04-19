#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Config;

namespace ElBruno.Text2Image.Tests.Cli;

[Collection("ConfigStore")]
public class ConfigStoreTests : IDisposable
{
    private readonly string _tempConfigDir;

    public ConfigStoreTests()
    {
        _tempConfigDir = Path.Combine(Path.GetTempPath(), $"t2i-test-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempConfigDir);
        Environment.SetEnvironmentVariable("APPDATA", _tempConfigDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempConfigDir);
    }

    [Fact]
    public async Task Save_Then_Load_RoundTrip_PreservesAllFields()
    {
        var store = new ConfigStore();

        var config = new AppConfig
        {
            DefaultProvider = "foundry-flux2",
            Providers =
            {
                ["foundry-flux2"] = new ProviderConfig
                {
                    Endpoint = "https://example.com",
                    Model = "flux-2-pro",
                    Extras = { ["timeout"] = "30" }
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("foundry-flux2", loaded.DefaultProvider);
        Assert.Single(loaded.Providers);
        Assert.Equal("https://example.com", loaded.Providers["foundry-flux2"].Endpoint);
        Assert.Equal("flux-2-pro", loaded.Providers["foundry-flux2"].Model);
        Assert.Equal("30", loaded.Providers["foundry-flux2"].Extras["timeout"]);
    }

    [Fact]
    public async Task Load_ReturnsDefault_WhenFileMissing()
    {
        var tempConfigDir2 = Path.Combine(Path.GetTempPath(), $"t2i-test-loadmissing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempConfigDir2);
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        var originalXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        
        try
        {
            Environment.SetEnvironmentVariable("APPDATA", tempConfigDir2);
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", tempConfigDir2);

            var configFile = ConfigPaths.ConfigFilePath;
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }

            var store = new ConfigStore();
            var config = await store.LoadAsync(CancellationToken.None);

            Assert.NotNull(config);
            Assert.Null(config.DefaultProvider);
            Assert.Empty(config.Providers);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", originalXdgConfig);
            if (Directory.Exists(tempConfigDir2))
            {
                try
                {
                    Directory.Delete(tempConfigDir2, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public async Task Save_CreatesParentDirectory_IfMissing()
    {
        var store = new ConfigStore();

        var config = new AppConfig { DefaultProvider = "cpu" };
        await store.SaveAsync(config, CancellationToken.None);

        Assert.True(File.Exists(ConfigPaths.ConfigFilePath));
    }

    [Fact]
    public async Task Save_IsAtomic()
    {
        var store = new ConfigStore();

        var config = new AppConfig { DefaultProvider = "cuda" };
        await store.SaveAsync(config, CancellationToken.None);

        var configDir = Path.GetDirectoryName(ConfigPaths.ConfigFilePath);
        var tmpFiles = Directory.GetFiles(configDir!, "*.tmp");
        Assert.Empty(tmpFiles);

        Assert.True(File.Exists(ConfigPaths.ConfigFilePath));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("APPDATA", null);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", null);
        
        if (Directory.Exists(_tempConfigDir))
        {
            try
            {
                Thread.Sleep(100);
                Directory.Delete(_tempConfigDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}

[CollectionDefinition("ConfigStore")]
public class ConfigStoreCollection : ICollectionFixture<object>
{
}
#endif
