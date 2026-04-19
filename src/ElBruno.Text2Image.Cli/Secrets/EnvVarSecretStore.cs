namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Secret store that reads from environment variables.
/// Format: T2I_<PROVIDER>_<FIELD> (e.g., T2I_FOUNDRY_FLUX2_APIKEY)
/// </summary>
internal sealed class EnvVarSecretStore : ISecretStore
{
    private const string Prefix = "T2I_";

    public string Name => "env";
    public bool IsAvailable => true;

    public Task<string?> GetAsync(string provider, string field, CancellationToken ct)
    {
        var envVarName = BuildEnvVarName(provider, field);
        var value = Environment.GetEnvironmentVariable(envVarName);
        return Task.FromResult(value);
    }

    public Task SetAsync(string provider, string field, string value, CancellationToken ct)
    {
        throw new NotSupportedException("env vars are read from process environment; set them via your shell");
    }

    public Task DeleteAsync(string provider, string field, CancellationToken ct)
    {
        throw new NotSupportedException("env vars are read from process environment; set them via your shell");
    }

    public Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct)
    {
        var providerPrefix = BuildProviderPrefix(provider);
        var fields = new List<string>();

        foreach (var key in Environment.GetEnvironmentVariables().Keys)
        {
            var envVarName = key.ToString();
            if (envVarName != null && envVarName.StartsWith(providerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fieldSuffix = envVarName.Substring(providerPrefix.Length);
                var normalizedField = fieldSuffix.ToLowerInvariant().Replace('_', '-');
                fields.Add(normalizedField);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(fields);
    }

    private static string BuildEnvVarName(string provider, string field)
    {
        var providerNormalized = provider.Replace("-", "_").ToUpperInvariant();
        var fieldNormalized = field.Replace("-", "_").ToUpperInvariant();
        return $"{Prefix}{providerNormalized}_{fieldNormalized}";
    }

    private static string BuildProviderPrefix(string provider)
    {
        var providerNormalized = provider.Replace("-", "_").ToUpperInvariant();
        return $"{Prefix}{providerNormalized}_";
    }
}
