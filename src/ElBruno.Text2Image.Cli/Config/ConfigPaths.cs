namespace ElBruno.Text2Image.Cli.Config;

/// <summary>
/// Provides platform-specific config file paths.
/// Note: Paths are evaluated dynamically to support test environment variable overrides.
/// </summary>
public static class ConfigPaths
{
    /// <summary>
    /// Gets the config directory path for the current OS.
    /// Windows: %APPDATA%\t2i (reads environment variable to support test isolation)
    /// Linux/macOS: $XDG_CONFIG_HOME/t2i or ~/.config/t2i
    /// </summary>
    public static string ConfigDirectory
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                // Read APPDATA from environment variable (supports test overrides)
                // Fallback to GetFolderPath if env var not set
                var appData = Environment.GetEnvironmentVariable("APPDATA") 
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "t2i");
            }
            else
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                {
                    return Path.Combine(xdgConfigHome, "t2i");
                }

                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".config", "t2i");
            }
        }
    }

    /// <summary>
    /// Gets the config file path for the current OS.
    /// </summary>
    public static string AppConfigFile => Path.Combine(ConfigDirectory, "config.json");
    
    /// <summary>
    /// Alias for AppConfigFile.
    /// </summary>
    public static string ConfigFilePath => AppConfigFile;
}
