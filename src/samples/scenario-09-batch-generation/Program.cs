using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Batch Generation ===");
Console.WriteLine();

using var generator = new StableDiffusion15();

Console.WriteLine("Ensuring model is available...");
await generator.EnsureModelAvailableAsync();
Console.WriteLine("Model ready!");
Console.WriteLine();

// Define a batch of prompts to generate
var prompts = new[]
{
    "a futuristic city skyline at sunset, cyberpunk, neon lights",
    "a peaceful meadow with wildflowers and butterflies, impressionist painting",
    "an astronaut floating in space with Earth in the background, photorealistic",
    "a steampunk clockwork dragon, intricate mechanical details, brass and copper",
    "a cozy coffee shop interior on a rainy day, warm lighting, watercolor"
};

// Create output directory
var outputDir = "batch_output";
Directory.CreateDirectory(outputDir);

var options = new ImageGenerationOptions
{
    NumInferenceSteps = 15,
    GuidanceScale = 7.5,
    Width = 512,
    Height = 512
};

Console.WriteLine($"Generating {prompts.Length} images...");
Console.WriteLine();

for (int i = 0; i < prompts.Length; i++)
{
    Console.WriteLine($"[{i + 1}/{prompts.Length}] \"{prompts[i][..Math.Min(60, prompts[i].Length)]}...\"");

    var result = await generator.GenerateAsync(prompts[i], options);

    var filename = Path.Combine(outputDir, $"batch_{i + 1:D2}.png");
    await result.SaveAsync(filename);
    Console.WriteLine($"  Saved to {filename} ({result.InferenceTimeMs}ms)");
}

Console.WriteLine();
Console.WriteLine($"Done! {prompts.Length} images saved to {Path.GetFullPath(outputDir)}");
