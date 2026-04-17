using System.Diagnostics;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;
using Microsoft.ML.OnnxRuntime;

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║    ElBruno.Text2Image — GPU Stress Test & Metrics           ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// ── Environment & Provider Info ──
var providers = OrtEnv.Instance().GetAvailableProviders();
var detected = SessionOptionsHelper.DetectBestProvider();

Console.WriteLine("── Environment ──");
Console.WriteLine($"  OS            : {Environment.OSVersion}");
Console.WriteLine($"  Processors    : {Environment.ProcessorCount}");
Console.WriteLine($"  64-bit OS     : {Environment.Is64BitOperatingSystem}");
Console.WriteLine($"  .NET Runtime  : {Environment.Version}");
Console.WriteLine();
Console.WriteLine("── ONNX Runtime ──");
Console.WriteLine($"  Version       : {typeof(SessionOptions).Assembly.GetName().Version}");
Console.WriteLine($"  Providers     : {string.Join(", ", providers)}");
Console.WriteLine($"  Selected      : {detected}");
Console.WriteLine();

// ── Stress Test Configuration ──
// Use the heaviest local models at the largest supported resolutions
// with high inference steps to maximize GPU load.
var stressRuns = new (string Label, Func<IImageGenerator> Factory, ImageGenerationOptions Options)[]
{
    // SDXL Turbo: XL-class architecture, normally runs 1-4 steps.
    // Cranking to 50 steps at 1024x1024 forces the large UNet through many iterations.
    ("SDXL Turbo — 1024×1024 × 50 steps", () => new SdxlTurbo(), new ImageGenerationOptions
    {
        NumInferenceSteps = 50,
        GuidanceScale = 0.0,
        Width = 1024,
        Height = 1024,
        Seed = 42
    }),

    // SD 2.1: 1024-dim OpenCLIP encoder, largest embedding model.
    // Running at 1024x1024 with 50 steps at high guidance for heavy VRAM + compute.
    ("SD 2.1 — 1024×1024 × 50 steps", () => new StableDiffusion21(), new ImageGenerationOptions
    {
        NumInferenceSteps = 50,
        GuidanceScale = 12.0,
        Width = 1024,
        Height = 1024,
        Seed = 42
    }),

    // SD 2.1 at extreme resolution: 1536x1536 pushes VRAM to the limit.
    ("SD 2.1 — 1536×1536 × 30 steps (extreme)", () => new StableDiffusion21(), new ImageGenerationOptions
    {
        NumInferenceSteps = 30,
        GuidanceScale = 10.0,
        Width = 1536,
        Height = 1536,
        Seed = 42
    }),

    // SDXL Turbo at 100 steps — pure compute stress, lots of UNet iterations.
    ("SDXL Turbo — 768×768 × 100 steps", () => new SdxlTurbo(), new ImageGenerationOptions
    {
        NumInferenceSteps = 100,
        GuidanceScale = 0.0,
        Width = 768,
        Height = 768,
        Seed = 42
    }),
};

var prompts = new[]
{
    "a hyper-detailed mechanical clockwork city floating in space, intricate gears and pipes, golden light, volumetric fog, 8k render",
    "a photorealistic portrait of a cybernetic warrior in neon-lit rain, cinematic lighting, dramatic shadows, ultra high detail",
};

var outputDir = "stress_output";
Directory.CreateDirectory(outputDir);

Console.WriteLine("── Stress Test Plan ──");
Console.WriteLine($"  Runs          : {stressRuns.Length}");
Console.WriteLine($"  Prompts/run   : {prompts.Length}");
Console.WriteLine($"  Total images  : {stressRuns.Length * prompts.Length}");
Console.WriteLine();

// ── Collect Metrics ──
var allMetrics = new List<(string Run, string Prompt, long InferenceMs, long TotalMs, long PeakMemoryMB, int Width, int Height, int Steps)>();
var overallStopwatch = Stopwatch.StartNew();
var processAtStart = Process.GetCurrentProcess();
var startMemory = processAtStart.WorkingSet64;

