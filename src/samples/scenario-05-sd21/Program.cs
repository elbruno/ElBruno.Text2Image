using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Stable Diffusion 2.1 ===");
Console.WriteLine();

// SD 2.1 uses OpenCLIP ViT-H (1024-dim embeddings) and supports 768x768 natively
using var generator = new StableDiffusion21();

Console.WriteLine("Ensuring model is available (SD 2.1 is ~4.9 GB, first download takes a while)...");
await generator.EnsureModelAvailableAsync(
    new Progress<DownloadProgress>(p =>
    {
        if (p.CurrentFile != null)
            Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
    }));
Console.WriteLine();
Console.WriteLine("Model ready!");
Console.WriteLine();

// SD 2.1 defaults to 768x768 for best quality
var prompt = "a majestic eagle soaring over snow-capped mountains at golden hour, photorealistic";
Console.WriteLine($"Generating at 768x768 (SD 2.1 native resolution)...");
Console.WriteLine($"Prompt: \"{prompt}\"");

var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
{
    NumInferenceSteps = 25,
    GuidanceScale = 7.5,
    Width = 768,
    Height = 768,
    Seed = 42
});

var outputPath = "sd21_output.png";
await result.SaveAsync(outputPath);
Console.WriteLine();
Console.WriteLine($"Image saved to: {Path.GetFullPath(outputPath)}");
Console.WriteLine($"Seed: {result.Seed}");
Console.WriteLine($"Inference time: {result.InferenceTimeMs}ms");
