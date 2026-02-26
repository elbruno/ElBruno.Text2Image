# ElBruno.Text2Image

[![NuGet](https://img.shields.io/nuget/v/ElBruno.Text2Image.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Text2Image)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ElBruno.Text2Image.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Text2Image)
[![Build Status](https://github.com/elbruno/ElBruno.Text2Image/actions/workflows/publish.yml/badge.svg)](https://github.com/elbruno/ElBruno.Text2Image/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/elbruno/ElBruno.Text2Image?style=social)](https://github.com/elbruno/ElBruno.Text2Image)
[![Twitter Follow](https://img.shields.io/twitter/follow/elbruno?style=social)](https://twitter.com/elbruno)

**HuggingFace ONNX Models:**
[![SD 2.1 ONNX](https://img.shields.io/badge/🤗%20HuggingFace-SD%202.1%20ONNX-yellow?style=flat-square)](https://huggingface.co/elbruno/stable-diffusion-2-1-ONNX)
[![SDXL Turbo ONNX](https://img.shields.io/badge/🤗%20HuggingFace-SDXL%20Turbo%20ONNX-yellow?style=flat-square)](https://huggingface.co/elbruno/sdxl-turbo-ONNX)

> 📢 **This project started with [FLUX.2 Flex on Microsoft Foundry](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/meet-flux-2-flex-for-text%E2%80%91heavy-design-and-ui-prototyping-now-available-on-micro/4496041)** — a cloud-first approach to text-to-image generation with best-in-class text rendering. After wrapping that API, we thought: *"Why not bring the same developer experience to local models too?"* So we did. Now you can generate images from text prompts using cloud APIs or local Stable Diffusion models with ONNX Runtime — all through the same clean .NET interface.

A .NET library for **text-to-image generation** — cloud and local. Generate images from text prompts using Microsoft Foundry FLUX.2 or Stable Diffusion (ONNX Runtime) with automatic model downloads from HuggingFace. No Python needed. Just `dotnet add package` and go. 🚀

## Features

- 🎨 **Text-to-Image** — Generate images from text prompts using Stable Diffusion or FLUX.2
- 🤖 **Multiple Models** — Stable Diffusion 1.5, LCM Dreamshaper, SDXL Turbo, SD 2.1, FLUX.2 (cloud)
- ⬇️ **Auto-Download** — ONNX models are automatically downloaded from HuggingFace on first use
- ☁️ **Cloud API** — FLUX.2 via Microsoft Foundry for high-quality text-heavy designs
- 🔧 **ONNX Runtime** — Fast, cross-platform inference (CPU, CUDA, DirectML)
- ⚡ **Auto GPU Detection** — Automatically uses GPU if available (CUDA → DirectML → CPU)
- 📦 **NuGet Package** — Simple `dotnet add package` installation
- 🎯 **Multi-target** — Supports .NET 8.0 and .NET 10.0
- 🔌 **Microsoft.Extensions.AI** — All generators implement `IImageGenerator` from [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions)
- 🌱 **Reproducible** — Seed-based generation for reproducible results

## Quick Start

### Install

Choose the package matching your hardware:

```bash
# CPU (default — works everywhere)
dotnet add package ElBruno.Text2Image.Cpu

# NVIDIA GPU (CUDA — 4x faster)
dotnet add package ElBruno.Text2Image.Cuda

# DirectML (AMD/Intel/NVIDIA on Windows)
dotnet add package ElBruno.Text2Image.DirectML

# FLUX.2 cloud via Microsoft Foundry (no GPU needed)
dotnet add package ElBruno.Text2Image.Foundry
```

> **Note:** These are mutually exclusive — install only ONE, following the same pattern as `Microsoft.ML.OnnxRuntime` vs `Microsoft.ML.OnnxRuntime.Gpu`.

### Basic Usage — Local (Stable Diffusion 1.5)

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

// Create a Stable Diffusion 1.5 generator (model downloads automatically on first use)
using var generator = new StableDiffusion15();

// Generate an image from a text prompt
var result = await generator.GenerateAsync("a beautiful sunset over a mountain lake, digital art");

// Save the generated image
await result.SaveAsync("output.png");
Console.WriteLine($"Generated in {result.InferenceTimeMs}ms (seed: {result.Seed})");
```

### Basic Usage — Cloud (FLUX.2 via Microsoft Foundry)

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

// Create a FLUX.2 generator using Microsoft Foundry
// Default model is FLUX.2-flex (text-heavy design and UI prototyping)
using var generator = new Flux2Generator(
    endpoint: "https://your-resource.openai.azure.com",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Flex",          // display name
    modelId: "FLUX.2-flex",            // API model identifier
    deploymentName: "FLUX.2-flex");    // Azure deployment name

// Generate an image — same interface as local models
var result = await generator.GenerateAsync("a futuristic cityscape with neon lights, cyberpunk style");
await result.SaveAsync("flux2-output.png");
```

### With Custom Options

```csharp
using var generator = new StableDiffusion15();

var result = await generator.GenerateAsync("a futuristic cityscape at night, neon lights",
    new ImageGenerationOptions
    {
        NumInferenceSteps = 20,  // More steps = better quality
        GuidanceScale = 7.5,     // Higher = follows prompt more closely
        Width = 512,
        Height = 512,
        Seed = 42,               // For reproducible results
        ExecutionProvider = ExecutionProvider.Cpu
    });

await result.SaveAsync("cityscape.png");
```

### Microsoft.Extensions.AI Interface

All generators implement `Microsoft.Extensions.AI.IImageGenerator`, enabling a standard API:

```csharp
using Microsoft.Extensions.AI;
using ElBruno.Text2Image.Models;

// Any generator can be used via the M.E.AI interface
using var sd15 = new StableDiffusion15();
IImageGenerator generator = sd15;

var request = new ImageGenerationRequest("a whimsical treehouse in a fantasy forest");
var options = new ImageGenerationOptions
{
    ImageSize = new System.Drawing.Size(512, 512),
    AdditionalProperties = new AdditionalPropertiesDictionary
    {
        ["num_inference_steps"] = 15,
        ["guidance_scale"] = 7.5,
        ["seed"] = 42
    }
};

var response = await generator.GenerateAsync(request, options);
var imageBytes = response.Contents.OfType<DataContent>().First().Data.ToArray();
await File.WriteAllBytesAsync("output.png", imageBytes);
```

### Custom Model Directory

```csharp
// Download and use models from a specific directory
using var generator = new StableDiffusion15(new ImageGenerationOptions
{
    ModelDirectory = @"D:\MyModels",
    NumInferenceSteps = 15
});

await generator.EnsureModelAvailableAsync();
var result = await generator.GenerateAsync("a serene lake");
await result.SaveAsync("output.png");
```

### Dependency Injection

```csharp
// Local model
services.AddStableDiffusion15(options =>
{
    options.NumInferenceSteps = 20;
    options.ModelDirectory = "/path/to/models";
});

// Cloud model (requires ElBruno.Text2Image.Foundry package)
services.AddFlux2Generator(
    endpoint: "https://your-resource.openai.azure.com",
    apiKey: "your-api-key",
    modelId: "FLUX.2-flex",
    deploymentName: "FLUX.2-flex");

// Inject IImageGenerator anywhere
public class MyService(IImageGenerator generator)
{
    public async Task<byte[]> GenerateImage(string prompt)
    {
        var result = await generator.GenerateAsync(prompt);
        return result.ImageBytes;
    }
}
```

## Supported Models

### Local Models (ONNX Runtime)

| Model | Class | ONNX Source | Steps | VRAM | Status |
|-------|-------|------------|-------|------|--------|
| **Stable Diffusion 1.5** | `StableDiffusion15` | `onnx-community/stable-diffusion-v1-5-ONNX` | 15-50 | ~4 GB | ✅ Available |
| **LCM Dreamshaper v7** | `LcmDreamshaperV7` | `TheyCallMeHex/LCM-Dreamshaper-V7-ONNX` | 2-4 | ~4 GB | ✅ Available |
| **SDXL Turbo** | `SdxlTurbo` | `elbruno/sdxl-turbo-ONNX` | 1-4 | ~8 GB | ✅ Available |
| **SD 2.1 Base** | `StableDiffusion21` | `elbruno/stable-diffusion-2-1-ONNX` | 15-50 | ~5 GB | ✅ Available |

### Cloud Models (REST API)

| Model | Class | Provider | Quality | Status |
|-------|-------|----------|---------|--------|
| **FLUX.2 Flex** | `Flux2Generator` | Microsoft Foundry | Excellent | ✅ Default |
| **FLUX.2 Pro** | `Flux2Generator` | Microsoft Foundry | Excellent | ✅ Available |
See [docs/model-support.md](docs/model-support.md) for detailed model comparison.

## Samples

| Sample | Description |
|--------|-------------|
| [scenario-01-simple](src/samples/scenario-01-simple/) | Basic text-to-image generation with SD 1.5 |
| [scenario-02-custom-options](src/samples/scenario-02-custom-options/) | Custom seeds, guidance scale, and steps |
| [scenario-03-flux2-cloud](src/samples/scenario-03-flux2-cloud/) | FLUX.2 cloud API via Microsoft Foundry |
| [scenario-04-lcm-fast](src/samples/scenario-04-lcm-fast/) | Ultra-fast generation with LCM Dreamshaper (2-4 steps) |
| [scenario-05-sd21](src/samples/scenario-05-sd21/) | Stable Diffusion 2.1 at 768×768 native resolution |
| [scenario-06-model-comparison](src/samples/scenario-06-model-comparison/) | Compare SD 1.5 vs LCM side-by-side |
| [scenario-07-custom-model-directory](src/samples/scenario-07-custom-model-directory/) | Download models to a custom directory |
| [scenario-08-meai-interface](src/samples/scenario-08-meai-interface/) | Use via Microsoft.Extensions.AI `IImageGenerator` |
| [scenario-09-batch-generation](src/samples/scenario-09-batch-generation/) | Generate multiple images from a batch of prompts |
| [scenario-10-progress-reporting](src/samples/scenario-10-progress-reporting/) | Detailed download progress reporting with progress bar |
| [scenario-11-gpu-diagnostics](src/samples/scenario-11-gpu-diagnostics/) | Show CPU vs GPU provider detection and diagnostics |

### Run a Sample

```bash
cd src/samples/scenario-01-simple
dotnet run
```

## Documentation

- [docs/architecture.md](docs/architecture.md) — Package structure and pipeline diagrams
- [docs/gpu-acceleration.md](docs/gpu-acceleration.md) — GPU setup (CUDA, DirectML, auto-detection)
- [docs/flux2-setup-guide.md](docs/flux2-setup-guide.md) — Microsoft Foundry FLUX.2 setup
- [docs/model-support.md](docs/model-support.md) — Detailed model comparison
- [docs/onnx-conversion-guide.md](docs/onnx-conversion-guide.md) — Step-by-step ONNX conversion guide
- [docs/publishing.md](docs/publishing.md) — NuGet publishing guide (Trusted Publishing / OIDC)
- [docs/security.md](docs/security.md) — Security considerations and hardening
- [scripts/](scripts/) — Python conversion and upload scripts

## 👋 About the Author

Hi! I'm **ElBruno** 🧡, a passionate developer and content creator exploring AI, .NET, and modern development practices.

**Made with ❤️ by [ElBruno](https://github.com/elbruno)**

If you like this project, consider following my work across platforms:

- 📻 **Podcast**: [No Tienen Nombre](https://notienenombre.com) — Spanish-language episodes on AI, development, and tech culture
- 💻 **Blog**: [ElBruno.com](https://elbruno.com) — Deep dives on embeddings, RAG, .NET, and local AI
- 📺 **YouTube**: [youtube.com/elbruno](https://www.youtube.com/elbruno) — Demos, tutorials, and live coding
- 🔗 **LinkedIn**: [@elbruno](https://www.linkedin.com/in/elbruno/) — Professional updates and insights
- 𝕏 **Twitter**: [@elbruno](https://www.x.com/in/elbruno/) — Quick tips, releases, and tech news

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Related Projects

- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader)
- [ElBruno.LocalEmbeddings](https://github.com/elbruno/elbruno.localembeddings)
- [ElBruno.VibeVoiceTTS](https://github.com/elbruno/ElBruno.VibeVoiceTTS)
- [ElBruno.QwenTTS](https://github.com/elbruno/ElBruno.QwenTTS)
