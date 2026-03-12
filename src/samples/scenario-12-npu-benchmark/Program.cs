using System.Diagnostics;
using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;
using Microsoft.ML.OnnxRuntime;

Console.WriteLine("=== ElBruno.Text2Image - NPU Benchmark ===");
Console.WriteLine();

// ── Section 1: NPU Diagnostics ──────────────────────────────────────────────
Console.WriteLine("── ONNX Runtime Available Providers ──");
var providers = OrtEnv.Instance().GetAvailableProviders();
foreach (var p in providers)
{
    var icon = p switch
    {
        "QNNExecutionProvider" => "🟢 (Qualcomm QNN / Snapdragon X)",
        "OpenVINOExecutionProvider" => "🟢 (Intel OpenVINO / Core Ultra)",
        "CUDAExecutionProvider" => "🔵 (NVIDIA CUDA)",
        "DmlExecutionProvider" => "🔵 (DirectML)",
        "CPUExecutionProvider" => "⚪ (CPU fallback)",
        _ => "🔵"
    };
    Console.WriteLine($"  • {p}  {icon}");
}
Console.WriteLine();

var ortAssembly = typeof(SessionOptions).Assembly;
Console.WriteLine("── OnnxRuntime Assembly Info ──");
Console.WriteLine($"  Assembly : {ortAssembly.GetName().Name}");
Console.WriteLine($"  Version  : {ortAssembly.GetName().Version}");
Console.WriteLine();

Console.WriteLine("── Auto-Detection Result ──");
var detected = SessionOptionsHelper.DetectBestProvider();
Console.WriteLine($"  DetectBestProvider() → {detected}");
Console.WriteLine();

// ── Section 2: Provider Probe ────────────────────────────────────────────────
Console.WriteLine("── Provider Probe Results ──");
foreach (var ep in new[]
{
    ExecutionProvider.QualcommQnn,
    ExecutionProvider.IntelOpenVino,
    ExecutionProvider.DirectML,
    ExecutionProvider.Cuda,
    ExecutionProvider.Cpu
})
{
    try
    {
        using var opts = SessionOptionsHelper.Create(ep);
        Console.WriteLine($"  {ep,-15} → ✅ OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {ep,-15} → ❌ {ex.GetType().Name}: {ex.Message[..Math.Min(80, ex.Message.Length)]}");
    }
}
Console.WriteLine();

// ── Section 3: Provider Selection (Interactive) ─────────────────────────────
Console.WriteLine("── Provider Selection ──");
Console.WriteLine("  [0] Auto (recommended - uses best available)");
Console.WriteLine("  [1] QNN (Qualcomm Snapdragon X NPU)");
Console.WriteLine("  [2] OpenVINO (Intel Core Ultra NPU)");
Console.WriteLine("  [3] CPU");
Console.Write("Select execution provider [0]: ");

var providerInput = Console.ReadLine()?.Trim();
var selectedProvider = providerInput switch
{
    "1" => ExecutionProvider.QualcommQnn,
    "2" => ExecutionProvider.IntelOpenVino,
    "3" => ExecutionProvider.Cpu,
    _ => ExecutionProvider.Auto
};
var resolvedProvider = SessionOptionsHelper.ResolveProvider(selectedProvider);
Console.WriteLine($"  → Using: {resolvedProvider}");
Console.WriteLine();

// ── Section 4: Number of Images ─────────────────────────────────────────────
Console.Write("How many images to generate? (default: 3): ");
var countInput = Console.ReadLine()?.Trim();
var imageCount = int.TryParse(countInput, out var parsed) ? Math.Clamp(parsed, 1, 10) : 3;
Console.WriteLine($"  → Generating {imageCount} image(s)");
Console.WriteLine();

// ── Section 5: Image Generation Benchmark ───────────────────────────────────
Console.WriteLine("── Image Generation Benchmark ──");

using var generator = new StableDiffusion15();

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

var options = new ImageGenerationOptions
{
    ExecutionProvider = selectedProvider,
    NumInferenceSteps = 10,
    GuidanceScale = 7.5,
    Width = 512,
    Height = 512
};

var prompts = new[]
{
    "a serene mountain landscape at sunset, digital art",
    "a futuristic city skyline with neon lights",
    "a cute robot painting on a canvas, cartoon style",
    "an astronaut floating in space with Earth in background",
    "a cozy cabin in a snowy forest, warm lighting"
};

var timings = new List<long>();
var totalStopwatch = Stopwatch.StartNew();

for (var i = 0; i < imageCount; i++)
{
    var prompt = prompts[i % prompts.Length];
    Console.WriteLine($"  [{i + 1}/{imageCount}] \"{prompt}\"");

    var sw = Stopwatch.StartNew();
    var result = await generator.GenerateAsync(prompt, options);
    sw.Stop();

    var outputPath = $"npu_benchmark_{i + 1}.png";
    await result.SaveAsync(outputPath);

    timings.Add(sw.ElapsedMilliseconds);
    Console.WriteLine($"           ⏱  {sw.ElapsedMilliseconds}ms | Seed: {result.Seed} | Saved: {outputPath}");
}

totalStopwatch.Stop();
Console.WriteLine();

// ── Summary ──────────────────────────────────────────────────────────────────
Console.WriteLine("── Benchmark Summary ──");
Console.WriteLine($"  Provider       : {resolvedProvider}");
Console.WriteLine($"  Images         : {imageCount}");
Console.WriteLine($"  Total time     : {totalStopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"  Average / image: {timings.Average():F0}ms");
Console.WriteLine($"  Fastest        : {timings.Min()}ms");
Console.WriteLine($"  Slowest        : {timings.Max()}ms");
Console.WriteLine($"  Output folder  : {Path.GetFullPath(".")}");
Console.WriteLine();

// ── Section 6: NPU Guidance ─────────────────────────────────────────────────
var hasNpu = providers.Contains("QNNExecutionProvider") || providers.Contains("OpenVINOExecutionProvider");
var usedNpu = resolvedProvider is ExecutionProvider.QualcommQnn or ExecutionProvider.IntelOpenVino;

if (!hasNpu)
{
    Console.WriteLine("⚠️  No NPU provider is available.");
    Console.WriteLine("   For Qualcomm Snapdragon X devices:");
    Console.WriteLine("     dotnet add package ElBruno.Text2Image.Npu.Qualcomm");
    Console.WriteLine("   For Intel Core Ultra devices:");
    Console.WriteLine("     dotnet add package ElBruno.Text2Image.Npu.Intel");
}
else if (usedNpu)
{
    Console.WriteLine($"🚀 NPU acceleration is active ({resolvedProvider}).");
}

Console.WriteLine();
Console.WriteLine("Done.");
