namespace ElBruno.Text2Image;

/// <summary>
/// ONNX Runtime execution provider selection.
/// </summary>
public enum ExecutionProvider
{
    /// <summary>
    /// Automatically detect the best available provider.
    /// Probes in order: CUDA → DirectML → CPU.
    /// </summary>
    Auto = -1,

    /// <summary>CPU execution (works everywhere).</summary>
    Cpu = 0,

    /// <summary>CUDA GPU acceleration (requires NVIDIA GPU + Microsoft.ML.OnnxRuntime.Gpu NuGet).</summary>
    Cuda = 1,

    /// <summary>DirectML GPU acceleration (Windows, AMD/Intel/NVIDIA + Microsoft.ML.OnnxRuntime.DirectML NuGet).</summary>
    DirectML = 2
}
