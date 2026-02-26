using ElBruno.Text2Image;
using Microsoft.ML.OnnxRuntime;

Console.WriteLine("=== ElBruno.Text2Image - GPU / CPU Diagnostics ===");
Console.WriteLine();

// 1. Show all ONNX Runtime providers registered in this build
Console.WriteLine("── ONNX Runtime Available Providers ──");
var providers = OrtEnv.Instance().GetAvailableProviders();
foreach (var p in providers)
{
    var icon = p switch
    {
        "CUDAExecutionProvider" => "🟢 (NVIDIA CUDA)",
        "DmlExecutionProvider" => "🟢 (DirectML / DirectX 12)",
        "CPUExecutionProvider" => "⚪ (CPU fallback)",
        _ => "🔵"
    };
    Console.WriteLine($"  • {p}  {icon}");
}
Console.WriteLine();

// 2. Which NuGet package is loaded?
var ortAssembly = typeof(SessionOptions).Assembly;
Console.WriteLine("── OnnxRuntime Assembly Info ──");
Console.WriteLine($"  Assembly : {ortAssembly.GetName().Name}");
Console.WriteLine($"  Version  : {ortAssembly.GetName().Version}");
Console.WriteLine($"  Location : {ortAssembly.Location}");
Console.WriteLine();

// 3. Auto-detection result
Console.WriteLine("── Auto-Detection Result ──");
var detected = SessionOptionsHelper.DetectBestProvider();
Console.WriteLine($"  DetectBestProvider() → {detected}");
Console.WriteLine();

// 4. Try each provider explicitly and report success/failure
Console.WriteLine("── Provider Probe Results ──");
foreach (var ep in new[] { ExecutionProvider.Cuda, ExecutionProvider.DirectML, ExecutionProvider.Cpu })
{
    try
    {
        using var opts = SessionOptionsHelper.Create(ep);
        Console.WriteLine($"  {ep,-10} → ✅ OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {ep,-10} → ❌ {ex.GetType().Name}: {ex.Message[..Math.Min(80, ex.Message.Length)]}");
    }
}
Console.WriteLine();

// 5. Guidance
if (detected == ExecutionProvider.Cpu && !providers.Contains("CUDAExecutionProvider") && !providers.Contains("DmlExecutionProvider"))
{
    Console.WriteLine("⚠️  No GPU provider is available.");
    Console.WriteLine("   To enable GPU acceleration, replace the OnnxRuntime NuGet package:");
    Console.WriteLine();
    Console.WriteLine("   NVIDIA CUDA (recommended for NVIDIA GPUs):");
    Console.WriteLine("     dotnet remove package Microsoft.ML.OnnxRuntime");
    Console.WriteLine("     dotnet add package Microsoft.ML.OnnxRuntime.Gpu");
    Console.WriteLine();
    Console.WriteLine("   DirectML (AMD / Intel / NVIDIA on Windows):");
    Console.WriteLine("     dotnet remove package Microsoft.ML.OnnxRuntime");
    Console.WriteLine("     dotnet add package Microsoft.ML.OnnxRuntime.DirectML");
}
else if (detected != ExecutionProvider.Cpu)
{
    Console.WriteLine($"🚀 GPU acceleration is active ({detected}).");
}

Console.WriteLine();
Console.WriteLine("Done.");
