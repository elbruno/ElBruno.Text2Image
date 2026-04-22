#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Config;

namespace ElBruno.Text2Image.Tests.Cli;

[Collection("ConfigStore")]
public class ConfigModelTests : IDisposable
{
    private readonly string _tempConfigDir;
    private readonly TextWriter _originalStdErr;

    public ConfigModelTests()
    {
        _tempConfigDir = Path.Combine(Path.GetTempPath(), $"t2i-test-config-model-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempConfigDir);
        Environment.SetEnvironmentVariable("APPDATA", _tempConfigDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempConfigDir);
        
        // Suppress Console.Error output during tests to prevent warning noise
        _originalStdErr = Console.Error;
        Console.SetError(TextWriter.Null);
    }

    [Fact]
    public async Task ProviderConfig_Model_RoundTrips()
    {
        var store = new ConfigStore();

        var config = new AppConfig
        {
            DefaultProvider = "foundry-mai2",
            Providers =
            {
                ["foundry-mai2"] = new ProviderConfig
                {
                    Endpoint = "https://example.services.ai.azure.com",
                    Model = "MAI-Image-2e"
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("MAI-Image-2e", loaded.Providers["foundry-mai2"].Model);
    }

    [Fact]
    public async Task ConfigStore_SetModel_WritesAndReadsBackCorrectly()
    {
        var store = new ConfigStore();

        var config = new AppConfig
        {
            Providers =
            {
                ["foundry-mai2"] = new ProviderConfig
                {
                    Endpoint = "https://example.services.ai.azure.com",
                    Model = "MAI-Image-2e"
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("MAI-Image-2e", loaded.Providers["foundry-mai2"].Model);
    }

    [Fact]
    public async Task ConfigStore_Flux2Model_RoundTrips()
    {
        var store = new ConfigStore();

        var config = new AppConfig
        {
            Providers =
            {
                ["foundry-flux2"] = new ProviderConfig
                {
                    Endpoint = "https://example.com/api",
                    Model = "FLUX.2-flex"
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("FLUX.2-flex", loaded.Providers["foundry-flux2"].Model);
    }

    [Fact]
    public async Task DefaultModel_IsNull_WhenNotSet()
    {
        var store = new ConfigStore();

        var config = new AppConfig
        {
            Providers =
            {
                ["foundry-mai2"] = new ProviderConfig
                {
                    Endpoint = "https://example.services.ai.azure.com"
                    // Model not set
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Null(loaded.Providers["foundry-mai2"].Model);
    }

    [Fact]
    public async Task MultipleProviders_DifferentModels_RoundTrip()
    {
        var store = new ConfigStore();

        var config = new AppConfig
        {
            Providers =
            {
                ["foundry-mai2"] = new ProviderConfig
                {
                    Endpoint = "https://mai.example.com",
                    Model = "MAI-Image-2e"
                },
                ["foundry-flux2"] = new ProviderConfig
                {
                    Endpoint = "https://flux.example.com",
                    Model = "FLUX.2-flex"
                }
            }
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Equal("MAI-Image-2e", loaded.Providers["foundry-mai2"].Model);
        Assert.Equal("FLUX.2-flex", loaded.Providers["foundry-flux2"].Model);
    }

    public void Dispose()
    {
        Console.SetError(_originalStdErr);
        
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
#endif
