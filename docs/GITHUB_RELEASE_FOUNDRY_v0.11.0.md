# ElBruno.Text2Image Foundry v0.11.0 — GPT-Image-2 Support

**Release Date:** April 21, 2026  
**Tag:** `foundry-v0.11.0`  
**Version:** 0.11.0

## What's New

This release adds **GPT-Image-2** (Microsoft's latest image generation model) to the Foundry library, bringing total supported cloud models to **four production-ready options**.

### ✅ New Features

- **GPT-Image-2 support** via `GptImage2Generator` class
  - Latest generation image synthesis from Microsoft
  - Azure OpenAI Service integration
  - Supports fixed sizes: 1024×1024, 1792×1024, 1024×1792
  - HD quality support for enhanced detail

### ✨ Model Support

The Foundry library now supports **four cloud-based image generation models**:

1. **GPT-Image-2** (NEW) — Microsoft's latest model
2. **GPT-Image-1.5** — DALL-E 3 via Azure OpenAI
3. **FLUX.2** — Black Forest Labs (pro and flex variants)
4. **MAI-Image-2** — Alibaba (standard and enhanced variants)

### 📊 Test Coverage

- **668 total tests passing**
- **net8.0:** 298 tests (6 skipped)
- **net10.0:** 370 tests (8 skipped)
- All models have comprehensive unit and integration tests

## Installation

### NuGet Package

```bash
dotnet add package ElBruno.Text2Image.Foundry --version 0.11.0
```

### Library Usage

```csharp
// GPT-Image-2 (NEW)
var generator = new GptImage2Generator(endpoint, apiKey);
var result = await generator.GenerateAsync("Your prompt", 1024, 1024);

// All models available
builder.Services.AddElbrunoText2ImageFoundry()
    .AddGptImage2(endpoint, apiKey)
    .AddGptImage1p5(endpoint, apiKey)
    .AddFlux2(foundryKey)
    .AddMaiImage2(foundryKey);
```

## Setup

### Prerequisites

- **For GPT-Image-2 & GPT-Image-1.5:** Azure OpenAI Service deployment
- **For FLUX.2 & MAI-Image-2:** Microsoft Foundry API access

### Configuration

Set environment variables:

```bash
export GPT_IMAGE_ENDPOINT="https://your-deployment.openai.azure.com/"
export GPT_IMAGE_API_KEY="your-key"
export FOUNDRY_FLUX2_API_KEY="your-key"
export FOUNDRY_MAI2_API_KEY="your-key"
```

## Documentation

- 📖 [README](https://github.com/elbruno/ElBruno.Text2Image/blob/main/README.md) — Complete feature overview
- 📝 [Release Notes](https://github.com/elbruno/ElBruno.Text2Image/blob/main/RELEASE_NOTES_v0.11-v0.12.md) — Full details on all models
- 📚 [Sample Projects](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples) — Runnable examples

## Breaking Changes

**None.** Fully backward compatible with v0.10.0.

## Deprecations

**None.**

## Dependencies

| Package | Version |
|---------|---------|
| Azure.AI.OpenAI | 2.1.* |
| .NET | net8.0, net10.0 |

## Contributors

- **Bruno Capuano** — Project lead, GPT-Image-2 integration

## Support

- 🐛 [Report Issues](https://github.com/elbruno/ElBruno.Text2Image/issues)
- 💬 [Join Discussions](https://github.com/elbruno/ElBruno.Text2Image/discussions)

---

**Changelog:** See [RELEASE_NOTES_v0.11-v0.12.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/RELEASE_NOTES_v0.11-v0.12.md)
