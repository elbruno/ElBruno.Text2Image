using Microsoft.ML.OnnxRuntime;

namespace ElBruno.Text2Image;

/// <summary>
/// Helper to configure ONNX Runtime session options based on the selected execution provider.
/// Supports automatic GPU detection with graceful fallback.
/// </summary>
public static class SessionOptionsHelper
{
    private static ExecutionProvider? _cachedBestProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Creates session options for the given provider. If Auto, probes for the best available GPU.
    /// </summary>
    public static SessionOptions Create(ExecutionProvider provider)
    {
        var resolved = provider == ExecutionProvider.Auto ? DetectBestProvider() : provider;
        return CreateForProvider(resolved);
    }

    /// <summary>
    /// Gets the resolved provider that will actually be used (useful for logging).
    /// </summary>
    public static ExecutionProvider ResolveProvider(ExecutionProvider provider)
    {
        return provider == ExecutionProvider.Auto ? DetectBestProvider() : provider;
    }

    /// <summary>
    /// Probes available execution providers in priority order: CUDA → DirectML → CPU.
    /// Result is cached after first call.
    /// </summary>
    public static ExecutionProvider DetectBestProvider()
    {
        if (_cachedBestProvider.HasValue)
            return _cachedBestProvider.Value;

        lock (_lock)
        {
            if (_cachedBestProvider.HasValue)
                return _cachedBestProvider.Value;

            // Check available providers from ONNX Runtime
            var available = OrtEnv.Instance().GetAvailableProviders();

            if (available.Contains("CUDAExecutionProvider"))
            {
                // Verify CUDA actually works by trying to create a session option
                if (TryAppendProvider(() => { var o = new SessionOptions(); o.AppendExecutionProvider_CUDA(); o.Dispose(); }))
                {
                    _cachedBestProvider = ExecutionProvider.Cuda;
                    return _cachedBestProvider.Value;
                }
            }

            if (available.Contains("DmlExecutionProvider"))
            {
                if (TryAppendProvider(() => { var o = new SessionOptions(); o.AppendExecutionProvider_DML(); o.Dispose(); }))
                {
                    _cachedBestProvider = ExecutionProvider.DirectML;
                    return _cachedBestProvider.Value;
                }
            }

            _cachedBestProvider = ExecutionProvider.Cpu;
            return _cachedBestProvider.Value;
        }
    }

    private static SessionOptions CreateForProvider(ExecutionProvider provider)
    {
        var options = new SessionOptions();
        switch (provider)
        {
            case ExecutionProvider.Cuda:
                options.AppendExecutionProvider_CUDA();
                break;
            case ExecutionProvider.DirectML:
                options.AppendExecutionProvider_DML();
                break;
            case ExecutionProvider.Cpu:
            default:
                options.AppendExecutionProvider_CPU();
                break;
        }
        return options;
    }

    private static bool TryAppendProvider(Action appendAction)
    {
        try
        {
            appendAction();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
