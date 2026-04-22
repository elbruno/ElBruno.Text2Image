# ElBruno.Text2Image v0.9.2 — Unified Multi-Model Support

**Release Date:** April 21, 2026  
**Tag:** `v0.9.2`  
**Repository:** https://github.com/elbruno/ElBruno.Text2Image

---

## 🎉 Release Overview

This release represents the **first unified release tag** for the entire ElBruno.Text2Image project ecosystem, bringing together all packages into a cohesive v0.9.2 release. This unified release marks a major milestone in multi-model support, with **four production-ready cloud image generation models** and **three local acceleration options** all available through a single, clean .NET interface.

### What's Included

- **ElBruno.Text2Image** (v0.9.1) — Core library with local model support
- **ElBruno.Text2Image.Foundry** (v0.11.0) — Cloud models via Microsoft Foundry
- **ElBruno.Text2Image.Cli** (v0.12.0) — Cross-platform CLI tool (`t2i`)
- **ElBruno.Text2Image.Cpu** (v0.9.1) — CPU-based inference
- **ElBruno.Text2Image.Cuda** (v0.9.1) — NVIDIA GPU acceleration
- **ElBruno.Text2Image.DirectML** (v0.9.1) — Windows GPU acceleration

---

## ✨ Major Features

### 🌐 Multi-Model Cloud Support

This release adds **four production-ready cloud image generation models**:

1. **GPT-Image-2** (NEW) — Microsoft's latest generation image synthesis
   - Via Azure OpenAI Service
   - Supports multiple sizes: 1024×1024, 1792×1024, 1024×1792
   - HD quality support for enhanced detail
   
2. **GPT-Image-1.5** — DALL-E 3 classic reliability
   - Via Azure OpenAI Service
   - Fixed output sizes for consistent quality
   - Excellent prompt following

3. **FLUX.2** — Black Forest Labs photorealistic generation
   - **FLUX.2-pro** (default) — Photorealistic image generation
   - **FLUX.2-flex** — Text-heavy design and UI prototyping
   - Variable aspect ratios for creative flexibility

4. **MAI-Image-2** — Alibaba's high-quality synthesis
   - **MAI-Image-2** (default) — Standard model
   - **MAI-Image-2e** — Enhanced variant
   - Excellent for diverse image types

### 💻 Local Model Support (ONNX Runtime)

| Model | Class | Steps | VRAM | Status |
|-------|-------|-------|------|--------|
| **Stable Diffusion 1.5** | `StableDiffusion15` | 15-50 | ~4 GB | ✅ Available |
| **LCM Dreamshaper v7** | `LcmDreamshaperV7` | 2-4 | ~4 GB | ✅ Available |
| **SDXL Turbo** | `SdxlTurbo` | 1-4 | ~8 GB | ✅ Available |
| **SD 2.1 Base** | `StableDiffusion21` | 15-50 | ~5 GB | ✅ Available |

### ⚡ Acceleration Options

- **CPU** — Works everywhere (default via ElBruno.Text2Image.Cpu)
- **CUDA** — NVIDIA GPUs (4x faster via ElBruno.Text2Image.Cuda)
- **DirectML** — AMD/Intel/NVIDIA on Windows (via ElBruno.Text2Image.DirectML)
- **Auto-detection** — Automatically uses fastest available provider

### 🎯 Unified Interface

All generators implement `Microsoft.Extensions.AI.IImageGenerator`, enabling:

```csharp
IImageGenerator generator = new StableDiffusion15();
// or
var generator = new Flux2Generator(endpoint, apiKey);
// or
var generator = new MaiImage2Generator(endpoint, apiKey);

// Same clean API for all:
var result = await generator.GenerateAsync(prompt, options);
await result.SaveAsync("output.png");
```

### 🛠️ Cross-Platform CLI

The `t2i` command-line tool now supports all models:

```bash
# Install
dotnet tool install --global ElBruno.Text2Image.Cli

# Quick start with any model
t2i --provider foundry-gpt-image-2 "A serene mountain landscape"
t2i --provider foundry-flux2 "A magical forest"
t2i --provider foundry-mai2 "A calm waterfall"

# With custom options
t2i --provider foundry-flux2 --width 1024 --height 768 "Your prompt"

# Configure model variants
t2i config set foundry-mai2.model MAI-Image-2e
t2i config set foundry-flux2.model FLUX.2-flex
```

### 📊 Quality Metrics

✅ **668 passing tests** across all frameworks:
- **net8.0:** 298 tests (6 skipped)
- **net10.0:** 370 tests (8 skipped)

✅ **100% Backward Compatible** — No breaking changes

✅ **Cross-Platform Support:**
- Windows (x64, x86, ARM64)
- macOS (x64, ARM64)
- Linux (x64, ARM64, ARM32)

---

## 📦 Installation

### Package Overview

