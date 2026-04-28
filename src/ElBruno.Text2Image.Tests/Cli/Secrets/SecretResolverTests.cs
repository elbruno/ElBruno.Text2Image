#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Cli.Secrets;

public class SecretResolverTests
{
    [Fact]
    public async Task Resolve_PrefersCliOverride_OverEverything()
    {
        var envStore = new FakeSecretStore { Name = "env" };
        envStore.Data[("test-provider", "apiKey")] = "env-value";

        var fileStore = new FakeSecretStore { Name = "file" };
        fileStore.Data[("test-provider", "apiKey")] = "file-value";

        var resolver = new SecretResolver(new[] { envStore, fileStore });

        var cliOverrides = new Dictionary<string, string?>
        {
            ["apiKey"] = "cli-override"
        };

        var result = await resolver.ResolveAsync("test-provider", "apiKey", cliOverrides, CancellationToken.None);

        Assert.Equal("cli-override", result);
    }

    [Fact]
    public async Task Resolve_FallsThroughStores_InOrder()
    {
        var envStore = new FakeSecretStore { Name = "env" };
        var dpapiStore = new FakeSecretStore { Name = "dpapi" };
        var fileStore = new FakeSecretStore { Name = "file" };
        fileStore.Data[("test-provider", "apiKey")] = "file-value";

        var resolver = new SecretResolver(new ISecretStore[] { envStore, dpapiStore, fileStore });

        var result = await resolver.ResolveAsync("test-provider", "apiKey", null, CancellationToken.None);

        Assert.Equal("file-value", result);
    }

    [Fact]
    public async Task Resolve_ReturnsNull_WhenAllStoresEmpty()
    {
        var envStore = new FakeSecretStore { Name = "env" };
        var fileStore = new FakeSecretStore { Name = "file" };

        var resolver = new SecretResolver(new[] { envStore, fileStore });

        var result = await resolver.ResolveAsync("missing-provider", "apiKey", null, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Resolve_SkipsUnavailableStores()
    {
        var unavailableStore = new FakeSecretStore
        {
            Name = "unavailable",
            IsAvailable = false
        };
        unavailableStore.Data[("test-provider", "apiKey")] = "should-be-skipped";

        var fileStore = new FakeSecretStore { Name = "file" };
        fileStore.Data[("test-provider", "apiKey")] = "file-value";

        var resolver = new SecretResolver(new ISecretStore[] { unavailableStore, fileStore });

        var result = await resolver.ResolveAsync("test-provider", "apiKey", null, CancellationToken.None);

        Assert.Equal("file-value", result);
    }

    [Fact]
    public async Task InspectAsync_ReportsCorrectSourcePerField()
    {
        var envStore = new FakeSecretStore { Name = "env" };
        envStore.Data[("test-provider", "endpoint")] = "https://env.example.com";

        var fileStore = new FakeSecretStore { Name = "file" };
        fileStore.Data[("test-provider", "apiKey")] = "file-key";

        var resolver = new SecretResolver(new ISecretStore[] { envStore, fileStore });

        var inspection = await resolver.InspectAsync("test-provider", CancellationToken.None);

        Assert.Contains(inspection, i => i.Field == "endpoint" && i.Store == "env");
        Assert.Contains(inspection, i => i.Field == "apiKey" && i.Store == "file");
    }

    [Fact]
    public async Task SetAsync_PrefersDpapi_WhenAvailable_AndNoExplicitChoice()
    {
        var dpapiStore = new FakeSecretStore { Name = "dpapi", IsAvailable = true };
        var fileStore = new FakeSecretStore { Name = "file" };

        var resolver = new SecretResolver(new ISecretStore[] { dpapiStore, fileStore });

        await resolver.SetAsync("test-provider", "apiKey", "secret-value", preferredStoreName: null, CancellationToken.None);

        Assert.True(dpapiStore.Data.ContainsKey(("test-provider", "apiKey")));
        Assert.False(fileStore.Data.ContainsKey(("test-provider", "apiKey")));
    }

    [Fact]
    public async Task SetAsync_ThrowsOnWindows_WhenDpapiUnavailable()
    {
        var dpapiStore = new FakeSecretStore { Name = "dpapi", IsAvailable = false };
        var fileStore = new FakeSecretStore { Name = "file" };

        var resolver = new SecretResolver(new ISecretStore[] { dpapiStore, fileStore });

        if (OperatingSystem.IsWindows())
        {
            // On Windows, must throw if DPAPI is unavailable
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => resolver.SetAsync("test-provider", "apiKey", "secret-value", preferredStoreName: null, CancellationToken.None));
            
            Assert.Contains("Windows DPAPI", ex.Message);
            Assert.Contains("not allowed on Windows", ex.Message);
        }
        else
        {
            // On non-Windows, should fall back to file store
            await resolver.SetAsync("test-provider", "apiKey", "secret-value", preferredStoreName: null, CancellationToken.None);
            Assert.False(dpapiStore.Data.ContainsKey(("test-provider", "apiKey")));
            Assert.True(fileStore.Data.ContainsKey(("test-provider", "apiKey")));
        }
    }
}
#endif
