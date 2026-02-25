using ElBruno.HuggingFace;

namespace ElBruno.Text2Image;

/// <summary>
/// Manages model file downloading from HuggingFace, validation, and caching.
/// </summary>
internal sealed class ModelManager
{
    /// <summary>
    /// Checks whether all required model files exist in the specified directory.
    /// </summary>
    public static bool IsModelAvailable(string modelPath, string[] requiredFiles)
    {
        if (!Directory.Exists(modelPath))
            return false;

        return requiredFiles.All(f => File.Exists(Path.Combine(modelPath, f)));
    }

    /// <summary>
    /// Downloads model files from HuggingFace if not already present.
    /// </summary>
    public static async Task EnsureModelAvailableAsync(
        string modelPath,
        string huggingFaceRepo,
        string[] requiredFiles,
        string[]? optionalFiles = null,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (IsModelAvailable(modelPath, requiredFiles))
        {
            progress?.Report(new DownloadProgress
            {
                Stage = DownloadStage.Complete,
                PercentComplete = 100,
                Message = "Model already downloaded"
            });
            return;
        }

        using var downloader = new HuggingFaceDownloader();

        IProgress<ElBruno.HuggingFace.DownloadProgress>? packageProgress = null;
        if (progress != null)
        {
            packageProgress = new Progress<ElBruno.HuggingFace.DownloadProgress>(p =>
            {
                progress.Report(new DownloadProgress
                {
                    Stage = (DownloadStage)(int)p.Stage,
                    PercentComplete = p.PercentComplete,
                    BytesDownloaded = p.BytesDownloaded,
                    TotalBytes = p.TotalBytes,
                    CurrentFile = p.CurrentFile,
                    Message = p.Message
                });
            });
        }

        await downloader.DownloadFilesAsync(new DownloadRequest
        {
            RepoId = huggingFaceRepo,
            LocalDirectory = modelPath,
            RequiredFiles = requiredFiles,
            OptionalFiles = optionalFiles ?? [],
            Progress = packageProgress
        }, cancellationToken);
    }
}
