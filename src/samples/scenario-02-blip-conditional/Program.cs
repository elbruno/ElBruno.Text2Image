using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - BLIP Conditional Captioning ===");
Console.WriteLine();

var imagePath = args.Length > 0 ? args[0] : null;
if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
{
    Console.WriteLine("Usage: scenario-02-blip-conditional <image-path> [text-prompt]");
    Console.WriteLine("Example: scenario-02-blip-conditional photo.jpg \"a photography of\"");
    return;
}

var textPrompt = args.Length > 1 ? args[1] : null;

using var captioner = new BlipCaptioner();

Console.WriteLine("Downloading BLIP model (if needed)...");
await captioner.EnsureModelAvailableAsync(new Progress<DownloadProgress>(p =>
{
    if (!string.IsNullOrEmpty(p.CurrentFile))
        Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
}));
Console.WriteLine();

// Unconditional captioning
Console.WriteLine($"Captioning: {imagePath}");
var result = await captioner.CaptionAsync(imagePath);
Console.WriteLine($"Unconditional caption: {result.Caption}");
Console.WriteLine($"Inference time: {result.InferenceTimeMs}ms");

// Conditional captioning (with text prompt)
if (!string.IsNullOrEmpty(textPrompt))
{
    Console.WriteLine();
    Console.WriteLine($"Conditional caption (prompt: \"{textPrompt}\"):");
    var condResult = await captioner.CaptionAsync(imagePath, new ImageCaptionerOptions { TextPrompt = textPrompt });
    Console.WriteLine($"Caption: {condResult.Caption}");
    Console.WriteLine($"Inference time: {condResult.InferenceTimeMs}ms");
}
