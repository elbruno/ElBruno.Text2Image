using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Custom Options Demo ===");
Console.WriteLine();

using var generator = new StableDiffusion15();

Console.WriteLine("Ensuring model is available...");
await generator.EnsureModelAvailableAsync();
Console.WriteLine("Model ready!");
Console.WriteLine();

// Generate with different seeds to show reproducibility
var prompt = "a futuristic cyberpunk cityscape at night, neon lights, rain";

var seeds = new[] { 42, 123, 999 };
foreach (var seed in seeds)
{
    Console.WriteLine($"Generating with seed {seed}...");
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        NumInferenceSteps = 20,
        GuidanceScale = 7.5,
        Width = 512,
        Height = 512,
        Seed = seed
    });

    var path = $"output_seed_{seed}.png";
    await result.SaveAsync(path);
    Console.WriteLine($"  Saved to {path} ({result.InferenceTimeMs}ms)");
}

// Generate with different guidance scales
Console.WriteLine();
Console.WriteLine("Generating with different guidance scales...");
var scales = new[] { 3.0, 7.5, 15.0 };
foreach (var scale in scales)
{
    Console.WriteLine($"  Guidance scale: {scale}");
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        NumInferenceSteps = 15,
        GuidanceScale = scale,
        Seed = 42
    });

    var path = $"output_cfg_{scale:F1}.png";
    await result.SaveAsync(path);
    Console.WriteLine($"  Saved to {path}");
}

Console.WriteLine();
Console.WriteLine("Done! Compare the generated images to see the effect of different settings.");
