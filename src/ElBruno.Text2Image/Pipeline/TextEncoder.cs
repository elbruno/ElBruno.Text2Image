using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ElBruno.Text2Image.Pipeline;

/// <summary>
/// Wraps the CLIP text encoder ONNX model to convert token IDs into text embeddings.
/// </summary>
internal sealed class TextEncoder : IDisposable
{
    private readonly InferenceSession _session;

    public TextEncoder(string modelPath, SessionOptions sessionOptions)
    {
        _session = new InferenceSession(modelPath, sessionOptions);
    }

    /// <summary>
    /// Encodes token IDs into text embeddings.
    /// </summary>
    /// <param name="tokenIds">Token IDs array of shape [77].</param>
    /// <param name="embeddingDim">Embedding dimension (768 for SD 1.5, 1024 for SD 2.x).</param>
    /// <returns>Text embeddings tensor of shape [1, 77, embeddingDim].</returns>
    public DenseTensor<float> Encode(int[] tokenIds, int embeddingDim = 768)
    {
        var inputTensor = new DenseTensor<int>(tokenIds, new int[] { 1, tokenIds.Length });
        var input = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputTensor)
        };

        using var output = _session.Run(input);
        var lastHiddenState = (output.First().Value as IEnumerable<float>)!.ToArray();

        return TensorHelper.CreateTensor(lastHiddenState, new int[] { 1, 77, embeddingDim });
    }

    /// <summary>
    /// Encodes both conditional and unconditional text embeddings for classifier-free guidance.
    /// Returns a tensor of shape [2, 77, embeddingDim] with uncond at [0] and cond at [1].
    /// </summary>
    public DenseTensor<float> EncodeWithGuidance(int[] condTokens, int[] uncondTokens, int embeddingDim = 768)
    {
        var condEmbedding = Encode(condTokens, embeddingDim).Buffer.ToArray();
        var uncondEmbedding = Encode(uncondTokens, embeddingDim).Buffer.ToArray();

        var combined = new DenseTensor<float>(new int[] { 2, 77, embeddingDim });
        for (int i = 0; i < uncondEmbedding.Length; i++)
        {
            combined[0, i / embeddingDim, i % embeddingDim] = uncondEmbedding[i];
            combined[1, i / embeddingDim, i % embeddingDim] = condEmbedding[i];
        }

        return combined;
    }

    public void Dispose() => _session.Dispose();
}