| Package | Purpose | Installation |
|---------|---------|--------------|
| **ElBruno.Text2Image** | Core library (base) | `dotnet add package ElBruno.Text2Image` |
| **ElBruno.Text2Image.Cpu** | CPU inference | `dotnet add package ElBruno.Text2Image.Cpu` |
| **ElBruno.Text2Image.Cuda** | NVIDIA GPU (4x faster) | `dotnet add package ElBruno.Text2Image.Cuda` |
| **ElBruno.Text2Image.DirectML** | Windows GPU | `dotnet add package ElBruno.Text2Image.DirectML` |
| **ElBruno.Text2Image.Foundry** | Cloud models | `dotnet add package ElBruno.Text2Image.Foundry` |
| **ElBruno.Text2Image.Cli** | CLI tool (`t2i`) | `dotnet tool install --global ElBruno.Text2Image.Cli` |

> **Note:** The CPU, CUDA, and DirectML packages are mutually exclusive — install only **ONE** matching your hardware. This follows the same pattern as `Microsoft.ML.OnnxRuntime`.

---

## 🚀 Quick Start Examples

### Local Generation (CPU)

```csharp
using ElBruno.Text2Image;

var generator = new StableDiffusion15();
var result = await generator.GenerateAsync("a beautiful sunset over mountains");
await result.SaveAsync("sunset.png");
Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

### Cloud Generation (FLUX.2)

```csharp
using ElBruno.Text2Image.Foundry;

var generator = new Flux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-key",
    modelName: "FLUX.2 Pro",
    modelId: "FLUX.2-pro");

var result = await generator.GenerateAsync("a futuristic cityscape with neon lights");
await result.SaveAsync("flux-output.png");
```

### Cloud Generation (MAI-Image-2)

```csharp
var generator = new MaiImage2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-key",
    modelName: "MAI-Image-2",
    modelId: "MAI-Image-2");

var result = await generator.GenerateAsync("a serene landscape");
await result.SaveAsync("mai-output.png");
```

### Cloud Generation (GPT-Image-2)

```csharp
var generator = new GptImage2Generator(
    endpoint: "https://your-deployment.openai.azure.com/",
    apiKey: "your-key",
    deploymentName: "gpt-image-2");

var result = await generator.GenerateAsync("a professional headshot");
await result.SaveAsync("gpt-image-2.png");
```

### Cloud Generation (GPT-Image-1.5)

```csharp
var generator = new GptImage1p5Generator(
    endpoint: "https://your-deployment.openai.azure.com/",
    apiKey: "your-key",
    deploymentName: "gpt-image-15");

var result = await generator.GenerateAsync("a serene mountain landscape");
await result.SaveAsync("gpt-image-1p5.png");
```

### Via Microsoft.Extensions.AI Interface

```csharp
using Microsoft.Extensions.AI;

IImageGenerator generator = new StableDiffusion15();

var request = new ImageGenerationRequest("a whimsical treehouse");
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

### Dependency Injection

```csharp
services.AddStableDiffusion15(options => {
    options.NumInferenceSteps = 20;
});

services.AddFlux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-key",
    modelId: "FLUX.2-pro");

// Inject anywhere
public class ImageService(IImageGenerator generator) {
    public async Task<byte[]> GenerateImage(string prompt) {
        var result = await generator.GenerateAsync(prompt);
        return result.ImageBytes;
    }
}
```

---

## 🔧 Setup Guides

### Cloud Models Setup

