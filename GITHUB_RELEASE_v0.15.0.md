# ElBruno.Text2Image v0.15.0 — Unified Multi-Model Release

**All packages now at v0.15.0** 🚀

This release brings a unified version across all packages and enhanced CLI documentation showcasing all supported models.

## What's New in v0.15.0

### Version Alignment
- **ElBruno.Text2Image** (Core): 0.15.0
- **ElBruno.Text2Image.Foundry**: 0.15.0
- **ElBruno.Text2Image.Cli**: 0.15.0
- **ElBruno.Text2Image.Cpu**: 0.15.0
- **ElBruno.Text2Image.Cuda**: 0.15.0
- **ElBruno.Text2Image.DirectML**: 0.15.0

### Enhanced CLI Documentation
Added comprehensive examples in the main README showing how to use all supported models via the CLI tool:

**Local Models:**
```bash
t2i --provider stable-diffusion-15 "sunset over mountains, oil painting style"
```

**Cloud Models:**
```bash
# FLUX.2 Pro (photorealistic)
t2i --provider foundry-flux2 "a futuristic cityscape with neon lights"

# FLUX.2 Flex (text-heavy design)
t2i config set foundry-flux2.model FLUX.2-flex
t2i "a business card design with modern minimalist style"

# MAI-Image-2 (high-quality generation)
t2i --provider foundry-mai2 "a serene mountain landscape at sunrise"

# GPT-Image-1.5 (DALL-E 3 via Azure OpenAI)
t2i --provider azure-openai-gpt-image-15 "an impressionist painting of a garden"

# GPT-Image-2 (next-gen model)
t2i --provider gpt-image-2 "a sci-fi space station in orbit"
```

## 🎯 Supported Models

### Cloud Models
- **FLUX.2 Pro** — High-quality photorealistic image generation (Microsoft Foundry)
- **FLUX.2 Flex** — Text-heavy design and UI prototyping (Microsoft Foundry)
- **MAI-Image-2** — High-quality image generation (Microsoft Foundry / Alibaba)
- **GPT-Image-1.5** — DALL-E 3 via Azure OpenAI
- **GPT-Image-2** — Next-generation GPT image model (Azure OpenAI)

### Local Models
- Stable Diffusion 1.5
- Stable Diffusion 2.1
- SDXL Turbo
- LCM Dreamshaper

## 📦 Installation

### NuGet Packages
```bash
# Core library (no acceleration backend)
dotnet add package ElBruno.Text2Image --version 0.15.0

# With Foundry cloud model support
dotnet add package ElBruno.Text2Image.Foundry --version 0.15.0

# Acceleration backends (choose one)
dotnet add package ElBruno.Text2Image.Cpu --version 0.15.0      # CPU (default)
dotnet add package ElBruno.Text2Image.Cuda --version 0.15.0     # NVIDIA GPU
dotnet add package ElBruno.Text2Image.DirectML --version 0.15.0 # AMD/Intel/NVIDIA on Windows
```

### CLI Tool
```bash
dotnet tool install --global ElBruno.Text2Image.Cli --version 0.15.0
# or update existing
dotnet tool update --global ElBruno.Text2Image.Cli
```

## 🚀 Quick Start Examples

### C# Library — Local Model (Stable Diffusion)
```csharp
using ElBruno.Text2Image;

using var generator = new StableDiffusion15();
var result = await generator.GenerateAsync("a beautiful sunset over mountains");
await result.SaveAsync("output.png");
```

### C# Library — Cloud (FLUX.2)
```csharp
using ElBruno.Text2Image.Foundry;

using var generator = new Flux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Pro",
    modelId: "FLUX.2-pro");

var result = await generator.GenerateAsync("a futuristic cityscape with neon lights");
await result.SaveAsync("flux2-output.png");
```

### C# Library — Cloud (GPT-Image-1.5)
```csharp
using ElBruno.Text2Image.Foundry;

using var generator = new GptImage1p5Generator(
    endpoint: "https://your-resource.openai.azure.com/",
    apiKey: "your-api-key",
    deploymentName: "gpt-image-15");

var result = await generator.GenerateAsync("a serene mountain landscape at sunset");
await result.SaveAsync("gpt-image-output.png");
```

### CLI — All Models
```bash
# Local (default)
t2i "a robot painting a landscape"

# FLUX.2
t2i --provider foundry-flux2 "a futuristic cityscape"

# MAI-Image-2
t2i --provider foundry-mai2 "a serene mountain landscape"

# GPT-Image-1.5
t2i --provider azure-openai-gpt-image-15 "an impressionist painting"

# GPT-Image-2
t2i --provider gpt-image-2 "a sci-fi space station"
```

## 🔧 CLI Configuration

```bash
# Interactive setup
t2i config

# View current config (credentials masked)
t2i config show

# Set default provider
t2i config set provider foundry-flux2

# Set model variant
t2i config set foundry-flux2.model FLUX.2-flex
t2i config set foundry-mai2.model MAI-Image-2e

# Set credentials
t2i config set foundry-flux2.endpoint "https://your-resource.services.ai.azure.com"
t2i config set foundry-flux2.apiKey "your-api-key"
```

## 📊 Feature Matrix

| Feature | Local | FLUX.2 | MAI-Image-2 | GPT-Image-1.5 | GPT-Image-2 |
|---------|-------|--------|-------------|---------------|-------------|
| Text-to-Image | ✅ | ✅ | ✅ | ✅ | ✅ |
| Async/Await | ✅ | ✅ | ✅ | ✅ | ✅ |
| Custom Prompts | ✅ | ✅ | ✅ | ✅ | ✅ |
| Size Control | ✅ | ✅ | ✅ | ✅ | ✅ |
| Quality Variants | ✅ | ✅ | ✅ | ✅ | ✅ |
| GPU Acceleration | ✅ | N/A | N/A | N/A | N/A |
| Auto Model Download | ✅ | N/A | N/A | N/A | N/A |

## 🐛 Bug Fixes & Improvements
- Unified version across all packages for easier dependency management
- Enhanced README with comprehensive CLI examples for all models
- Improved documentation of GPT-Image-1.5 and GPT-Image-2 support

## ✅ Testing & Compatibility
- 668+ passing tests across net8.0 and net10.0
- 100% backward compatible with v0.9.2
- Zero build warnings
- Cross-platform support (Windows, macOS, Linux)
- Tested with latest .NET 8.0 and .NET 10.0

## 📚 Documentation
- [Main README](https://github.com/elbruno/ElBruno.Text2Image/blob/main/README.md) — Getting started with all models
- [CLI Tool Guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md) — CLI tool reference
- [Multi-Model Blog Post](https://github.com/elbruno/ElBruno.Text2Image/blob/main/blog/2025-04-21-multi-model-image-generation.md) — Detailed model comparison and usage

## 🙏 Release Highlights

**v0.15.0 marks the unified release of all packages at a single version**, simplifying dependency management while maintaining backward compatibility with all previously published models.

All packages (Core, CLI, Foundry, CPU, CUDA, DirectML) are now synchronized at **0.15.0**, ensuring consistent version history and easier package management for all users.

---

**Released:** 2025
**License:** MIT
