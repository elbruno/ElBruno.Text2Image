# GPU Acceleration

ElBruno.Text2Image supports automatic GPU detection and acceleration using ONNX Runtime execution providers.

## Package Architecture

Following the same pattern as Microsoft.ML.OnnxRuntime, the library is split into **runtime-specific packages**:

```
ElBruno.Text2Image              ← Core library (managed code only, no native runtime)
ElBruno.Text2Image.Cpu          ← Core + CPU native runtime (default, works everywhere)
ElBruno.Text2Image.Cuda         ← Core + CUDA native runtime (NVIDIA GPUs)
ElBruno.Text2Image.DirectML     ← Core + DirectML native runtime (any GPU on Windows)
```

Install **one** of the runtime packages — they are mutually exclusive (just like `Microsoft.ML.OnnxRuntime` vs `Microsoft.ML.OnnxRuntime.Gpu`).

## Installation

### CPU (default, works everywhere)

```bash
dotnet add package ElBruno.Text2Image.Cpu
```

### NVIDIA GPU (CUDA) — recommended for NVIDIA GPUs

```bash
dotnet add package ElBruno.Text2Image.Cuda
```

**Requirements:**
- NVIDIA GPU (RTX 20xx or newer recommended)
- NVIDIA driver with CUDA 12.x support
- CUDA runtime libraries on PATH (cuBLAS, cuFFT, cuDNN, cuRAND, cuSOLVER, cuSPARSE, CUDA Runtime)

**Installing CUDA runtime libraries via pip (no full CUDA Toolkit needed):**

```bash
pip install nvidia-cublas-cu12 nvidia-cudnn-cu12 nvidia-cufft-cu12 nvidia-curand-cu12 nvidia-cusolver-cu12 nvidia-cusparse-cu12 nvidia-cuda-runtime-cu12 nvidia-cuda-nvrtc-cu12
```

Then add the installed DLL directories to your PATH. On Windows:

```powershell
$nvDir = "$env:USERPROFILE\AppData\Local\miniconda3\Lib\site-packages\nvidia"
# Or for pip: $nvDir = "$env:USERPROFILE\AppData\Local\Programs\Python\PythonXX\Lib\site-packages\nvidia"
$paths = Get-ChildItem $nvDir -Directory -Recurse | Where-Object { $_.Name -eq "bin" } | Select-Object -ExpandProperty FullName
$env:PATH = ($paths -join ";") + ";$env:PATH"
```

### Any GPU on Windows (DirectML) — AMD, Intel, NVIDIA

```bash
dotnet add package ElBruno.Text2Image.DirectML
```

**Requirements:**
- Windows 10/11
- Any DirectX 12 compatible GPU (AMD, Intel, NVIDIA)

## Automatic Detection (Default)

By default, `ExecutionProvider` is set to `Auto`, which probes for the best available provider in this order:

1. **CUDA** (NVIDIA GPUs) — fastest
2. **DirectML** (any GPU on Windows)
3. **CPU** — fallback, always available

```csharp
// Auto-detect is the default — no configuration needed
using var generator = new StableDiffusion15();
var result = await generator.GenerateAsync("a sunset over mountains");
```

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
```

## Checking Which Provider is Active

```csharp
var resolved = SessionOptionsHelper.ResolveProvider(ExecutionProvider.Auto);
Console.WriteLine($"Using: {resolved}"); // e.g., "Using: Cuda"
```

Run `scenario-11-gpu-diagnostics` for a full diagnostic report.

## Performance Comparison

Typical times for generating a 512×512 image with Stable Diffusion 1.5 (15 steps):

| Provider | Hardware | Approximate Time |
|----------|----------|-----------------|
| CPU | Modern desktop CPU | 30-60 seconds |
| CUDA | NVIDIA A10 24GB | ~9 seconds |
| CUDA | RTX 3060 | 3-5 seconds |
| CUDA | RTX 4090 | 1-2 seconds |
| DirectML | RX 7900 | 5-8 seconds |

> Times are approximate and depend on hardware, model, and number of inference steps.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Auto-detect picks CPU despite having a GPU | Install `ElBruno.Text2Image.Cuda` or `.DirectML` instead of `.Cpu` |
| CUDA "missing DLL" error | Install CUDA runtime libs via pip (see above) and add to PATH |
| DirectML error | Ensure you're on Windows 10/11 with DirectX 12 support |
| Out of GPU memory | Reduce image dimensions or use fewer inference steps |
| Run `scenario-11-gpu-diagnostics` | Shows all available providers and probes each one |