for (int r = 0; r < stressRuns.Length; r++)
{
    var (label, factory, options) = stressRuns[r];

    Console.WriteLine($"━━━ Run {r + 1}/{stressRuns.Length}: {label} ━━━");

    using var generator = factory();

    Console.Write("  Downloading model... ");
    await generator.EnsureModelAvailableAsync(
        new Progress<DownloadProgress>(p =>
        {
            if (p.CurrentFile != null)
                Console.Write($"\r  Downloading: {p.CurrentFile} ({p.PercentComplete:F0}%)   ");
        }));
    Console.WriteLine("ready!");

    for (int p = 0; p < prompts.Length; p++)
    {
        var prompt = prompts[p];
        var shortPrompt = prompt.Length > 60 ? prompt[..60] + "…" : prompt;
        Console.Write($"  [{p + 1}/{prompts.Length}] \"{shortPrompt}\"");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memBefore = Process.GetCurrentProcess().WorkingSet64;
        var sw = Stopwatch.StartNew();

        var result = await generator.GenerateAsync(prompt, options);

        sw.Stop();
        var memAfter = Process.GetCurrentProcess().WorkingSet64;
        var peakMB = Math.Max(memAfter, memBefore) / (1024 * 1024);

        var filename = Path.Combine(outputDir, $"stress_r{r + 1}_p{p + 1}.png");
        await result.SaveAsync(filename);

        allMetrics.Add((label, shortPrompt, result.InferenceTimeMs, sw.ElapsedMilliseconds, peakMB, options.Width, options.Height, options.NumInferenceSteps));

        Console.WriteLine($" → {result.InferenceTimeMs}ms inference, {sw.ElapsedMilliseconds}ms total, ~{peakMB} MB");
    }

    Console.WriteLine();
}

overallStopwatch.Stop();

// ── Metrics Summary ──
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    METRICS SUMMARY                          ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

Console.WriteLine($"  Provider           : {detected}");
Console.WriteLine($"  Total wall time    : {overallStopwatch.Elapsed.TotalSeconds:F1}s");
Console.WriteLine($"  Total images       : {allMetrics.Count}");
Console.WriteLine($"  Avg inference time : {allMetrics.Average(m => m.InferenceMs):F0}ms");
Console.WriteLine($"  Min inference time : {allMetrics.Min(m => m.InferenceMs)}ms");
Console.WriteLine($"  Max inference time : {allMetrics.Max(m => m.InferenceMs)}ms");
Console.WriteLine($"  Peak process memory: {allMetrics.Max(m => m.PeakMemoryMB)} MB");
Console.WriteLine();

Console.WriteLine("── Per-Run Breakdown ──");
Console.WriteLine($"  {"Run",-45} {"Res",-12} {"Steps",-7} {"Avg(ms)",-10} {"Max(ms)",-10} {"Mem(MB)",-10}");
Console.WriteLine($"  {new string('─', 94)}");

var grouped = allMetrics.GroupBy(m => m.Run);
foreach (var group in grouped)
{
    var first = group.First();
    Console.WriteLine($"  {group.Key,-45} {first.Width}×{first.Height,-5} {first.Steps,-7} {group.Average(m => m.InferenceMs),-10:F0} {group.Max(m => m.InferenceMs),-10} {group.Max(m => m.PeakMemoryMB),-10}");
}
Console.WriteLine();

// ── Throughput Metrics ──
Console.WriteLine("── Throughput ──");
var totalPixels = allMetrics.Sum(m => (long)m.Width * m.Height);
var totalSteps = allMetrics.Sum(m => m.Steps);
var totalInferenceSec = allMetrics.Sum(m => m.InferenceMs) / 1000.0;
Console.WriteLine($"  Total pixels generated : {totalPixels:N0}");
Console.WriteLine($"  Total inference steps  : {totalSteps:N0}");
Console.WriteLine($"  Steps/sec (inference)  : {totalSteps / totalInferenceSec:F2}");
Console.WriteLine($"  Megapixels/sec         : {totalPixels / totalInferenceSec / 1_000_000.0:F4}");
Console.WriteLine();

Console.WriteLine($"  Output directory       : {Path.GetFullPath(outputDir)}");
Console.WriteLine();
Console.WriteLine("Done. GPU stress test complete.");
