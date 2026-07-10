#if NET10_0_OR_GREATER
using System.Net;
using System.Text;
using Xunit;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Tests.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Foundry;

/// <summary>
/// Phase 3A: Tests for all provider adapters (Flux2, MAI-2, GPT-1.5, GPT-2).
/// Tests config/secret resolution, health checks, and generation workflows.
/// </summary>
[Collection("ConfigStore")]
public class ProviderAdapterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalAppData;
    private readonly string _originalXdgConfig;
    private readonly TextWriter _originalStdErr;

    public ProviderAdapterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"t2i-adapter-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? string.Empty;
        _originalXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
        
        Environment.SetEnvironmentVariable("APPDATA", _tempDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempDir);
        
        // Suppress Console.Error output during tests to prevent warning noise
        _originalStdErr = Console.Error;
        Console.SetError(TextWriter.Null);
    }

    public void Dispose()
    {
        Console.SetError(_originalStdErr);
        
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

    #region Flux2 Adapter Tests

    [Fact]
    public async Task Flux2Adapter_CheckAsync_WithMissingCredentials_ReturnsFalse()
    {
        var adapter = CreateFlux2Adapter();

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
        Assert.NotNull(health.Reason);
        Assert.Contains("Missing", health.Reason);
    }

    [Fact]
    public async Task Flux2Adapter_CheckAsync_WithValidCredentials_ReturnsTrue()
    {
        var (adapter, _, secretStore, configStore) = CreateFlux2AdapterWithDependencies();
        
        // Set up valid config and secrets
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api",
            Model = "flux-2-pro"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public async Task Flux2Adapter_CheckAsync_WithOnlyEndpoint_ReturnsFalse()
    {
        var (adapter, _, secretStore, configStore) = CreateFlux2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api"
        };
        await configStore.SaveAsync(config, CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
        Assert.Contains("apiKey", health.Reason ?? "");
    }

    [Fact]
    public async Task Flux2Adapter_CheckAsync_WithOnlyApiKey_ReturnsFalse()
    {
        var (adapter, _, secretStore, _) = CreateFlux2AdapterWithDependencies();
        
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
        Assert.Contains("endpoint", health.Reason ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Flux2Adapter_HasCorrectMetadata()
    {
        var adapter = CreateFlux2Adapter();

        Assert.Equal("foundry-flux2", adapter.Id);
        Assert.Equal("FLUX.2 Pro (Cloud)", adapter.DisplayName);
        Assert.Equal(ProviderKind.Cloud, adapter.Kind);
        Assert.Contains("apiKey", adapter.RequiredSecrets);
        Assert.Contains("endpoint", adapter.RequiredFields);
        Assert.Contains("model", adapter.RequiredFields);
    }

    [Fact]
    public async Task Flux2Adapter_GenerateAsync_WithMissingCredentials_ThrowsException()
    {
        var adapter = CreateFlux2Adapter();
        var request = new GenerationRequest(
            Prompt: "test",
            Width: 512,
            Height: 512,
            Steps: 20,
            OutputPath: Path.Combine(_tempDir, "test.png"),
            ExtraOptions: new Dictionary<string, string?>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.GenerateAsync(request, null, CancellationToken.None));
    }

    #endregion

    #region MAI-2 Adapter Tests

    [Fact]
    public async Task Mai2Adapter_CheckAsync_WithMissingCredentials_ReturnsFalse()
    {
        var adapter = CreateMai2Adapter();

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
        Assert.NotNull(health.Reason);
        Assert.Contains("Missing", health.Reason);
    }

    [Fact]
    public async Task Mai2Adapter_CheckAsync_WithValidCredentials_ReturnsTrue()
    {
        var (adapter, _, secretStore, configStore) = CreateMai2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-mai2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api",
            Model = "mai-image-2"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-mai2", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public void Mai2Adapter_HasCorrectMetadata()
    {
        var adapter = CreateMai2Adapter();

        Assert.Equal("foundry-mai2", adapter.Id);
        Assert.Equal("MAI-Image-2 (Cloud)", adapter.DisplayName);
        Assert.Equal(ProviderKind.Cloud, adapter.Kind);
        Assert.Contains("apiKey", adapter.RequiredSecrets);
        Assert.Contains("endpoint", adapter.RequiredFields);
    }

    [Fact]
    public async Task Mai2Adapter_GenerateAsync_WithMissingCredentials_ThrowsException()
    {
        var adapter = CreateMai2Adapter();
        var request = new GenerationRequest(
            Prompt: "test",
            Width: 512,
            Height: 512,
            Steps: 20,
            OutputPath: Path.Combine(_tempDir, "test.png"),
            ExtraOptions: new Dictionary<string, string?>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.GenerateAsync(request, null, CancellationToken.None));
    }

    #endregion

    #region GPT Image 1.5 Adapter Tests

    [Fact]
    public async Task GptImage1p5Adapter_CheckAsync_WithMissingCredentials_ReturnsFalse()
    {
        var adapter = CreateGptImage1p5Adapter();

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
        Assert.NotNull(health.Reason);
    }

    [Fact]
    public async Task GptImage1p5Adapter_CheckAsync_WithValidCredentials_ReturnsTrue()
    {
        var (adapter, _, secretStore, configStore) = CreateGptImage1p5AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-gpt-image-1p5"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api",
            Model = "gpt-image-1.5"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-gpt-image-1p5", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public void GptImage1p5Adapter_HasCorrectMetadata()
    {
        var adapter = CreateGptImage1p5Adapter();

        Assert.Equal("foundry-gpt-image-1p5", adapter.Id);
        Assert.Equal("GPT-Image-1.5 (Azure OpenAI)", adapter.DisplayName);
        Assert.Equal(ProviderKind.Cloud, adapter.Kind);
        Assert.Contains("apiKey", adapter.RequiredSecrets);
    }

    #endregion

    #region GPT Image 2 Adapter Tests

    [Fact]
    public async Task GptImage2Adapter_CheckAsync_WithMissingCredentials_ReturnsFalse()
    {
        var adapter = CreateGptImage2Adapter();

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
        Assert.NotNull(health.Reason);
    }

    [Fact]
    public async Task GptImage2Adapter_CheckAsync_WithValidCredentials_ReturnsTrue()
    {
        var (adapter, _, secretStore, configStore) = CreateGptImage2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-gpt-image-2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api",
            Model = "gpt-image-2"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-gpt-image-2", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public void GptImage2Adapter_HasCorrectMetadata()
    {
        var adapter = CreateGptImage2Adapter();

        Assert.Equal("foundry-gpt-image-2", adapter.Id);
        Assert.Equal("GPT-Image-2 (Azure OpenAI)", adapter.DisplayName);
        Assert.Equal(ProviderKind.Cloud, adapter.Kind);
        Assert.Contains("apiKey", adapter.RequiredSecrets);
    }

    #endregion

    #region DefaultModel Tests

    [Fact]
    public void Adapters_ExposeExpectedDefaultModelNames()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretResolver = CreateSecretResolver();
        var configStore = new ConfigStore();

        Assert.Equal("MAI-Image-2", new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore).DefaultModel);
        Assert.Equal("MAI-Image-2.5", new FoundryMaiImage25Adapter(httpClientFactory, secretResolver, configStore).DefaultModel);
        Assert.Equal("MAI-Image-2.5-Flash", new FoundryMaiImage25FlashAdapter(httpClientFactory, secretResolver, configStore).DefaultModel);
        Assert.Equal("FLUX.2-pro", new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore).DefaultModel);
        Assert.Equal("gpt-image-2", new FoundryGptImage2Adapter(httpClientFactory, secretResolver, configStore).DefaultModel);
        Assert.Equal("gpt-image-1.5", new FoundryGptImage1p5Adapter(httpClientFactory, secretResolver, configStore).DefaultModel);
    }

    #endregion

    #region Config Resolution Tests

    [Fact]
    public async Task Adapters_ReadEndpoint_FromConfig()
    {
        var (adapter, _, _, configStore) = CreateFlux2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://custom-endpoint.example.com/api",
            Model = "flux-2-pro"
        };
        await configStore.SaveAsync(config, CancellationToken.None);

        var loadedConfig = await configStore.LoadAsync(CancellationToken.None);

        Assert.NotNull(loadedConfig.Providers["foundry-flux2"].Endpoint);
        Assert.Equal("https://custom-endpoint.example.com/api", loadedConfig.Providers["foundry-flux2"].Endpoint);
    }

    [Fact]
    public async Task Adapters_ReadApiKey_FromSecretStore()
    {
        var (_, secretResolver, secretStore, _) = CreateFlux2AdapterWithDependencies();
        
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-secret-key", CancellationToken.None);

        var resolved = await secretResolver.ResolveAsync("foundry-flux2", "apiKey", null, CancellationToken.None);

        Assert.Equal("test-secret-key", resolved);
    }

    [Fact]
    public async Task Adapters_ConfigAndSecrets_WorkTogether()
    {
        var (adapter, secretResolver, secretStore, configStore) = CreateFlux2AdapterWithDependencies();
        
        // Set up config
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api",
            Model = "flux-2-pro"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        
        // Set up secret
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.True(health.Ok);
    }

    [Fact]
    public async Task Adapters_EnvVar_FallbackWorks()
    {
        Environment.SetEnvironmentVariable("T2I_FOUNDRY_FLUX2_APIKEY", "env-var-key");
        
        try
        {
            var fakeStore = new FakeSecretStore();
            var envStore = new EnvVarSecretStore();
            var secretResolver = new SecretResolver(new List<ISecretStore> { fakeStore, envStore });

            var resolved = await secretResolver.ResolveAsync("foundry-flux2", "apiKey", null, CancellationToken.None);

            Assert.Equal("env-var-key", resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("T2I_FOUNDRY_FLUX2_APIKEY", null);
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Flux2Adapter_CheckAsync_WithHttpError_HandlesGracefully()
    {
        Environment.SetEnvironmentVariable("T2I_DETAILED_HEALTH_CHECKS", "1");
        
        try
        {
            var (adapter, _, secretStore, configStore) = CreateFlux2AdapterWithDependencies(
                createHttpHandler: _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            
            var config = new AppConfig();
            config.Providers["foundry-flux2"] = new ProviderConfig
            {
                Endpoint = "https://test.example.com/api",
                Model = "flux-2-pro"
            };
            await configStore.SaveAsync(config, CancellationToken.None);
            await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

            var health = await adapter.CheckAsync(CancellationToken.None);

            // With detailed checks enabled and HTTP error, should report issue
            Assert.False(health.Ok);
        }
        finally
        {
            Environment.SetEnvironmentVariable("T2I_DETAILED_HEALTH_CHECKS", null);
        }
    }

    [Fact]
    public async Task Adapters_CheckAsync_WithNetworkTimeout_HandlesGracefully()
    {
        Environment.SetEnvironmentVariable("T2I_DETAILED_HEALTH_CHECKS", "1");
        
        try
        {
            var (adapter, _, secretStore, configStore) = CreateFlux2AdapterWithDependencies(
                createHttpHandler: _ => throw new TaskCanceledException("Timeout"));
            
            var config = new AppConfig();
            config.Providers["foundry-flux2"] = new ProviderConfig
            {
                Endpoint = "https://test.example.com/api",
                Model = "flux-2-pro"
            };
            await configStore.SaveAsync(config, CancellationToken.None);
            await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

            var health = await adapter.CheckAsync(CancellationToken.None);

            Assert.False(health.Ok);
            Assert.NotNull(health.Reason);
        }
        finally
        {
            Environment.SetEnvironmentVariable("T2I_DETAILED_HEALTH_CHECKS", null);
        }
    }

    #endregion

    #region Additional Provider Tests

    [Fact]
    public void AllAdapters_HaveUniqueIds()
    {
        var flux2 = CreateFlux2Adapter();
        var mai2 = CreateMai2Adapter();
        var gpt15 = CreateGptImage1p5Adapter();
        var gpt2 = CreateGptImage2Adapter();

        var ids = new[] { flux2.Id, mai2.Id, gpt15.Id, gpt2.Id };
        Assert.Equal(4, ids.Distinct().Count());
    }

    [Fact]
    public void AllAdapters_AreCloudProviders()
    {
        var flux2 = CreateFlux2Adapter();
        var mai2 = CreateMai2Adapter();
        var gpt15 = CreateGptImage1p5Adapter();
        var gpt2 = CreateGptImage2Adapter();

        Assert.Equal(ProviderKind.Cloud, flux2.Kind);
        Assert.Equal(ProviderKind.Cloud, mai2.Kind);
        Assert.Equal(ProviderKind.Cloud, gpt15.Kind);
        Assert.Equal(ProviderKind.Cloud, gpt2.Kind);
    }

    [Fact]
    public void AllAdapters_RequireApiKey()
    {
        var flux2 = CreateFlux2Adapter();
        var mai2 = CreateMai2Adapter();
        var gpt15 = CreateGptImage1p5Adapter();
        var gpt2 = CreateGptImage2Adapter();

        Assert.Contains("apiKey", flux2.RequiredSecrets);
        Assert.Contains("apiKey", mai2.RequiredSecrets);
        Assert.Contains("apiKey", gpt15.RequiredSecrets);
        Assert.Contains("apiKey", gpt2.RequiredSecrets);
    }

    [Fact]
    public async Task Flux2Adapter_MultipleHealthChecks_AreConsistent()
    {
        var (adapter, _, secretStore, configStore) = CreateFlux2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-flux2"] = new ProviderConfig
        {
            Endpoint = "https://test.example.com/api",
            Model = "flux-2-pro"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-flux2", "apiKey", "test-key", CancellationToken.None);

        var health1 = await adapter.CheckAsync(CancellationToken.None);
        var health2 = await adapter.CheckAsync(CancellationToken.None);

        Assert.Equal(health1.Ok, health2.Ok);
    }

    [Fact]
    public async Task Mai2Adapter_WithPartialConfig_ReturnsFalse()
    {
        var (adapter, _, _, configStore) = CreateMai2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-mai2"] = new ProviderConfig
        {
            Model = "mai-image-2"
            // Missing Endpoint
        };
        await configStore.SaveAsync(config, CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
    }

    [Fact]
    public async Task GptImage1p5Adapter_WithEmptyEndpoint_ReturnsFalse()
    {
        var (adapter, _, secretStore, configStore) = CreateGptImage1p5AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-gpt-image-1p5"] = new ProviderConfig
        {
            Endpoint = "",
            Model = "gpt-image-1.5"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-gpt-image-1p5", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
    }

    [Fact]
    public async Task GptImage2Adapter_WithWhitespaceEndpoint_ReturnsFalse()
    {
        var (adapter, _, secretStore, configStore) = CreateGptImage2AdapterWithDependencies();
        
        var config = new AppConfig();
        config.Providers["foundry-gpt-image-2"] = new ProviderConfig
        {
            Endpoint = "   ",
            Model = "gpt-image-2"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-gpt-image-2", "apiKey", "test-key", CancellationToken.None);

        var health = await adapter.CheckAsync(CancellationToken.None);

        Assert.False(health.Ok);
    }

    [Fact]
    public async Task Adapters_SecretResolver_ReturnsNullForMissingSecret()
    {
        var (_, secretResolver, _, _) = CreateFlux2AdapterWithDependencies();

        var resolved = await secretResolver.ResolveAsync("foundry-flux2", "missing-field", null, CancellationToken.None);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task Adapters_ConfigStore_RoundTripPreservesData()
    {
        var (_, _, _, configStore) = CreateFlux2AdapterWithDependencies();
        
        var config = new AppConfig { DefaultProvider = "test-provider" };
        config.Providers["test"] = new ProviderConfig
        {
            Endpoint = "https://example.com",
            Model = "test-model"
        };
        config.Providers["test"].Extras["custom"] = "value";

        await configStore.SaveAsync(config, CancellationToken.None);
        var loaded = await configStore.LoadAsync(CancellationToken.None);

        Assert.Equal("test-provider", loaded.DefaultProvider);
        Assert.Equal("https://example.com", loaded.Providers["test"].Endpoint);
        Assert.Equal("test-model", loaded.Providers["test"].Model);
        Assert.Equal("value", loaded.Providers["test"].Extras["custom"]);
    }

    #endregion

    #region MAI-2.5 Adapter Tests

    [Fact]
    public async Task MaiImage25Adapter_GenerateAsync_UsesEndpointOverride()
    {
        string? requestedEndpoint = null;
        var (adapter, secretStore, configStore) = CreateMaiImage25AdapterWithDependencies(request =>
        {
            requestedEndpoint = request.RequestUri!.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":[{"b64_json":"iVBORw=="}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });

        var config = new AppConfig();
        config.Providers["foundry-mai25"] = new ProviderConfig
        {
            Endpoint = "https://configured.services.ai.azure.com",
            Model = "MAI-Image-2.5"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-mai25", "apiKey", "test-key", CancellationToken.None);

        var request = new GenerationRequest(
            "test",
            1024,
            1024,
            20,
            Path.Combine(_tempDir, "override.png"),
            new Dictionary<string, string?>
            {
                ["endpoint"] = "https://override.services.ai.azure.com"
            });

        var result = await adapter.GenerateAsync(request, null, CancellationToken.None);

        Assert.StartsWith("https://override.services.ai.azure.com/mai/v1/images/generations", requestedEndpoint);
        Assert.Equal("https://override.services.ai.azure.com", result.Metadata["endpoint"]);
    }

    [Fact]
    public async Task MaiImage25Adapter_GenerateAsync_RejectsPixelCountAboveMaximum()
    {
        var (adapter, secretStore, configStore) = CreateMaiImage25AdapterWithDependencies();

        var config = new AppConfig();
        config.Providers["foundry-mai25"] = new ProviderConfig
        {
            Endpoint = "https://configured.services.ai.azure.com",
            Model = "MAI-Image-2.5"
        };
        await configStore.SaveAsync(config, CancellationToken.None);
        await secretStore.SetAsync("foundry-mai25", "apiKey", "test-key", CancellationToken.None);

        var request = new GenerationRequest(
            "test",
            1025,
            1024,
            20,
            Path.Combine(_tempDir, "too-large.png"),
            new Dictionary<string, string?>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => adapter.GenerateAsync(request, null, CancellationToken.None));
    }

    #endregion

    #region Helper Methods

    private IProviderAdapter CreateFlux2Adapter()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretResolver = CreateSecretResolver();
        var configStore = new ConfigStore();
        
        return new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);
    }

    private (IProviderAdapter Adapter, SecretResolver Resolver, FakeSecretStore SecretStore, ConfigStore ConfigStore) 
        CreateFlux2AdapterWithDependencies(Func<HttpRequestMessage, HttpResponseMessage>? createHttpHandler = null)
    {
        var httpClientFactory = new FakeHttpClientFactory(createHttpHandler);
        var secretStore = new FakeSecretStore();
        var secretResolver = new SecretResolver(new List<ISecretStore> { secretStore });
        var configStore = new ConfigStore();
        
        var adapter = new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);
        return (adapter, secretResolver, secretStore, configStore);
    }

    private IProviderAdapter CreateMai2Adapter()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretResolver = CreateSecretResolver();
        var configStore = new ConfigStore();
        
        return new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);
    }

    private (IProviderAdapter Adapter, SecretResolver Resolver, FakeSecretStore SecretStore, ConfigStore ConfigStore) 
        CreateMai2AdapterWithDependencies()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretStore = new FakeSecretStore();
        var secretResolver = new SecretResolver(new List<ISecretStore> { secretStore });
        var configStore = new ConfigStore();
        
        var adapter = new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);
        return (adapter, secretResolver, secretStore, configStore);
    }

    private IProviderAdapter CreateGptImage1p5Adapter()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretResolver = CreateSecretResolver();
        var configStore = new ConfigStore();
        
        return new FoundryGptImage1p5Adapter(httpClientFactory, secretResolver, configStore);
    }

    private (IProviderAdapter Adapter, SecretResolver Resolver, FakeSecretStore SecretStore, ConfigStore ConfigStore) 
        CreateGptImage1p5AdapterWithDependencies()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretStore = new FakeSecretStore();
        var secretResolver = new SecretResolver(new List<ISecretStore> { secretStore });
        var configStore = new ConfigStore();
        
        var adapter = new FoundryGptImage1p5Adapter(httpClientFactory, secretResolver, configStore);
        return (adapter, secretResolver, secretStore, configStore);
    }

    private IProviderAdapter CreateGptImage2Adapter()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretResolver = CreateSecretResolver();
        var configStore = new ConfigStore();
        
        return new FoundryGptImage2Adapter(httpClientFactory, secretResolver, configStore);
    }

    private (IProviderAdapter Adapter, SecretResolver Resolver, FakeSecretStore SecretStore, ConfigStore ConfigStore) 
        CreateGptImage2AdapterWithDependencies()
    {
        var httpClientFactory = new FakeHttpClientFactory();
        var secretStore = new FakeSecretStore();
        var secretResolver = new SecretResolver(new List<ISecretStore> { secretStore });
        var configStore = new ConfigStore();
        
        var adapter = new FoundryGptImage2Adapter(httpClientFactory, secretResolver, configStore);
        return (adapter, secretResolver, secretStore, configStore);
    }

    private (FoundryMaiImage25Adapter Adapter, FakeSecretStore SecretStore, ConfigStore ConfigStore)
        CreateMaiImage25AdapterWithDependencies(Func<HttpRequestMessage, HttpResponseMessage>? createHttpHandler = null)
    {
        var httpClientFactory = new FakeHttpClientFactory(createHttpHandler);
        var secretStore = new FakeSecretStore();
        var secretResolver = new SecretResolver(new List<ISecretStore> { secretStore });
        var configStore = new ConfigStore();

        var adapter = new FoundryMaiImage25Adapter(httpClientFactory, secretResolver, configStore);
        return (adapter, secretStore, configStore);
    }

    private SecretResolver CreateSecretResolver()
    {
        var fakeStore = new FakeSecretStore();
        var stores = new List<ISecretStore> { fakeStore };
        return new SecretResolver(stores);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _createHandler;

        public FakeHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage>? createHandler = null)
        {
            _createHandler = createHandler;
        }

        public HttpClient CreateClient(string name)
        {
            var handler = new FakeHttpHandler(_createHandler ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }));
            return new HttpClient(handler);
        }
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_responseFactory(request));
            }
            catch (Exception ex)
            {
                return Task.FromException<HttpResponseMessage>(ex);
            }
        }
    }

    #endregion
}
#endif
