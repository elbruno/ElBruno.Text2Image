# GPU Acceleration

ElBruno.Text2Image supports automatic GPU detection and acceleration using ONNX Runtime execution providers.

## Automatic Detection (Default)

By default, `ExecutionProvider` is set to `Auto`, which probes for the best available provider in this order:

1. **CUDA** (NVIDIA GPUs) — fastest, requires `Microsoft.ML.OnnxRuntime.Gpu`
2. **DirectML** (any GPU on Windows) — AMD, Intel, NVIDIA, requires `Microsoft.ML.OnnxRuntime.DirectML`
3. **CPU** — fallback, always available

```csharp
// Auto-detect is the default — no configuration needed
using var generator = new StableDiffusion15();
var result = await generator.GenerateAsync("a sunset over mountains");
```

## Installing GPU Packages

The base `ElBruno.Text2Image` NuGet package includes the CPU-only ONNX Runtime. To enable GPU acceleration, install the appropriate provider package **instead of** (not alongside) the CPU package.

### NVIDIA GPU (CUDA)

```bash
# Remove the CPU-only package (pulled in transitively)
dotnet remove package Microsoft.ML.OnnxRuntime

# Install the CUDA-enabled package
dotnet add package Microsoft.ML.OnnxRuntime.Gpu
```

**Requirements:**
- NVIDIA GPU (RTX 20xx or newer recommended)
- CUDA 12.x toolkit installed
- cuDNN 9.x installed

### Any GPU on Windows (DirectML)

```bash
# Remove the CPU-only package
dotnet remove package Microsoft.ML.OnnxRuntime

# Install the DirectML-enabled package
dotnet add package Microsoft.ML.OnnxRuntime.DirectML
```

**Requirements:**
- Windows 10/11
- Any DirectX 12 compatible GPU (AMD, Intel, NVIDIA)

## Explicit Provider Selection

You can override auto-detection by setting the `ExecutionProvider` in `ImageGenerationOptions`:

```csharp
// Force CPU
var result = await generator.GenerateAsync("prompt", new ImageGenerationOptions
{
    ExecutionProvider = ExecutionProvider.Cpu
});

// Force CUDA
var result = await generator.GenerateAsync("prompt", new ImageGenerationOptions
{
    ExecutionProvider = ExecutionProvider.Cuda
});

// Force DirectML
var result = await generator.GenerateAsync("prompt", new ImageGenerationOptions
{
    ExecutionProvider = ExecutionProvider.DirectML
});
```

## Checking Which Provider is Active

```csharp
var resolved = SessionOptionsHelper.ResolveProvider(ExecutionProvider.Auto);
Console.WriteLine($"Using: {resolved}"); // e.g., "Using: Cuda"
```

## Performance Comparison

Typical times for generating a 512×512 image with Stable Diffusion 1.5 (20 steps):

| Provider | Hardware | Approximate Time |
|----------|----------|-----------------|
| CPU | Modern desktop CPU | 30-60 seconds |
| CUDA | RTX 3060 | 3-5 seconds |
| CUDA | RTX 4090 | 1-2 seconds |
| DirectML | RX 7900 | 5-8 seconds |

> Times are approximate and depend on hardware, model, and number of inference steps.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Auto-detect picks CPU despite having a GPU | Install the GPU-specific OnnxRuntime NuGet package |
| CUDA error at runtime | Verify CUDA toolkit and cuDNN are installed and on PATH |
| DirectML error | Ensure you're on Windows 10/11 with DirectX 12 support |
| Out of GPU memory | Reduce image dimensions or use fewer inference steps |
| Both CPU and GPU packages installed | Remove `Microsoft.ML.OnnxRuntime` and keep only the GPU variant |
