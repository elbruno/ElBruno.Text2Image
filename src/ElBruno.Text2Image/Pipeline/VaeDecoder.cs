using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ElBruno.Text2Image.Pipeline;

/// <summary>
/// Wraps the VAE decoder ONNX model to convert latent representations into images.
/// </summary>
internal sealed class VaeDecoder : IDisposable
{
    private readonly InferenceSession _session;

    public VaeDecoder(string modelPath, SessionOptions sessionOptions)
    {
        _session = new InferenceSession(modelPath, sessionOptions);
    }

    /// <summary>
    /// Decodes latent tensor into an image.
    /// </summary>
    /// <param name="latents">Latent tensor [1, 4, H/8, W/8]. Must be pre-scaled (divided by 0.18215).</param>
    /// <param name="width">Output image width.</param>
    /// <param name="height">Output image height.</param>
    /// <returns>PNG image bytes.</returns>
    public byte[] Decode(DenseTensor<float> latents, int width, int height)
    {
        var input = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("latent_sample", latents)
        };

        using var output = _session.Run(input);
        var outputTensor = (output.First().Value as DenseTensor<float>)!;

        return ConvertToImage(outputTensor, width, height);
    }

    /// <summary>
    /// Converts VAE decoder output tensor [1, 3, H, W] to PNG bytes.
    /// </summary>
    private static byte[] ConvertToImage(DenseTensor<float> tensor, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var r = (byte)Math.Round(Math.Clamp((tensor[0, 0, y, x] / 2f + 0.5f), 0f, 1f) * 255f);
                    var g = (byte)Math.Round(Math.Clamp((tensor[0, 1, y, x] / 2f + 0.5f), 0f, 1f) * 255f);
                    var b = (byte)Math.Round(Math.Clamp((tensor[0, 2, y, x] / 2f + 0.5f), 0f, 1f) * 255f);
                    row[x] = new Rgba32(r, g, b, 255);
                }
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public void Dispose() => _session.Dispose();
}
