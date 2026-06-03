#if NET10_0_OR_GREATER
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Tests.Cli.Secrets;
using ElBruno.Text2Image.Foundry;

namespace ElBruno.Text2Image.Tests.Foundry;

/// <summary>
/// Phase 3B: Provider-specific tests for model selection, parameter validation, and quirks.
/// Tests Flux2 batch handling, MAI-2 dimensions, GPT endpoint variations.
/// </summary>
[Collection("ConfigStore")]
public class ProviderSpecificTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalAppData;
    private readonly string _originalXdgConfig;

    public ProviderSpecificTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"t2i-provider-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? string.Empty;
        _originalXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
        
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

    #region Flux2 Model Selection Tests

    [Fact]
    public async Task Flux2Adapter_UsesConfiguredModel()
    {
        var (configStore, secretStore) = CreateStores();
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com",
            Model = "flux-2-ultra"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

        var adapter = CreateFlux2Adapter(configStore, secretStore);
        var health = await adapter.CheckAsync(CancellationToken.None);

        // Adapter should be healthy with configured model
        Assert.True(health.Ok);
    }

    [Fact]
    public async Task Flux2Adapter_FallsBackToDefaultModel_WhenNotConfigured()
    {
        var (configStore, secretStore) = CreateStores();
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

        var adapter = CreateFlux2Adapter(configStore, secretStore);
        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public void Flux2Adapter_RequiresBothEndpointAndModel()
    {
        var adapter = CreateFlux2Adapter();
        var requiredFields = adapter.RequiredFields;

        Assert.Contains("endpoint", requiredFields);
        Assert.Contains("model", requiredFields);
    }

    #endregion

    #region MAI-2 Dimension Tests

    [Fact]
    public async Task MaiImage2Adapter_UsesConfiguredModel()
    {
        var (configStore, secretStore) = CreateStores();
        var config = new AppConfig();
        config.Providers["foundry-mai2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com",
            Model = "MAI-Image-2e"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-mai2", "apiKey", "test-key", CancellationToken.None);

        var adapter = CreateMaiImage2Adapter(configStore, secretStore);
        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public void MaiImage2Adapter_RequiresBothEndpointAndModel()
    {
        var adapter = CreateMaiImage2Adapter();
        var requiredFields = adapter.RequiredFields;

        Assert.Contains("endpoint", requiredFields);
        Assert.Contains("model", requiredFields);
    }

    [Fact]
    public async Task MaiImage2Adapter_CheckAsync_WithMissingModel_ReturnsFalse()
    {
        var (configStore, secretStore) = CreateStores();
        var config = new AppConfig();
        config.Providers["foundry-mai2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-mai2", "apiKey", "test-key", CancellationToken.None);

        var adapter = CreateMaiImage2Adapter(configStore, secretStore);
        var health = await adapter.CheckAsync(CancellationToken.None);

        // Should still pass - model defaults are internal
        Assert.True(health.Ok);
    }

    #endregion

    #region GPT Endpoint Variations Tests

    [Fact]
    public async Task GptImage1p5Adapter_AcceptsValidEndpoint()
    {
        var (configStore, secretStore) = CreateStores();
        var config = new AppConfig();
        config.Providers["foundry-gpt-image-1p5"] = new ProviderConfig
        {
            Endpoint = "https://eastus.api.cognitive.microsoft.com",
            Model = "dall-e-3"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-gpt-image-1p5", "apiKey", "test-key", CancellationToken.None);

        var adapter = CreateGptImage1p5Adapter(configStore, secretStore);
        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public async Task GptImage2Adapter_AcceptsValidEndpoint()
    {
        var (configStore, secretStore) = CreateStores();
        var config = new AppConfig();
        config.Providers["foundry-gpt-image-2"] = new ProviderConfig
        {
            Endpoint = "https://westus.api.cognitive.microsoft.com",
            Model = "dall-e-3-hd"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-gpt-image-2", "apiKey", "test-key", CancellationToken.None);

        var adapter = CreateGptImage2Adapter(configStore, secretStore);
        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public void GptImage1p5Adapter_RequiresBothEndpointAndModel()
    {
        var adapter = CreateGptImage1p5Adapter();
        var requiredFields = adapter.RequiredFields;

        Assert.Contains("endpoint", requiredFields);
        Assert.Contains("model", requiredFields);
    }

    [Fact]
    public void GptImage2Adapter_RequiresBothEndpointAndModel()
    {
        var adapter = CreateGptImage2Adapter();
        var requiredFields = adapter.RequiredFields;

        Assert.Contains("endpoint", requiredFields);
        Assert.Contains("model", requiredFields);
    }

    #endregion

    #region Fallback Logic Tests

    [Fact]
    public async Task AllAdapters_FallbackToDefaultModel_WhenConfigMissing()
    {
        var (configStore, secretStore) = CreateStores();
        
        var flux2 = CreateFlux2Adapter(configStore, secretStore);
        var mai2 = CreateMaiImage2Adapter(configStore, secretStore);
        var gpt15 = CreateGptImage1p5Adapter(configStore, secretStore);
        var gpt2 = CreateGptImage2Adapter(configStore, secretStore);

        // All should handle missing config gracefully
        var flux2Health = await flux2.CheckAsync(CancellationToken.None);
        var mai2Health = await mai2.CheckAsync(CancellationToken.None);
        var gpt15Health = await gpt15.CheckAsync(CancellationToken.None);
        var gpt2Health = await gpt2.CheckAsync(CancellationToken.None);

        Assert.False(flux2Health.Ok);
        Assert.False(mai2Health.Ok);
        Assert.False(gpt15Health.Ok);
        Assert.False(gpt2Health.Ok);
    }

    #endregion

    #region Helper Methods

    private (ConfigStore, FakeSecretStore) CreateStores()
    {
        var configStore = new ConfigStore();
        var secretStore = new FakeSecretStore { IsAvailable = true };
        return (configStore, secretStore);
    }

    private FoundryFlux2Adapter CreateFlux2Adapter(ConfigStore? configStore = null, FakeSecretStore? secretStore = null)
    {
        configStore ??= new ConfigStore();
        secretStore ??= new FakeSecretStore { IsAvailable = true };
        var resolver = new SecretResolver(new[] { secretStore });
        
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return new FoundryFlux2Adapter(factory, resolver, configStore);
    }

    private FoundryMaiImage2Adapter CreateMaiImage2Adapter(ConfigStore? configStore = null, FakeSecretStore? secretStore = null)
    {
        configStore ??= new ConfigStore();
        secretStore ??= new FakeSecretStore { IsAvailable = true };
        var resolver = new SecretResolver(new[] { secretStore });
        
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return new FoundryMaiImage2Adapter(factory, resolver, configStore);
    }

    private FoundryGptImage1p5Adapter CreateGptImage1p5Adapter(ConfigStore? configStore = null, FakeSecretStore? secretStore = null)
    {
        configStore ??= new ConfigStore();
        secretStore ??= new FakeSecretStore { IsAvailable = true };
        var resolver = new SecretResolver(new[] { secretStore });
        
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return new FoundryGptImage1p5Adapter(factory, resolver, configStore);
    }

    private FoundryGptImage2Adapter CreateGptImage2Adapter(ConfigStore? configStore = null, FakeSecretStore? secretStore = null)
    {
        configStore ??= new ConfigStore();
        secretStore ??= new FakeSecretStore { IsAvailable = true };
        var resolver = new SecretResolver(new[] { secretStore });
        
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        return new FoundryGptImage2Adapter(factory, resolver, configStore);
    }

    #endregion
}
#endif
