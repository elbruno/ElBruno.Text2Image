#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Cli.Secrets;

public class DpapiSecretStoreTests : IDisposable
{
    public DpapiSecretStoreTests()
    {
        if (OperatingSystem.IsWindows())
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"t2i-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", tempDir);
        }
    }

    [Fact]
    public async Task RoundTrip_EncryptedValue_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var store = new DpapiSecretStore();
        const string testValue = "super-secret-key-12345";

        await store.SetAsync("test-provider", "apiKey", testValue, CancellationToken.None);
        var retrieved = await store.GetAsync("test-provider", "apiKey", CancellationToken.None);

        Assert.Equal(testValue, retrieved);

        await store.DeleteAsync("test-provider", "apiKey", CancellationToken.None);
    }

    [Fact]
    public void IsAvailable_FalseOnNonWindows()
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

    [Fact]
    public void Name_ReturnsDpapi()
    {
        var store = new DpapiSecretStore();
        Assert.Equal("dpapi", store.Name);
    }

    [Fact]
    public async Task ListFields_ReturnsOnlyMatchingProvider()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var store = new DpapiSecretStore();

        await store.SetAsync("provider-a", "field1", "value1", CancellationToken.None);
        await store.SetAsync("provider-a", "field2", "value2", CancellationToken.None);
        await store.SetAsync("provider-b", "field1", "value3", CancellationToken.None);

        var fields = await store.ListFieldsAsync("provider-a", CancellationToken.None);

        Assert.Equal(2, fields.Count);
        Assert.Contains("field1", fields);
        Assert.Contains("field2", fields);

        await store.DeleteAsync("provider-a", "field1", CancellationToken.None);
        await store.DeleteAsync("provider-a", "field2", CancellationToken.None);
        await store.DeleteAsync("provider-b", "field1", CancellationToken.None);
    }

    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData) && Directory.Exists(localAppData))
            {
                try
                {
                    Directory.Delete(localAppData, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            Environment.SetEnvironmentVariable("LOCALAPPDATA", null);
        }
    }
}
#endif