- **[FLUX.2 Setup Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/flux2-setup-guide.md)** — Microsoft Foundry FLUX.2 configuration
- **[MAI-Image-2 Setup Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/mai-image-2-setup-guide.md)** — Microsoft Foundry MAI-Image-2 configuration
- **[GPT-Image-1.5 Setup Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/gpt-image-1p5-setup-guide.md)** — Azure OpenAI (DALL-E 3) configuration
- **[GPT-Image-2 Setup Guide (Coming Soon)](https://github.com/elbruno/ElBruno.Text2Image/issues)** — Azure OpenAI GPT-Image-2 configuration

### Local Model Setup

- **[GPU Acceleration Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/gpu-acceleration.md)** — CUDA, DirectML setup and auto-detection
- **[ONNX Conversion Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/onnx-conversion-guide.md)** — Convert your own ONNX models

---

## 📚 Complete Documentation

- **[README.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/README.md)** — Complete feature overview
- **[Architecture Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/architecture.md)** — Package structure and pipelines
- **[Model Support](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/model-support.md)** — Detailed model comparison
- **[CLI Tool Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)** — Full CLI reference
- **[Security Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/security.md)** — Security considerations

---

## 🎨 Sample Projects

| Sample | Purpose |
|--------|---------|
| [scenario-01-simple](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-01-simple) | Basic SD 1.5 generation |
| [scenario-02-custom-options](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-02-custom-options) | Custom seeds, guidance, steps |
| [scenario-03-flux2-cloud](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-03-flux2-cloud) | FLUX.2 cloud API |
| [scenario-04-lcm-fast](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-04-lcm-fast) | Ultra-fast LCM generation |
| [scenario-05-sd21](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-05-sd21) | SD 2.1 at 768×768 |
| [scenario-06-model-comparison](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-06-model-comparison) | SD 1.5 vs LCM comparison |
| [scenario-13-mai-image2-cloud](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-13-mai-image2-cloud) | MAI-Image-2 cloud API |
| [scenario-15-gpt-image-1p5-cloud](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples/scenario-15-gpt-image-1p5-cloud) | GPT-Image-1.5 (DALL-E 3) cloud API |

---

## 🎯 What's New

### New Features

✅ **GPT-Image-2 Support** — Microsoft's latest generation model via Azure OpenAI  
✅ **Unified Release Tag** — v0.9.2 across all packages  
✅ **Model Variants** — Switch between model variants (e.g., MAI-Image-2 vs MAI-Image-2e)  
✅ **Enhanced CLI** — All four cloud models available through `t2i` command  
✅ **Complete Documentation** — Setup guides for all models  
✅ **Sample Projects** — Runnable examples for each model  

### Breaking Changes

**None.** This release is **100% backward compatible**.

### Deprecations

**None.** All existing APIs continue to work unchanged.

---

## 📋 Included Packages

### ElBruno.Text2Image (0.9.1)

Core library with local model support (Stable Diffusion via ONNX Runtime)

```bash
dotnet add package ElBruno.Text2Image
```

### ElBruno.Text2Image.Foundry (0.11.0)

Cloud models: FLUX.2, MAI-Image-2, GPT-Image-1.5, GPT-Image-2

```bash
dotnet add package ElBruno.Text2Image.Foundry
```

### ElBruno.Text2Image.Cli (0.12.0)

Cross-platform CLI tool (`t2i`)

```bash
dotnet tool install --global ElBruno.Text2Image.Cli
```

### ElBruno.Text2Image.Cpu (0.9.1)

CPU-based ONNX Runtime inference (default for environments without GPU)

```bash
dotnet add package ElBruno.Text2Image.Cpu
```

### ElBruno.Text2Image.Cuda (0.9.1)

NVIDIA GPU acceleration (CUDA) — 4x faster inference on NVIDIA GPUs

```bash
dotnet add package ElBruno.Text2Image.Cuda
```

### ElBruno.Text2Image.DirectML (0.9.1)

Windows GPU acceleration (DirectML) — Supports AMD, Intel, and NVIDIA GPUs on Windows

```bash
dotnet add package ElBruno.Text2Image.DirectML
```

---

## 🌐 Recent Blog Post

This release is accompanied by the blog post: **[Introducing Multi-Model Image Generation with Microsoft Foundry](https://elbruno.com)** — Detailed walkthrough of all four cloud models and how to integrate them into your .NET applications.

---

## 🐛 Bug Fixes

- Fixed `-h` short option collision in CLI (`t2i -h` now correctly shows help)
- Fixed MAI-Image-2 default dimension handling in CLI
- Improved error messages for authentication and deployment failures

---

## 📈 Test Coverage

✅ **668 passing tests** across all frameworks

- **net8.0:** 298 tests (6 skipped)
- **net10.0:** 370 tests (8 skipped)
- All integration tests include environment variable detection
- All models have unit and integration tests

---

## 🔗 Dependencies

No new dependencies added in this release. All projects continue to use existing pinned versions:

| Package | Version |
|---------|---------|
| Microsoft.Extensions.AI | 10.3.0 |
| Microsoft.ML.OnnxRuntime | 1.24.1 |
| Azure.AI.OpenAI | 2.1.* |
| Spectre.Console | 0.49.1 |
| .NET | 8.0, 10.0 |

---

## 🚀 Performance

- **Local CPU:** Stable Diffusion 1.5 in 30-60 seconds (depending on step count)
- **Local GPU (CUDA):** 4x faster — ~8-15 seconds
- **Cloud Models:** 30-90 seconds depending on model and quality settings

---

## 💬 Community & Support

- 📖 **GitHub Issues:** [Report bugs or request features](https://github.com/elbruno/ElBruno.Text2Image/issues)
- 💻 **GitHub Discussions:** [Share ideas and ask questions](https://github.com/elbruno/ElBruno.Text2Image/discussions)
- 📺 **YouTube:** [Watch demos and tutorials](https://www.youtube.com/elbruno)
- 🔗 **LinkedIn:** [@elbruno](https://www.linkedin.com/in/elbruno/)

---

## 👨‍💻 About the Author

**ElBruno** 🧡 — Passionate developer exploring AI, .NET, and modern development practices.

- 📻 **Podcast:** [No Tienen Nombre](https://notienenombre.com) (Spanish-language AI & development)
- 💻 **Blog:** [ElBruno.com](https://elbruno.com)
- 📺 **YouTube:** [youtube.com/elbruno](https://www.youtube.com/elbruno)
- 🔗 **LinkedIn:** [@elbruno](https://www.linkedin.com/in/elbruno/)
- 𝕏 **Twitter:** [@elbruno](https://www.x.com/in/elbruno/)

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](https://github.com/elbruno/ElBruno.Text2Image/blob/main/LICENSE) file for details.

---

**Ready to generate stunning images with .NET? Install v0.9.2 today!** 🎨
