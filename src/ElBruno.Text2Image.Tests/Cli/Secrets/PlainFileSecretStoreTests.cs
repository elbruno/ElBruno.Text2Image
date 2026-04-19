#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Secrets;
using ElBruno.Text2Image.Cli.Config;
using System.Runtime.InteropServices;

namespace ElBruno.Text2Image.Tests.Cli.Secrets;

[Collection("PlainFileSecretStore")]
public class PlainFileSecretStoreTests : IDisposable
{
    private readonly string _tempConfigDir;

    public PlainFileSecretStoreTests()
    {
        _tempConfigDir = Path.Combine(Path.GetTempPath(), $"t2i-test-pfs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempConfigDir);
        Environment.SetEnvironmentVariable("APPDATA", _tempConfigDir);
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", _tempConfigDir);
    }

    [Fact]
    public async Task RoundTrip_PersistsToDisk()
    {
        var store = new PlainFileSecretStore();
        const string testValue = "my-secret-key";

        await store.SetAsync("test-provider", "apiKey", testValue, CancellationToken.None);
        var retrieved = await store.GetAsync("test-provider", "apiKey", CancellationToken.None);

        Assert.Equal(testValue, retrieved);
    }

    [Fact]
    public async Task Delete_RemovesEntry()
    {
        var store = new PlainFileSecretStore();
        const string testValue = "my-secret-key";

        await store.SetAsync("test-provider", "apiKey", testValue, CancellationToken.None);
        await store.DeleteAsync("test-provider", "apiKey", CancellationToken.None);
        var retrieved = await store.GetAsync("test-provider", "apiKey", CancellationToken.None);

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task ListFields_ReturnsOnlyMatchingProvider()
    {
        var store = new PlainFileSecretStore();

        await store.SetAsync("provider-a", "field1", "value1", CancellationToken.None);
        await store.SetAsync("provider-a", "field2", "value2", CancellationToken.None);
        await store.SetAsync("provider-b", "field1", "value3", CancellationToken.None);

        var fields = await store.ListFieldsAsync("provider-a", CancellationToken.None);

        Assert.Equal(2, fields.Count);
        Assert.Contains("field1", fields);
        Assert.Contains("field2", fields);
    }

    [Fact]
    public async Task Save_SetsFileMode0600_OnLinux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        var store = new PlainFileSecretStore();
        await store.SetAsync("test-provider", "apiKey", "secret", CancellationToken.None);

        var secretsFile = Path.Combine(ConfigPaths.ConfigDirectory, "secrets.json");
        Assert.True(File.Exists(secretsFile));

        var mode = File.GetUnixFileMode(secretsFile);
        var expected = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        Assert.Equal(expected, mode);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        var store = new PlainFileSecretStore();
        Assert.True(store.IsAvailable);
    }

    [Fact]
    public void Name_ReturnsFile()
    {
        var store = new PlainFileSecretStore();
        Assert.Equal("file", store.Name);
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

[CollectionDefinition("PlainFileSecretStore")]
public class PlainFileSecretStoreCollection : ICollectionFixture<object>
{
}
#endif
