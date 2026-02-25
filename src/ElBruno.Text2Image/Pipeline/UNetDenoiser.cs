using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ElBruno.Text2Image.Pipeline;

/// <summary>
/// Wraps the UNet ONNX model for iterative denoising in the diffusion pipeline.
/// </summary>
internal sealed class UNetDenoiser : IDisposable
{
    private readonly InferenceSession _session;

    public UNetDenoiser(string modelPath, SessionOptions sessionOptions)
    {
        _session = new InferenceSession(modelPath, sessionOptions);
    }

    /// <summary>
    /// Runs a single UNet denoising step.
    /// </summary>
    /// <param name="sample">Latent sample tensor [batch, 4, H/8, W/8].</param>
    /// <param name="timestep">Current timestep value.</param>
    /// <param name="encoderHiddenStates">Text embeddings [batch, 77, embeddingDim].</param>
    /// <returns>Predicted noise tensor [batch, 4, H/8, W/8].</returns>
    public DenseTensor<float> Predict(
        DenseTensor<float> sample,
        long timestep,
        DenseTensor<float> encoderHiddenStates)
    {
        var timestepTensor = new DenseTensor<long>(new long[] { timestep }, new int[] { 1 });

        var input = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("sample", sample),
            NamedOnnxValue.CreateFromTensor("timestep", timestepTensor),
            NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenStates)
        };

        using var output = _session.Run(input);
        var outputData = (output.First().Value as DenseTensor<float>)!;

        // Copy result to avoid disposal issues
        var result = new float[outputData.Length];
        outputData.Buffer.Span.CopyTo(result);
        return new DenseTensor<float>(result, outputData.Dimensions.ToArray());
    }

    public void Dispose() => _session.Dispose();
}
