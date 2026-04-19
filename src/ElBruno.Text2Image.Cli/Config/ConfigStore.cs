namespace ElBruno.Text2Image.Cli.Config;

/// <summary>
/// Persists and loads AppConfig from disk.
/// </summary>
public sealed class ConfigStore
{
    // TODO(Wash): implement Load/Save logic
    public Task<AppConfig> LoadAsync(CancellationToken ct)
    {
        throw new NotImplementedException("ConfigStore.LoadAsync not yet implemented");
    }

    public Task SaveAsync(AppConfig config, CancellationToken ct)
    {
        throw new NotImplementedException("ConfigStore.SaveAsync not yet implemented");
    }
}
