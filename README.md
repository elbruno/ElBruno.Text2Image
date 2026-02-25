# ElBruno.Text2Image

[![NuGet](https://img.shields.io/nuget/v/ElBruno.Text2Image.svg)](https://www.nuget.org/packages/ElBruno.Text2Image)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET library for **local text-to-image generation** using Stable Diffusion and ONNX Runtime. Generate images from text prompts with automatic model download from HuggingFace — no Python dependency required.

## Features

- 🎨 **Text-to-Image** — Generate images from text prompts using Stable Diffusion
- 🤖 **Multiple Models** — Stable Diffusion 1.5, with LCM Dreamshaper and SDXL Turbo planned
- ⬇️ **Auto-Download** — ONNX models are automatically downloaded from HuggingFace on first use
- 🔧 **ONNX Runtime** — Fast, cross-platform inference (CPU, CUDA, DirectML)
- 📦 **NuGet Package** — Simple `dotnet add package` installation
- 🎯 **Multi-target** — Supports .NET 8.0 and .NET 10.0
- 🌱 **Reproducible** — Seed-based generation for reproducible results

## Quick Start

### Install

```bash
dotnet add package ElBruno.Text2Image
```

### Basic Usage

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

### Dependency Injection

```csharp
services.AddStableDiffusion15(options =>
{
    options.NumInferenceSteps = 20;
    options.ModelDirectory = "/path/to/models";
});

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

| Model | Class | ONNX Source | Steps | VRAM | Status |
|-------|-------|------------|-------|------|--------|
| **Stable Diffusion 1.5** | `StableDiffusion15` | `onnx-community/stable-diffusion-v1-5-ONNX` | 15-50 | ~4 GB | ✅ Available |
| **LCM Dreamshaper v7** | `LcmDreamshaperV7` | `TheyCallMeHex/LCM-Dreamshaper-V7-ONNX` | 2-4 | ~4 GB | ✅ Available |
| SDXL Turbo | *Coming soon* | Needs ONNX export | 1-4 | ~8 GB | 🔜 Planned |
| SD 2.1 Base | *Coming soon* | Needs ONNX export | 15-50 | ~5 GB | 🔜 Planned |

See [docs/model-support.md](docs/model-support.md) for detailed model comparison.

## Samples

| Sample | Description |
|--------|-------------|
| [scenario-01-simple](src/samples/scenario-01-simple/) | Basic text-to-image generation with SD 1.5 |
| [scenario-02-custom-options](src/samples/scenario-02-custom-options/) | Custom seeds, guidance scale, and steps |

### Run a Sample

```bash
cd src/samples/scenario-01-simple
dotnet run
```

## Architecture

```
Text Prompt
    │
    ▼
┌─────────────────┐
│ CLIP Tokenizer   │  Text → token IDs (77 tokens)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Text Encoder     │  text_encoder/model.onnx → embeddings [2,77,768]
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ UNet + Scheduler │  unet/model.onnx — iterative denoising loop
│                   │  Euler Ancestral / LMS scheduler
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ VAE Decoder      │  vae_decoder/model.onnx → pixels [1,3,512,512]
└────────┬────────┘
         │
         ▼
   PNG Image (512×512)
```

## ONNX Model Conversion

For models not yet in ONNX format, see:
- [docs/onnx-conversion-guide.md](docs/onnx-conversion-guide.md) — Step-by-step conversion guide
- [scripts/](scripts/) — Python conversion and upload scripts

## Dependencies

- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader) — Model download from HuggingFace
- [Microsoft.ML.OnnxRuntime](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime) — ONNX inference
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) — Cross-platform image processing

## Building

```bash
dotnet build
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

**Bruno Capuano** — [@elbruno](https://github.com/elbruno)

## Related Projects

- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader)
- [ElBruno.LocalEmbeddings](https://github.com/elbruno/elbruno.localembeddings)
- [ElBruno.VibeVoiceTTS](https://github.com/elbruno/ElBruno.VibeVoiceTTS)
