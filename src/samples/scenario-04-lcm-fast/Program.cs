using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - LCM Fast Generation (2-4 steps) ===");
Console.WriteLine();

// LCM Dreamshaper v7 uses Latent Consistency Models for ultra-fast inference
// It works best with 2-8 steps and NO classifier-free guidance (scale = 1.0)
using var generator = new LcmDreamshaperV7();

Console.WriteLine("Ensuring model is available...");
await generator.EnsureModelAvailableAsync(
    new Progress<DownloadProgress>(p =>
    {
        if (p.CurrentFile != null)
            Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
    }));
Console.WriteLine();
Console.WriteLine("Model ready!");
Console.WriteLine();

var prompt = "a cute robot painting on a canvas in a sunny studio, digital art";

// Generate with just 4 steps — LCM's sweet spot
Console.WriteLine($"Generating with 4 steps (LCM default)...");
var sw = System.Diagnostics.Stopwatch.StartNew();
var result = await generator.GenerateAsync(prompt);
sw.Stop();

var path = "lcm_4steps.png";
await result.SaveAsync(path);
Console.WriteLine($"  Saved to {path}");
Console.WriteLine($"  Inference time: {result.InferenceTimeMs}ms");
Console.WriteLine();

// Compare with 2 steps — even faster but lower quality
Console.WriteLine("Generating with 2 steps (fastest)...");
var result2 = await generator.GenerateAsync(prompt, new ImageGenerationOptions
{
    NumInferenceSteps = 2,
    GuidanceScale = 1.0,
    Seed = 42
});
await result2.SaveAsync("lcm_2steps.png");
Console.WriteLine($"  Saved to lcm_2steps.png");
Console.WriteLine($"  Inference time: {result2.InferenceTimeMs}ms");
Console.WriteLine();

Console.WriteLine("Done! LCM models trade some quality for dramatically faster generation.");
