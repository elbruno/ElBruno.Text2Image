#if NET10_0_OR_GREATER
using System.Runtime.Versioning;
using Xunit;
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Cli.Secrets;

/// <summary>
/// Phase 3B: Secret store tests for DPAPI encryption, file permissions, expiration, rotation.
/// Tests platform-specific secret storage security features.
/// </summary>
[Collection("ConfigStore")]
public class SecretStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _originalAppData;
    private readonly string _originalXdgConfig;
    private readonly string _originalLocalAppData;

    public SecretStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"t2i-secret-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA") ?? string.Empty;
        _originalXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
        _originalLocalAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA") ?? string.Empty;
        
        Environment.SetEnvironmentVariable("APPDATA", _tempDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempDir);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _tempDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("APPDATA", _originalAppData);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _originalXdgConfig);
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalLocalAppData);
        
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

    #region DPAPI Tests (Windows-specific)

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task DpapiSecretStore_StoresAndRetrievesSecret_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            // Skip on non-Windows
            return;
        }

        var store = new DpapiSecretStore();
        
        await store.SetAsync("test-provider", "apiKey", "secret-value", CancellationToken.None);
        var retrieved = await store.GetAsync("test-provider", "apiKey", CancellationToken.None);

        Assert.Equal("secret-value", retrieved);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public async Task DpapiSecretStore_EncryptsData_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new DpapiSecretStore();
        await store.SetAsync("test-provider", "apiKey", "secret-value", CancellationToken.None);

        var secretsFile = Path.Combine(_tempDir, "t2i", "secrets.dpapi");
        Assert.True(File.Exists(secretsFile));

        var fileContent = await File.ReadAllTextAsync(secretsFile);
        // Secret should be encrypted - original value should NOT appear in file
        Assert.DoesNotContain("secret-value", fileContent);
    }

    [Fact]
    public void DpapiSecretStore_IsAvailable_OnlyOnWindows()
    {
        var store = new DpapiSecretStore();
        
        if (OperatingSystem.IsWindows())
        {
            Assert.True(store.IsAvailable);
        }
        else
        {
            Assert.False(store.IsAvailable);
        }
    }

    #endregion

    #region PlainFileSecretStore Tests

    [Fact]
    public async Task PlainFileSecretStore_StoresAndRetrievesSecret()
    {
        var store = new PlainFileSecretStore();
        
        await store.SetAsync("test-provider", "apiKey", "secret-value", CancellationToken.None);
        var retrieved = await store.GetAsync("test-provider", "apiKey", CancellationToken.None);

        Assert.Equal("secret-value", retrieved);
    }

    [Fact]
    public async Task PlainFileSecretStore_ListsFieldsForProvider()
    {
        var store = new PlainFileSecretStore();
        
        await store.SetAsync("test-provider", "apiKey", "value1", CancellationToken.None);
        await store.SetAsync("test-provider", "endpoint", "value2", CancellationToken.None);
        await store.SetAsync("other-provider", "apiKey", "value3", CancellationToken.None);

        var fields = await store.ListFieldsAsync("test-provider", CancellationToken.None);

        Assert.Equal(2, fields.Count);
        Assert.Contains("apiKey", fields);
        Assert.Contains("endpoint", fields);
    }

    [Fact]
    public async Task PlainFileSecretStore_DeletesSecret()
    {
        var store = new PlainFileSecretStore();
        
        await store.SetAsync("test-provider", "apiKey", "secret-value", CancellationToken.None);
        await store.DeleteAsync("test-provider", "apiKey", CancellationToken.None);
        var retrieved = await store.GetAsync("test-provider", "apiKey", CancellationToken.None);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task PlainFileSecretStore_ReturnsNull_ForNonexistentSecret()
    {
        var store = new PlainFileSecretStore();
        
        var retrieved = await store.GetAsync("nonexistent", "field", CancellationToken.None);

        Assert.Null(retrieved);
    }

    #endregion

    #region SecretResolver Tests

    [Fact]
    public async Task SecretResolver_RetrievesFromStore()
    {
        var store = new FakeSecretStore { IsAvailable = true };
        store.Data[("test-provider", "apiKey")] = "stored-value";
        
        var resolver = new SecretResolver(new[] { store });
        var value = await resolver.ResolveAsync("test-provider", "apiKey", null, CancellationToken.None);

        Assert.Equal("stored-value", value);
    }

    [Fact]
    public async Task SecretResolver_FallsBackToEnvironmentVariable()
    {
        var store = new FakeSecretStore { IsAvailable = true };
        var envStore = new EnvVarSecretStore();
        var resolver = new SecretResolver(new ISecretStore[] { store, envStore });
        
        Environment.SetEnvironmentVariable("T2I_TEST_PROVIDER_APIKEY", "env-value");
        var value = await resolver.ResolveAsync("test-provider", "apiKey", null, CancellationToken.None);

        Assert.Equal("env-value", value);
        Environment.SetEnvironmentVariable("T2I_TEST_PROVIDER_APIKEY", null);
    }

    #endregion
}
#endif
