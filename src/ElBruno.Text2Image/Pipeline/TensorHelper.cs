using Microsoft.ML.OnnxRuntime.Tensors;

namespace ElBruno.Text2Image.Pipeline;

/// <summary>
/// Helper utilities for tensor operations used in the Stable Diffusion pipeline.
/// </summary>
internal static class TensorHelper
{
    /// <summary>
    /// Creates a DenseTensor from data and dimensions.
    /// </summary>
    public static DenseTensor<T> CreateTensor<T>(T[] data, int[] dimensions)
    {
        return new DenseTensor<T>(data, dimensions);
    }

    /// <summary>
    /// Generates random Gaussian latent samples using Box-Muller transform.
    /// </summary>
    public static DenseTensor<float> GenerateLatentSample(int height, int width, int seed, float initNoiseSigma)
    {
        var random = new Random(seed);
        var channels = 4;
        var dims = new int[] { 1, channels, height / 8, width / 8 };
        var length = dims[0] * dims[1] * dims[2] * dims[3];
        var data = new float[length];

        for (int i = 0; i < length; i++)
        {
            var u1 = random.NextDouble();
            var u2 = random.NextDouble();
            var radius = Math.Sqrt(-2.0 * Math.Log(u1));
            var theta = 2.0 * Math.PI * u2;
            data[i] = (float)(radius * Math.Cos(theta)) * initNoiseSigma;
        }

        return new DenseTensor<float>(data, dims);
    }

    /// <summary>
    /// Generates a random tensor with Gaussian distribution.
    /// </summary>
    public static DenseTensor<float> GetRandomTensor(int[] dimensions)
    {
        var random = new Random();
        var length = 1;
        foreach (var d in dimensions) length *= d;
        var data = new float[length];

        for (int i = 0; i < length; i++)
        {
            var u1 = random.NextDouble();
            var u2 = random.NextDouble();
            var radius = Math.Sqrt(-2.0 * Math.Log(u1));
            var theta = 2.0 * Math.PI * u2;
            data[i] = (float)(radius * Math.Cos(theta));
        }

        return new DenseTensor<float>(data, dimensions);
    }

    /// <summary>
    /// Duplicates tensor data for classifier-free guidance (batch of 2).
    /// </summary>
    public static DenseTensor<float> Duplicate(float[] data, int[] dimensions)
    {
        var doubled = new float[data.Length * 2];
        Array.Copy(data, 0, doubled, 0, data.Length);
        Array.Copy(data, 0, doubled, data.Length, data.Length);
        return new DenseTensor<float>(doubled, dimensions);
    }

    /// <summary>
    /// Splits a [2, C, H, W] tensor into two [1, C, H, W] tensors.
    /// </summary>
    public static (DenseTensor<float> first, DenseTensor<float> second) SplitTensor(
        DenseTensor<float> tensor, int channels, int height, int width)
    {
        var singleDims = new int[] { 1, channels, height, width };
        var singleLength = channels * height * width;
        var data = tensor.Buffer.Span;

        var first = new float[singleLength];
        var second = new float[singleLength];

        data.Slice(0, singleLength).CopyTo(first);
        data.Slice(singleLength, singleLength).CopyTo(second);

        return (new DenseTensor<float>(first, singleDims), new DenseTensor<float>(second, singleDims));
    }

    /// <summary>
    /// Applies classifier-free guidance: pred = uncond + scale * (cond - uncond).
    /// </summary>
    public static DenseTensor<float> ApplyGuidance(
        DenseTensor<float> noisePredUncond, DenseTensor<float> noisePredText, double guidanceScale)
    {
        var uncond = noisePredUncond.Buffer.Span;
        var text = noisePredText.Buffer.Span;
        var result = new float[uncond.Length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = uncond[i] + (float)guidanceScale * (text[i] - uncond[i]);
        }

        return new DenseTensor<float>(result, noisePredUncond.Dimensions.ToArray());
    }

    /// <summary>
    /// Multiplies all elements of a tensor by a scalar.
    /// </summary>
    public static DenseTensor<float> MultiplyByFloat(DenseTensor<float> tensor, float value)
    {
        var data = tensor.Buffer.ToArray();
        for (int i = 0; i < data.Length; i++)
            data[i] *= value;
        return new DenseTensor<float>(data, tensor.Dimensions.ToArray());
    }

    /// <summary>
    /// Divides all elements of a tensor by a scalar.
    /// </summary>
    public static DenseTensor<float> DivideByFloat(DenseTensor<float> tensor, float value)
    {
        var data = tensor.Buffer.ToArray();
        for (int i = 0; i < data.Length; i++)
            data[i] /= value;
        return new DenseTensor<float>(data, tensor.Dimensions.ToArray());
    }

    /// <summary>
    /// Adds two tensors element-wise.
    /// </summary>
    public static DenseTensor<float> AddTensors(DenseTensor<float> a, DenseTensor<float> b)
    {
        var dataA = a.Buffer.Span;
        var dataB = b.Buffer.Span;
        var result = new float[dataA.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = dataA[i] + dataB[i];
        return new DenseTensor<float>(result, a.Dimensions.ToArray());
    }

    /// <summary>
    /// Subtracts tensor b from tensor a element-wise.
    /// </summary>
    public static DenseTensor<float> SubtractTensors(DenseTensor<float> a, DenseTensor<float> b)
    {
        var dataA = a.Buffer.Span;
        var dataB = b.Buffer.Span;
        var result = new float[dataA.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = dataA[i] - dataB[i];
        return new DenseTensor<float>(result, a.Dimensions.ToArray());
    }
}
