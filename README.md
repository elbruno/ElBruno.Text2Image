# ElBruno.Text2Image

[![NuGet](https://img.shields.io/nuget/v/ElBruno.Text2Image.svg)](https://www.nuget.org/packages/ElBruno.Text2Image)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET library for **local image captioning** using ONNX Runtime. Supports multiple vision models with automatic model download from HuggingFace — no Python dependency required.

## Features

- 🖼️ **Image Captioning** — Generate text descriptions from images
- 🤖 **Multiple Models** — ViT-GPT2, BLIP, and more coming soon
- ⬇️ **Auto-Download** — Models are automatically downloaded from HuggingFace on first use
- 🔧 **ONNX Runtime** — Fast, cross-platform inference (CPU, CUDA, DirectML)
- 📦 **NuGet Package** — Simple `dotnet add package` installation
- 🎯 **Multi-target** — Supports .NET 8.0 and .NET 10.0

## Quick Start

### Install

```bash
dotnet add package ElBruno.Text2Image
```

### Basic Usage

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Models;

// Create a captioner (model downloads automatically on first use)
using var captioner = new ViTGpt2Captioner();

// Generate a caption
var result = await captioner.CaptionAsync("photo.jpg");
Console.WriteLine(result.Caption);
// Output: "a cat sitting on a table"
```

### With Options

```csharp
using var captioner = new ViTGpt2Captioner(new ImageCaptionerOptions
{
    MaxTokens = 30,
    UseQuantized = true,  // Smaller, faster model
    ExecutionProvider = ExecutionProvider.Cpu
});

var result = await captioner.CaptionAsync("photo.jpg");
Console.WriteLine($"Caption: {result.Caption}");
Console.WriteLine($"Time: {result.InferenceTimeMs}ms");
```

### Dependency Injection

```csharp
services.AddViTGpt2Captioner(options =>
{
    options.MaxTokens = 50;
    options.ModelDirectory = "/path/to/models";
});

// Inject IImageCaptioner anywhere
public class MyService(IImageCaptioner captioner)
{
    public async Task<string> DescribeImage(string path)
    {
        var result = await captioner.CaptionAsync(path);
        return result.Caption;
    }
}
```

## Supported Models

| Model | Class | Size | Best For |
|-------|-------|------|----------|
| **ViT-GPT2** | `ViTGpt2Captioner` | ~300M | Quick, simple captioning |
| **BLIP Base** | `BlipCaptioner` | ~385M | Flexible captioning with text prompts |
| Florence-2 | *Coming soon* | ~230M | Multi-task vision (caption, detection, OCR) |
| GIT Base | *Coming soon* | ~130M | COCO-style captioning |
| Moondream 2 | *Coming soon* | ~1.8B | Highest quality VLM |

See [docs/model-support.md](docs/model-support.md) for detailed model comparison.

## Samples

| Sample | Description |
|--------|-------------|
| [scenario-01-simple](src/samples/scenario-01-simple/) | Basic image captioning with ViT-GPT2 |
| [scenario-02-blip-conditional](src/samples/scenario-02-blip-conditional/) | BLIP with conditional captioning |
| [scenario-03-florence2-multitask](src/samples/scenario-03-florence2-multitask/) | Florence-2 multi-task (coming soon) |
| [scenario-04-compare-models](src/samples/scenario-04-compare-models/) | Compare all available models |

### Run a Sample

```bash
cd src/samples/scenario-01-simple
dotnet run -- path/to/image.jpg
```

## Architecture

```
Image File/Stream
       │
       ▼
┌──────────────┐
│ Preprocessor │  Resize, normalize, to tensor
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Encoder    │  ViT / CLIP / DaViT (ONNX)
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Decoder    │  GPT-2 / BERT / Custom (ONNX, autoregressive)
└──────┬───────┘
       │
       ▼
┌──────────────┐
│  Tokenizer   │  Token IDs → text
└──────┬───────┘
       │
       ▼
   Caption text
```

## ONNX Model Conversion

For models not yet in ONNX format, see:
- [docs/onnx-conversion-guide.md](docs/onnx-conversion-guide.md) — Step-by-step conversion guide
- [scripts/](scripts/) — Python conversion and upload scripts

## Dependencies

- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader) — Model download from HuggingFace
- [Microsoft.ML.OnnxRuntime](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime) — ONNX inference
- [Microsoft.ML.Tokenizers](https://www.nuget.org/packages/Microsoft.ML.Tokenizers) — Tokenization
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
