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
    Console.WriteLine("   Install a GPU-enabled package instead:");
    Console.WriteLine();
    Console.WriteLine("   NVIDIA CUDA:");
    Console.WriteLine("     dotnet add package ElBruno.Text2Image.Cuda");
    Console.WriteLine();
    Console.WriteLine("   DirectML (AMD / Intel / NVIDIA on Windows):");
    Console.WriteLine("     dotnet add package ElBruno.Text2Image.DirectML");
}
else if (detected == ExecutionProvider.Cpu && providers.Contains("CUDAExecutionProvider"))
{
    Console.WriteLine("⚠️  CUDA provider is listed but failed to initialize.");
    Console.WriteLine("   Install CUDA runtime libraries:");
    Console.WriteLine("     pip install nvidia-cublas-cu12 nvidia-cudnn-cu12 nvidia-cufft-cu12 \\");
    Console.WriteLine("       nvidia-curand-cu12 nvidia-cusolver-cu12 nvidia-cusparse-cu12 \\");
    Console.WriteLine("       nvidia-cuda-runtime-cu12 nvidia-cuda-nvrtc-cu12");
    Console.WriteLine("   Then add the installed DLL directories to PATH.");
    Console.WriteLine("   See docs/gpu-acceleration.md for details.");
}
else if (detected != ExecutionProvider.Cpu)
{
    Console.WriteLine($"🚀 GPU acceleration is active ({detected}).");
}

Console.WriteLine();
Console.WriteLine("Done.");
