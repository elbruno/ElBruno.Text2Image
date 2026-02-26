using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Custom Model Directory ===");
Console.WriteLine();

// By default, models are stored in %LOCALAPPDATA%\ElBruno\Text2Image\
// You can override this to store models in any location you want.
var customDir = Path.Combine(Directory.GetCurrentDirectory(), "my-models");
Console.WriteLine($"Custom model directory: {Path.GetFullPath(customDir)}");
Console.WriteLine();

// Pass the custom directory via ImageGenerationOptions
var options = new ImageGenerationOptions
{
    ModelDirectory = customDir,
    NumInferenceSteps = 15,
    GuidanceScale = 7.5,
    Width = 512,
    Height = 512,
    Seed = 42
};

using var generator = new StableDiffusion15(defaultOptions: options);

// The model will be downloaded to the custom directory
Console.WriteLine("Downloading model to custom directory...");
await generator.EnsureModelAvailableAsync(
    new Progress<DownloadProgress>(p =>
    {
        if (p.CurrentFile != null)
            Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
    }));
Console.WriteLine();
Console.WriteLine("Model ready!");

// Verify the model files are in the custom directory
var modelPath = Path.Combine(customDir, "stable-diffusion-v1-5-onnx");
if (Directory.Exists(modelPath))
{
    Console.WriteLine($"Model files stored at: {modelPath}");
    var files = Directory.GetFiles(modelPath, "*", SearchOption.AllDirectories);
    Console.WriteLine($"Total files: {files.Length}");
    var totalSize = files.Sum(f => new FileInfo(f).Length);
    Console.WriteLine($"Total size: {totalSize / (1024.0 * 1024.0):F1} MB");
}
Console.WriteLine();

// Generate an image using the model from the custom directory
var prompt = "a serene Japanese garden with cherry blossoms and a koi pond, watercolor style";
Console.WriteLine($"Generating image...");
Console.WriteLine($"Prompt: \"{prompt}\"");

var result = await generator.GenerateAsync(prompt, options);

var outputPath = "custom_dir_output.png";
await result.SaveAsync(outputPath);
Console.WriteLine();
Console.WriteLine($"Image saved to: {Path.GetFullPath(outputPath)}");
Console.WriteLine($"Inference time: {result.InferenceTimeMs}ms");
