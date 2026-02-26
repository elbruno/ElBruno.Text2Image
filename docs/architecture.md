# Architecture

## Package Structure

Following the same pattern as `Microsoft.ML.OnnxRuntime`:

```
ElBruno.Text2Image              ← Core library (C# managed code, no native runtime)
    ├── depends on: OnnxRuntime.Managed (managed API only)
    │
ElBruno.Text2Image.Cpu          ← Core + OnnxRuntime CPU native
ElBruno.Text2Image.Cuda         ← Core + OnnxRuntime.Gpu CUDA native
ElBruno.Text2Image.DirectML     ← Core + OnnxRuntime.DirectML native
ElBruno.Text2Image.Foundry      ← Core + FLUX.2 via Microsoft Foundry cloud API
```

Users install **one** package depending on their hardware:

| Package | Runtime | Platform |
|---------|---------|----------|
| `ElBruno.Text2Image.Cpu` | CPU | Cross-platform |
| `ElBruno.Text2Image.Cuda` | NVIDIA CUDA | Linux, Windows |
| `ElBruno.Text2Image.DirectML` | DirectML | Windows (AMD/Intel/NVIDIA) |
| `ElBruno.Text2Image.Foundry` | Cloud API | Cross-platform (no GPU required) |

> **Note:** The GPU packages are mutually exclusive — install only ONE, following the same pattern as `Microsoft.ML.OnnxRuntime` vs `Microsoft.ML.OnnxRuntime.Gpu`.

## Key Dependencies

- [ElBruno.HuggingFace.Downloader](https://github.com/elbruno/ElBruno.HuggingFace.Downloader) — Model download from HuggingFace
- [Microsoft.ML.OnnxRuntime](https://www.nuget.org/packages/Microsoft.ML.OnnxRuntime) — ONNX inference
- [Microsoft.Extensions.AI.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions) — Standard AI interfaces (`IImageGenerator`)
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) — Cross-platform image processing

## Local Pipeline (Stable Diffusion)

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
│                   │  Euler Ancestral / LMS / LCM scheduler
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

## Cloud Pipeline (FLUX.2)

```
Text Prompt
    │
    ▼
┌──────────────────────┐
│ HTTP POST → Azure AI │  JSON: {prompt, size, n}
│ Foundry Endpoint     │
└──────────┬───────────┘
           │
           ▼  (supports async 202 polling)
           │
   PNG Image (1024×1024)
```

The FLUX.2 pipeline supports both synchronous (200 OK with image data) and asynchronous (202 Accepted + `operation-location` polling) response patterns from the Microsoft Foundry API.
