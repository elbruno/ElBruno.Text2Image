namespace ElBruno.Text2Image;

/// <summary>
/// ONNX Runtime execution provider selection.
/// </summary>
public enum ExecutionProvider
{
    /// <summary>CPU execution (default, works everywhere).</summary>
    Cpu = 0,

    /// <summary>CUDA GPU acceleration (requires NVIDIA GPU + CUDA toolkit).</summary>
    Cuda = 1,

    /// <summary>DirectML GPU acceleration (Windows, AMD/Intel/NVIDIA).</summary>
    DirectML = 2
}
