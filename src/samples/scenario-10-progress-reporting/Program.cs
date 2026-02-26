using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Download Progress Reporting ===");
Console.WriteLine();

// This sample shows detailed download progress for first-run model setup.
// On subsequent runs, the model is already cached and no download occurs.

using var generator = new StableDiffusion15();

Console.WriteLine("Starting model download with detailed progress...");
Console.WriteLine();

var downloadStart = DateTime.Now;
await generator.EnsureModelAvailableAsync(
    new Progress<DownloadProgress>(p =>
    {
        switch (p.Stage)
        {
            case DownloadStage.Downloading:
                // Build a simple progress bar
                var barLength = 30;
                var filled = (int)(p.PercentComplete / 100.0 * barLength);
                var bar = new string('█', filled) + new string('░', barLength - filled);
                Console.Write($"\r  [{bar}] {p.PercentComplete,5:F1}% - {p.CurrentFile ?? ""}   ");
                break;

            case DownloadStage.Complete:
                Console.WriteLine();
                Console.WriteLine($"  Download complete!");
                break;

            default:
                if (p.Message != null)
                    Console.WriteLine($"  {p.Stage}: {p.Message}");
                break;
        }
    }));

var elapsed = DateTime.Now - downloadStart;
Console.WriteLine($"  Total time: {elapsed.TotalSeconds:F1}s");
Console.WriteLine();

// Quick generation to verify the model works
Console.WriteLine("Generating a quick test image...");
var result = await generator.GenerateAsync("a simple red circle on a white background", new ImageGenerationOptions
{
    NumInferenceSteps = 10,
    Width = 512,
    Height = 512
});

await result.SaveAsync("progress_test.png");
Console.WriteLine($"Test image saved ({result.InferenceTimeMs}ms)");
