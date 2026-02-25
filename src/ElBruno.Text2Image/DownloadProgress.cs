namespace ElBruno.Text2Image;

/// <summary>
/// Reports download progress for model files.
/// </summary>
public sealed class DownloadProgress
{
    /// <summary>The current download stage.</summary>
    public DownloadStage Stage { get; set; }

    /// <summary>Percentage complete (0-100).</summary>
    public double PercentComplete { get; set; }

    /// <summary>Bytes downloaded so far.</summary>
    public long BytesDownloaded { get; set; }

    /// <summary>Total bytes to download.</summary>
    public long TotalBytes { get; set; }

    /// <summary>Name of the file currently being downloaded.</summary>
    public string? CurrentFile { get; set; }

    /// <summary>Human-readable status message.</summary>
    public string? Message { get; set; }
}

/// <summary>
/// Stages of the model download process.
/// </summary>
public enum DownloadStage
{
    /// <summary>Checking existing files.</summary>
    Checking = 0,

    /// <summary>Downloading files.</summary>
    Downloading = 1,

    /// <summary>Download complete.</summary>
    Complete = 2
}
