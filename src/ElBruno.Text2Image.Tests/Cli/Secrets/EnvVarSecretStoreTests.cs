#if NET10_0_OR_GREATER
using Xunit;
using ElBruno.Text2Image.Cli.Secrets;

namespace ElBruno.Text2Image.Tests.Cli.Secrets;

public class EnvVarSecretStoreTests : IDisposable
{
    private readonly List<string> _envVarsToCleanUp = new();

    [Fact]
    public async Task Get_ReadsEnvVar_WithCorrectNamingConvention()
    {
        SetTestEnvVar("T2I_FOUNDRY_FLUX2_APIKEY", "xyz");

        var store = new EnvVarSecretStore();
        var result = await store.GetAsync("foundry-flux2", "apiKey", CancellationToken.None);

        Assert.Equal("xyz", result);
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenEnvVarMissing()
    {
        var store = new EnvVarSecretStore();
        var result = await store.GetAsync("nonexistent-provider", "apiKey", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Set_Throws_NotSupported()
    {
        var store = new EnvVarSecretStore();

        await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await store.SetAsync("test", "field", "value", CancellationToken.None));
    }

    [Fact]
    public async Task ListFields_ReturnsAllPrefixedFields()
    {
        SetTestEnvVar("T2I_FOUNDRY_FLUX2_APIKEY", "key1");
        SetTestEnvVar("T2I_FOUNDRY_FLUX2_ENDPOINT", "https://example.com");
        SetTestEnvVar("T2I_FOUNDRY_FLUX2_MODEL", "flux-2-pro");
        SetTestEnvVar("T2I_OTHER_PROVIDER_FIELD", "other");

        var store = new EnvVarSecretStore();
        var fields = await store.ListFieldsAsync("foundry-flux2", CancellationToken.None);

        Assert.Equal(3, fields.Count);
        Assert.Contains("apikey", fields);
        Assert.Contains("endpoint", fields);
        Assert.Contains("model", fields);
    }

    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        var store = new EnvVarSecretStore();
        Assert.True(store.IsAvailable);
    }

    [Fact]
    public void Name_ReturnsEnv()
    {
        var store = new EnvVarSecretStore();
        Assert.Equal("env", store.Name);
    }

    private void SetTestEnvVar(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
        _envVarsToCleanUp.Add(name);
    }

    public void Dispose()
    {
        foreach (var varName in _envVarsToCleanUp)
        {
            Environment.SetEnvironmentVariable(varName, null, EnvironmentVariableTarget.Process);
        }
    }
}
#endif
