#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Tui;
using ElBruno.Text2Image.Cli.Providers;
using ElBruno.Text2Image.Cli.Config;
using ElBruno.Text2Image.Cli.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace ElBruno.Text2Image.Tests.Cli;

public class ConfigDisplayTests
{
    [Fact]
    public void Mask_MasksApiKeyStartingWithSk()
    {
        var result = ConsoleHelpers.Mask("sk-1234567890abcdef1234567890abcdef");
        
        Assert.StartsWith("sk-", result);
        Assert.Contains("***", result);
        Assert.DoesNotContain("1234567890abcdef", result);
    }

    [Fact]
    public void Mask_ShowsFirstAndLastCharacters()
    {
        var secret = "sk-1234567890abcdefgh";
        var result = ConsoleHelpers.Mask(secret);
        
        Assert.Contains("sk-123", result);
        Assert.Contains("efgh", result);
        Assert.Contains("***...***", result);
    }

    [Fact]
    public void FoundryMaiImage2Adapter_RequiredSecrets_DoesNotContainEndpoint()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.DoesNotContain("endpoint", adapter.RequiredSecrets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryMaiImage2Adapter_RequiredSecrets_DoesNotContainModel()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.DoesNotContain("model", adapter.RequiredSecrets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryMaiImage2Adapter_RequiredSecrets_ContainsOnlyApiKey()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.Single(adapter.RequiredSecrets);
        Assert.Contains("apiKey", adapter.RequiredSecrets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryMaiImage2Adapter_RequiredFields_ContainsEndpoint()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.Contains("endpoint", adapter.RequiredFields, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryMaiImage2Adapter_RequiredFields_ContainsModel()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryMaiImage2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.Contains("model", adapter.RequiredFields, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryFlux2Adapter_RequiredSecrets_DoesNotContainEndpoint()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.DoesNotContain("endpoint", adapter.RequiredSecrets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryFlux2Adapter_RequiredSecrets_DoesNotContainModel()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.DoesNotContain("model", adapter.RequiredSecrets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryFlux2Adapter_RequiredSecrets_ContainsOnlyApiKey()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.Single(adapter.RequiredSecrets);
        Assert.Contains("apiKey", adapter.RequiredSecrets, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryFlux2Adapter_RequiredFields_ContainsEndpoint()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.Contains("endpoint", adapter.RequiredFields, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void FoundryFlux2Adapter_RequiredFields_ContainsModel()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var configStore = new ConfigStore();
        var secretResolver = new SecretResolver(Array.Empty<ISecretStore>());
        var adapter = new FoundryFlux2Adapter(httpClientFactory, secretResolver, configStore);

        Assert.Contains("model", adapter.RequiredFields, StringComparer.OrdinalIgnoreCase);
    }
}
#endif
