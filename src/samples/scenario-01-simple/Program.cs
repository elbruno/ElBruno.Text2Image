using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Simple Image Captioning ===");
Console.WriteLine();

// Check if an image path was provided
var imagePath = args.Length > 0 ? args[0] : null;
if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
{
    Console.WriteLine("Usage: scenario-01-simple <image-path>");
    Console.WriteLine("Example: scenario-01-simple photo.jpg");
    return;
}

// Create the ViT-GPT2 captioner (smallest, fastest model)
using var captioner = new ViTGpt2Captioner();

// Ensure model is downloaded (automatic on first use)
Console.WriteLine("Downloading model (if needed)...");
await captioner.EnsureModelAvailableAsync(new Progress<DownloadProgress>(p =>
{
    if (!string.IsNullOrEmpty(p.CurrentFile))
        Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
}));
Console.WriteLine();

// Generate caption
Console.WriteLine($"Captioning: {imagePath}");
var result = await captioner.CaptionAsync(imagePath);

Console.WriteLine();
Console.WriteLine($"Caption: {result.Caption}");
Console.WriteLine($"Model: {result.ModelName}");
Console.WriteLine($"Inference time: {result.InferenceTimeMs}ms");
