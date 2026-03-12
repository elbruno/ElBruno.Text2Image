namespace ElBruno.Text2Image;

/// <summary>
/// ONNX Runtime execution provider selection.
/// </summary>
public enum ExecutionProvider
{
    /// <summary>
    /// Automatically detect the best available provider.
    /// Probes in order: CUDA → DirectML → QNN → OpenVINO → CPU.
    /// </summary>
    Auto = -1,

    /// <summary>CPU execution (works everywhere).</summary>
    Cpu = 0,

    /// <summary>CUDA GPU acceleration (requires NVIDIA GPU + Microsoft.ML.OnnxRuntime.Gpu NuGet).</summary>
    Cuda = 1,

    /// <summary>DirectML GPU acceleration (Windows, AMD/Intel/NVIDIA + Microsoft.ML.OnnxRuntime.DirectML NuGet).</summary>
    DirectML = 2,

    /// <summary>QNN NPU acceleration (Qualcomm Snapdragon X + Microsoft.ML.OnnxRuntime.QNN).</summary>
    QualcommQnn = 3,

    /// <summary>Intel OpenVINO NPU acceleration (Intel Core Ultra + Intel.ML.OnnxRuntime.OpenVino).</summary>
    IntelOpenVino = 4
}
