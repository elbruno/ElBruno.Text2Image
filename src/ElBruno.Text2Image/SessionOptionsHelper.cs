using Microsoft.ML.OnnxRuntime;

namespace ElBruno.Text2Image;

/// <summary>
/// Helper to configure ONNX Runtime session options based on the selected execution provider.
/// </summary>
internal static class SessionOptionsHelper
{
    public static SessionOptions Create(ExecutionProvider provider)
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
}
