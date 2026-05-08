using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;
using System.Text;

Console.WriteLine("=== ElBruno.Text2Image - Scenario 17: Model Comparison Grid ===");
Console.WriteLine();

const string prompt = "a futuristic city at sunset, digital art";
const int seed = 42;
const int width = 512;
const int height = 512;

Console.WriteLine($"Prompt: \"{prompt}\"");
Console.WriteLine($"Seed: {seed}");
Console.WriteLine($"Resolution: {width}x{height}");
Console.WriteLine();

var scenarios = new (string ModelName, Func<IImageGenerator> CreateGenerator, ImageGenerationOptions Options, string OutputFile, string ModelSize)[]
{
    ("StableDiffusion15", () => new StableDiffusion15(), new ImageGenerationOptions
    {
        NumInferenceSteps = 20, GuidanceScale = 7.5, Seed = seed, Width = width, Height = height
    }, "sd15.png", "~4 GB"),
    ("LcmDreamshaperV7", () => new LcmDreamshaperV7(), new ImageGenerationOptions
    {
        NumInferenceSteps = 4, GuidanceScale = 1.0, Seed = seed, Width = width, Height = height
    }, "lcm.png", "~4 GB"),
    ("SdxlTurbo", () => new SdxlTurbo(), new ImageGenerationOptions
    {
        NumInferenceSteps = 4, GuidanceScale = 0.0, Seed = seed, Width = width, Height = height
    }, "sdxl-turbo.png", "~8 GB")
};

var rows = new List<(string ModelName, long InferenceTimeMs, int Steps, string ModelSize, string OutputFile)>();

foreach (var scenario in scenarios)
{
    Console.WriteLine($"--- {scenario.ModelName} ---");
    using var generator = scenario.CreateGenerator();

    Console.Write("Ensuring model is available... ");
    await generator.EnsureModelAvailableAsync();
    Console.WriteLine("ready");

    Console.Write("Generating image... ");
    var result = await generator.GenerateAsync(prompt, scenario.Options);
    await result.SaveAsync(scenario.OutputFile);
    Console.WriteLine($"done ({result.InferenceTimeMs}ms)");
    Console.WriteLine($"Saved: {Path.GetFullPath(scenario.OutputFile)}");
    Console.WriteLine();

    rows.Add((scenario.ModelName, result.InferenceTimeMs, scenario.Options.NumInferenceSteps, scenario.ModelSize, scenario.OutputFile));
}

var readme = new StringBuilder()
    .AppendLine("# Scenario 17: Side-by-side model comparison grid")
    .AppendLine()
    .AppendLine("Prompt used for all models:")
    .AppendLine()
    .AppendLine($"> {prompt}")
    .AppendLine()
    .AppendLine("| Model | Inference time (ms) | Steps used | Model size | Output |")
    .AppendLine("|---|---:|---:|---|---|");

foreach (var row in rows)
{
    readme.AppendLine($"| {row.ModelName} | {row.InferenceTimeMs} | {row.Steps} | {row.ModelSize} | `{row.OutputFile}` |");
}

await File.WriteAllTextAsync("README.md", readme.ToString());
Console.WriteLine($"README updated: {Path.GetFullPath("README.md")}");
