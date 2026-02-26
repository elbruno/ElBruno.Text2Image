using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

Console.WriteLine("=== ElBruno.Text2Image - Model Comparison ===");
Console.WriteLine();

var prompt = "a cozy cabin in the woods during autumn, warm lighting, oil painting style";
var seed = 42;

Console.WriteLine($"Prompt: \"{prompt}\"");
Console.WriteLine($"Seed: {seed}");
Console.WriteLine();

// Define the models to compare (all at 512x512 for fair comparison)
var models = new (string Name, IImageGenerator Generator, ImageGenerationOptions Options)[]
{
    ("SD 1.5 (20 steps)", new StableDiffusion15(), new ImageGenerationOptions
    {
        NumInferenceSteps = 20, GuidanceScale = 7.5, Seed = seed
    }),
    ("LCM Dreamshaper (4 steps)", new LcmDreamshaperV7(), new ImageGenerationOptions
    {
        NumInferenceSteps = 4, GuidanceScale = 1.0, Seed = seed
    })
};

foreach (var (name, generator, options) in models)
{
    Console.WriteLine($"--- {name} ---");
    Console.Write("  Downloading model... ");
    await generator.EnsureModelAvailableAsync();
    Console.WriteLine("ready!");

    Console.Write("  Generating... ");
    var result = await generator.GenerateAsync(prompt, options);

    var filename = $"comparison_{name.Replace(" ", "_").Replace("(", "").Replace(")", "")}.png";
    await result.SaveAsync(filename);
    Console.WriteLine($"done in {result.InferenceTimeMs}ms");
    Console.WriteLine($"  Saved to: {filename}");
    Console.WriteLine();

    generator.Dispose();
}

Console.WriteLine("Done! Compare the generated images to see quality/speed tradeoffs.");
Console.WriteLine("  - SD 1.5: Higher quality, slower (20 steps)");
Console.WriteLine("  - LCM: Much faster (4 steps), slightly lower quality");
