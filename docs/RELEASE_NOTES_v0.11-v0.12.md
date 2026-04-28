# Release Notes: Multi-Model Support

## Foundry Library v0.11.0 & CLI v0.12.0

**Release Date:** April 21, 2026

---

## Overview

This release adds **GPT-Image-2** (Microsoft's latest image generation model) to the Foundry library and CLI tool, bringing the total supported models to **four production-ready options** for cloud-based image generation. All models are tested, documented, and ready for production use.

### New in This Release

✅ **GPT-Image-2** support via Azure OpenAI Service  
✅ **All four models** available in CLI and library  
✅ **668 passing tests** across net8.0 and net10.0  
✅ **Sample projects** for each model  
✅ **Breaking changes:** None  
✅ **Deprecations:** None  

---

## Supported Models

### 1. GPT-Image-2 (Microsoft Azure OpenAI) — NEW

**Latest generation image generation model from Microsoft.**

- **Class:** `GptImage2Generator`
- **Provider ID:** `foundry-gpt-image-2`
- **CLI:** `t2i --provider foundry-gpt-image-2 "your prompt"`
- **Supported Sizes:** 1024×1024, 1792×1024, 1024×1792
- **Quality:** HD support for enhanced detail
- **Setup:** Azure OpenAI Service deployment
- **Sample:** `scenario-16-gpt-image-2-cloud`

```csharp
// Library usage
var generator = new GptImage2Generator(endpoint, apiKey);
var result = await generator.GenerateAsync("A serene mountain landscape", width: 1024, height: 1024);
```

```bash
# CLI usage
t2i --provider foundry-gpt-image-2 --width 1024 --height 1024 "A serene mountain landscape"
```

---

### 2. GPT-Image-1.5 (Microsoft Azure OpenAI) — DALL-E 3

**Fast, high-quality DALL-E 3 via Azure OpenAI.**

- **Class:** `GptImage1p5Generator`
- **Provider ID:** `foundry-gpt-image-1p5`
- **CLI:** `t2i --provider foundry-gpt-image-1p5 "your prompt"`
- **Supported Sizes:** 1024×1024, 1792×1024, 1024×1792
- **Features:** Automatic size mapping, clear error messages
- **Setup:** Azure OpenAI Service deployment
- **Sample:** `scenario-15-gpt-image-1p5-cloud`

```csharp
// Library usage
var generator = new GptImage1p5Generator(endpoint, apiKey);
var result = await generator.GenerateAsync("A futuristic city", width: 1024, height: 1024);
```

```bash
# CLI usage
t2i --provider foundry-gpt-image-1p5 --width 1024 --height 1024 "A futuristic city"
```

---

### 3. FLUX.2 (Black Forest Labs via Microsoft Foundry)

**Fast, high-quality diffusion model supporting both pro and flex variants.**

- **Class:** `Flux2Generator`
- **Provider ID:** `foundry-flux2`
- **CLI:** `t2i --provider foundry-flux2 "your prompt"`
- **Supported Sizes:** Any (no fixed size constraints)
- **Variants:** `FLUX.2-pro` (default), `FLUX.2-flex`
- **Setup:** Microsoft Foundry API access
- **Sample:** `scenario-03-flux2-cloud`

```csharp
// Library usage
var generator = new Flux2Generator(apiKey, modelId: "FLUX.2-pro");
var result = await generator.GenerateAsync("A magical forest", width: 1024, height: 768);

// With custom model variant
var flexGenerator = new Flux2Generator(apiKey, modelId: "FLUX.2-flex");
```

```bash
# CLI usage
t2i --provider foundry-flux2 --width 1024 --height 768 "A magical forest"

# With custom model variant
t2i config set foundry-flux2.model FLUX.2-flex
t2i --provider foundry-flux2 "A magical forest"
```

---

### 4. MAI-Image-2 (Alibaba via Microsoft Foundry)

**High-resolution, cost-effective image generation with strong prompt understanding.**

- **Class:** `MaiImage2Generator`
- **Provider ID:** `foundry-mai2`
- **CLI:** `t2i --provider foundry-mai2 "your prompt"`
- **Supported Sizes:** Minimum 768×768, auto-bumps smaller requests
- **Variants:** `MAI-Image-2`, `MAI-Image-2e`
- **Setup:** Microsoft Foundry API access
- **Sample:** `scenario-13-mai-image2-cloud`

```csharp
// Library usage
var generator = new MaiImage2Generator(apiKey);
var result = await generator.GenerateAsync("A serene waterfall", width: 1024, height: 1024);

// With custom model variant
var variantGenerator = new MaiImage2Generator(apiKey, modelId: "MAI-Image-2e");
```

```bash
# CLI usage
t2i --provider foundry-mai2 --width 1024 --height 1024 "A serene waterfall"

# With custom model variant
t2i config set foundry-mai2.model MAI-Image-2e
t2i --provider foundry-mai2 "A serene waterfall"
```

---

## CLI Quick Reference

### Generate images with different providers:

```bash
# GPT-Image-2 (NEW)
t2i --provider foundry-gpt-image-2 "Your prompt"

# GPT-Image-1.5 (DALL-E 3)
t2i --provider foundry-gpt-image-1p5 "Your prompt"

# FLUX.2 (Black Forest Labs)
t2i --provider foundry-flux2 "Your prompt"

# MAI-Image-2 (Alibaba)
t2i --provider foundry-mai2 "Your prompt"
```

### With custom dimensions:

```bash
t2i --provider foundry-flux2 --width 1024 --height 768 "Your prompt"
```

### Configure model variants:

```bash
# List current config
t2i config show

# Switch FLUX.2 variant
t2i config set foundry-flux2.model FLUX.2-flex

# Switch MAI-Image-2 variant
t2i config set foundry-mai2.model MAI-Image-2e
```

---

## Library Usage Examples

### Basic usage for each model:

```csharp
// GPT-Image-2
var gptImage2 = new GptImage2Generator(azureEndpoint, azureApiKey);
await gptImage2.GenerateAsync("Prompt", 1024, 1024);

// GPT-Image-1.5
var gptImage1p5 = new GptImage1p5Generator(azureEndpoint, azureApiKey);
await gptImage1p5.GenerateAsync("Prompt", 1024, 1024);

// FLUX.2
var flux2 = new Flux2Generator(foundryApiKey, "FLUX.2-pro");
await flux2.GenerateAsync("Prompt", 1024, 1024);

// MAI-Image-2
var mai = new MaiImage2Generator(foundryApiKey);
await mai.GenerateAsync("Prompt", 1024, 1024);
```

### Register with dependency injection:

```csharp
builder.Services.AddElbrunoText2ImageFoundry()
    .AddGptImage2(azureEndpoint, azureApiKey)
    .AddGptImage1p5(azureEndpoint, azureApiKey)
    .AddFlux2(foundryApiKey)
    .AddMaiImage2(foundryApiKey);

// In your service
private readonly IImageGeneratorFactory _factory;
public MyService(IImageGeneratorFactory factory) => _factory = factory;

var flux2Gen = _factory.CreateGenerator("foundry-flux2");
var result = await flux2Gen.GenerateAsync("Prompt", 1024, 1024);
```

---

## Setup & Configuration

### Prerequisites

- **For GPT-Image-2 and GPT-Image-1.5:** Azure OpenAI Service deployment with credentials
- **For FLUX.2 and MAI-Image-2:** Microsoft Foundry API access with credentials

### Environment Variables

```bash
# GPT-Image-2 and GPT-Image-1.5 (Azure OpenAI)
export GPT_IMAGE_ENDPOINT="https://your-deployment.openai.azure.com/"
export GPT_IMAGE_API_KEY="your-azure-api-key"
export GPT_IMAGE_MODEL="gpt-image-2"  # or "gpt-image-1p5"

# FLUX.2 (Microsoft Foundry)
export FOUNDRY_FLUX2_API_KEY="your-foundry-key"

# MAI-Image-2 (Microsoft Foundry)
export FOUNDRY_MAI2_API_KEY="your-foundry-key"
```

### Configuration (CLI)

```bash
# Initialize configuration
t2i init

# Set providers
t2i config set providers foundry-gpt-image-2 foundry-gpt-image-1p5 foundry-flux2 foundry-mai2

# Set model variants
t2i config set foundry-flux2.model FLUX.2-flex
t2i config set foundry-mai2.model MAI-Image-2e
```

---

## Testing

### Test Coverage

- **Total:** 668 tests passing
- **net8.0:** 298 tests (6 skipped)
- **net10.0:** 370 tests (8 skipped)

### Run tests:

```bash
dotnet test --no-build
```

---

## What's New Since v0.10.0

### Foundry Library

- ✅ Added `GptImage2Generator` class for GPT-Image-2 support
- ✅ Comprehensive unit tests for GPT-Image-2 (all variants)
- ✅ Full integration with existing `IImageGenerator` interface
- ✅ Proper error handling and API compatibility

### CLI Tool

- ✅ Added `foundry-gpt-image-2` provider option
- ✅ Updated CLI help and documentation
- ✅ All four models accessible via `--provider` flag
- ✅ Model variant switching via `t2i config set`

### Samples

- ✅ `scenario-16-gpt-image-2-cloud` — Complete GPT-Image-2 example

---

## Breaking Changes

**None.** This release is fully backward compatible.

---

## Deprecations

**None.**

---

## Dependencies & Platform Support

### .NET Support

- **Foundry Library:** net8.0, net10.0
- **CLI Tool:** net10.0 (respects RollForward: LatestMajor)

### Key Dependencies

| Dependency | Version | Purpose |
|---|---|---|
| Azure.AI.OpenAI | 2.1.* | GPT-Image-1.5 & GPT-Image-2 support |
| Spectre.Console | 0.49.1 | CLI rendering and styling |
| Microsoft.Extensions.* | 9.0.0 | Hosting, configuration, HTTP |
| System.Security.Cryptography | 9.0.0 | DPAPI secret encryption (Windows) |

---

## Migration Guide

### Upgrading from v0.10.0

1. **No code changes required** — Existing projects continue to work unchanged
2. **New option available:** If you want to use GPT-Image-2, simply:

```csharp
var gptImage2 = new GptImage2Generator(endpoint, apiKey);
var result = await gptImage2.GenerateAsync("Your prompt", 1024, 1024);
```

3. **CLI:** Use `--provider foundry-gpt-image-2` for GPT-Image-2

---

## Documentation

- **README:** Updated with all four models
- **Setup Guides:** Individual setup guides for each model
- **Blog:** New blog post on multi-model support in ElBruno.Text2Image
- **Samples:** Four complete sample projects (one per model)

---

## Support & Issues

- **GitHub Issues:** https://github.com/elbruno/ElBruno.Text2Image/issues
- **Discussions:** https://github.com/elbruno/ElBruno.Text2Image/discussions
- **Email:** bruno@elbruno.com

---

## Contributors

- **Bruno Capuano** — Project lead, GPT-Image-2 & GPT-Image-1.5 integration
- **Community** — Testing, feedback, and feature requests

---

## License

MIT — See [LICENSE](LICENSE) for details.

---

**Thank you for using ElBruno.Text2Image! 🚀**
