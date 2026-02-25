using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ElBruno.Text2Image;

/// <summary>
/// Preprocesses images for ONNX model input (resize, normalize, to tensor).
/// </summary>
internal static class ImagePreprocessor
{
    /// <summary>
    /// Preprocesses an image for ViT models (224×224, mean=0.5, std=0.5).
    /// </summary>
    public static float[] PreprocessForViT(string imagePath)
    {
        using var image = Image.Load<Rgb24>(imagePath);
        return Preprocess(image, 224, [0.5f, 0.5f, 0.5f], [0.5f, 0.5f, 0.5f]);
    }

    /// <summary>
    /// Preprocesses an image for ViT models from a stream.
    /// </summary>
    public static float[] PreprocessForViTFromStream(Stream stream)
    {
        using var image = Image.Load<Rgb24>(stream);
        return Preprocess(image, 224, [0.5f, 0.5f, 0.5f], [0.5f, 0.5f, 0.5f]);
    }

    /// <summary>
    /// Preprocesses an image for BLIP models (384×384, ImageNet normalization).
    /// </summary>
    public static float[] PreprocessForBlip(string imagePath)
    {
        using var image = Image.Load<Rgb24>(imagePath);
        return Preprocess(image, 384, [0.48145466f, 0.4578275f, 0.40821073f], [0.26862954f, 0.26130258f, 0.27577711f]);
    }

    /// <summary>
    /// Preprocesses an image for BLIP models from a stream.
    /// </summary>
    public static float[] PreprocessForBlipFromStream(Stream stream)
    {
        using var image = Image.Load<Rgb24>(stream);
        return Preprocess(image, 384, [0.48145466f, 0.4578275f, 0.40821073f], [0.26862954f, 0.26130258f, 0.27577711f]);
    }

    /// <summary>
    /// Generic preprocessing with configurable normalization.
    /// Returns a float array in NCHW format [1, 3, height, width].
    /// </summary>
    public static float[] Preprocess(Image<Rgb24> image, int targetSize, float[] mean, float[] std)
    {
        var (resizeW, resizeH) = GetResizeDimensions(image.Width, image.Height, targetSize);
        image.Mutate(x => x
            .Resize(resizeW, resizeH)
            .Crop(new Rectangle((resizeW - targetSize) / 2, (resizeH - targetSize) / 2, targetSize, targetSize)));

        var tensor = new float[1 * 3 * targetSize * targetSize];
        var channelSize = targetSize * targetSize;

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < targetSize; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < targetSize; x++)
                {
                    var pixel = row[x];
                    var idx = y * targetSize + x;
                    tensor[0 * channelSize + idx] = (pixel.R / 255f - mean[0]) / std[0];
                    tensor[1 * channelSize + idx] = (pixel.G / 255f - mean[1]) / std[1];
                    tensor[2 * channelSize + idx] = (pixel.B / 255f - mean[2]) / std[2];
                }
            }
        });

        return tensor;
    }

    private static (int width, int height) GetResizeDimensions(int originalW, int originalH, int targetSize)
    {
        if (originalW < originalH)
        {
            var scale = (float)targetSize / originalW;
            return (targetSize, (int)(originalH * scale));
        }
        else
        {
            var scale = (float)targetSize / originalH;
            return ((int)(originalW * scale), targetSize);
        }
    }
}
