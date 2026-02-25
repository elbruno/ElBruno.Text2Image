using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Compare Models ===");
Console.WriteLine();

var imagePath = args.Length > 0 ? args[0] : null;
if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
{
    Console.WriteLine("Usage: scenario-04-compare-models <image-path>");
    Console.WriteLine("Example: scenario-04-compare-models photo.jpg");
    return;
}

Console.WriteLine($"Image: {imagePath}");
Console.WriteLine(new string('-', 60));

// Compare ViT-GPT2 and BLIP
var captioners = new IImageCaptioner[]
{
    new ViTGpt2Captioner(),
    new BlipCaptioner()
};

foreach (var captioner in captioners)
{
    try
    {
        Console.WriteLine($"\n[{captioner.ModelName}]");
        Console.Write("  Downloading model...");
        await captioner.EnsureModelAvailableAsync();
        Console.WriteLine(" done.");

        var result = await captioner.CaptionAsync(imagePath);
        Console.WriteLine($"  Caption: {result.Caption}");
        Console.WriteLine($"  Time: {result.InferenceTimeMs}ms");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
    }
    finally
    {
        captioner.Dispose();
    }
}

Console.WriteLine(new string('-', 60));
Console.WriteLine("Done!");
