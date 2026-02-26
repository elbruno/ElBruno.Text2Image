using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Simple Text-to-Image Generation ===");
Console.WriteLine();

// Create a Stable Diffusion 1.5 generator (auto-detects GPU)
using var generator = new StableDiffusion15();

// Show where models will be stored
var modelDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ElBruno", "Text2Image", "stable-diffusion-v1-5-onnx");
Console.WriteLine($"Model location: {modelDir}");

// Show which execution provider will be used
var options = new ImageGenerationOptions
{
    NumInferenceSteps = 15,
    GuidanceScale = 7.5,
    Width = 512,
    Height = 512
};
var resolvedProvider = SessionOptionsHelper.ResolveProvider(options.ExecutionProvider);
Console.WriteLine($"Execution provider: {resolvedProvider} (requested: {options.ExecutionProvider})");
Console.WriteLine();

// Download the model if not already present
Console.WriteLine("Ensuring model is available (this may take a while on first run)...");
await generator.EnsureModelAvailableAsync(
    new Progress<DownloadProgress>(p =>
    {
        if (p.CurrentFile != null)
            Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
    }));
Console.WriteLine();
Console.WriteLine("Model ready!");
Console.WriteLine();

// Generate a small logo image (same prompt as scenario-03 cloud sample)
var prompt = "a simple flat icon of a paintbrush and a sparkle, purple and blue gradient, white background, minimal, square logo";
Console.WriteLine($"Generating image for prompt: \"{prompt}\"");
Console.WriteLine($"Generating with {resolvedProvider}...");

var result = await generator.GenerateAsync(prompt, options);

// Save the result
var outputPath = "generated_image.png";
await result.SaveAsync(outputPath);
Console.WriteLine();
Console.WriteLine($"Image saved to: {Path.GetFullPath(outputPath)}");
Console.WriteLine($"Seed: {result.Seed}");
Console.WriteLine($"Inference time: {result.InferenceTimeMs}ms");
