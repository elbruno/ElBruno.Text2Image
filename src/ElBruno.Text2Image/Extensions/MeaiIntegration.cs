using System.Drawing;
using Microsoft.Extensions.AI;

namespace ElBruno.Text2Image;

/// <summary>
/// Well-known keys for additional properties used in <see cref="Microsoft.Extensions.AI.ImageGenerationOptions.AdditionalProperties"/>.
/// </summary>
public static class Text2ImagePropertyNames
{
    /// <summary>Number of denoising inference steps (int).</summary>
    public const string NumInferenceSteps = "num_inference_steps";

    /// <summary>Classifier-free guidance scale (double).</summary>
    public const string GuidanceScale = "guidance_scale";

    /// <summary>Random seed for reproducible generation (int).</summary>
    public const string Seed = "seed";

    /// <summary>ONNX Runtime execution provider (string: "Auto", "Cpu", "Cuda", "DirectML").</summary>
    public const string ExecutionProvider = "execution_provider";

    /// <summary>Local model directory override (string).</summary>
    public const string ModelDirectory = "model_directory";
}

/// <summary>
/// Helpers for converting between ElBruno.Text2Image options and Microsoft.Extensions.AI options.
/// </summary>
public static class ImageGenerationOptionsConverter
{
    /// <summary>
    /// Converts M.E.AI <see cref="Microsoft.Extensions.AI.ImageGenerationOptions"/> to our <see cref="ImageGenerationOptions"/>.
    /// </summary>
    public static ImageGenerationOptions FromMeaiOptions(Microsoft.Extensions.AI.ImageGenerationOptions? meaiOptions)
    {
        var options = new ImageGenerationOptions();
        if (meaiOptions == null) return options;

        if (meaiOptions.ImageSize.HasValue)
        {
            options.Width = meaiOptions.ImageSize.Value.Width;
            options.Height = meaiOptions.ImageSize.Value.Height;
        }

        if (meaiOptions.AdditionalProperties != null)
        {
            if (meaiOptions.AdditionalProperties.TryGetValue(Text2ImagePropertyNames.NumInferenceSteps, out var steps) && steps is int s)
                options.NumInferenceSteps = s;

            if (meaiOptions.AdditionalProperties.TryGetValue(Text2ImagePropertyNames.GuidanceScale, out var guidance) && guidance is double g)
                options.GuidanceScale = g;

            if (meaiOptions.AdditionalProperties.TryGetValue(Text2ImagePropertyNames.Seed, out var seed) && seed is int seedVal)
                options.Seed = seedVal;

            if (meaiOptions.AdditionalProperties.TryGetValue(Text2ImagePropertyNames.ExecutionProvider, out var ep))
            {
                if (ep is ExecutionProvider epEnum)
                    options.ExecutionProvider = epEnum;
                else if (ep is string epStr && Enum.TryParse<ExecutionProvider>(epStr, true, out var parsed))
                    options.ExecutionProvider = parsed;
            }

            if (meaiOptions.AdditionalProperties.TryGetValue(Text2ImagePropertyNames.ModelDirectory, out var dir) && dir is string dirStr)
                options.ModelDirectory = dirStr;
        }

        return options;
    }

    /// <summary>
    /// Converts our <see cref="ImageGenerationResult"/> to a M.E.AI <see cref="ImageGenerationResponse"/>.
    /// </summary>
    public static ImageGenerationResponse ToMeaiResponse(ImageGenerationResult result)
    {
        var content = new DataContent(result.ImageBytes, "image/png");
        return new ImageGenerationResponse(new List<AIContent> { content })
        {
            RawRepresentation = result
        };
    }
}
