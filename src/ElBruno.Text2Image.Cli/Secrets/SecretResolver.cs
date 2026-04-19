namespace ElBruno.Text2Image.Cli.Secrets;

/// <summary>
/// Resolves secrets using the 5-layer chain:
/// 1. CLI flags
/// 2. Environment variables
/// 3. OS native store (DPAPI on Windows)
/// 4. Plaintext config file
/// 5. Interactive wizard (if TTY)
/// </summary>
public sealed class SecretResolver
{
    // TODO(Wash): implement resolution chain
    public Task<string?> ResolveAsync(
        string provider,
        string field,
        IDictionary<string, string?>? cliOverrides,
        CancellationToken ct)
    {
        throw new NotImplementedException("SecretResolver not yet implemented");
    }
}
