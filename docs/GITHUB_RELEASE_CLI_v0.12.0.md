# ElBruno.Text2Image CLI v0.12.0 — Multi-Model Support

**Release Date:** April 21, 2026  
**Tag:** `cli-v0.12.0`  
**Version:** 0.12.0  
**Tool Name:** `t2i`

## What's New

This release brings **GPT-Image-2** (Microsoft's latest image generation model) to the CLI, making **four production-ready cloud models** available through a single unified interface.

### ✅ New Features

- **GPT-Image-2 provider** — `--provider foundry-gpt-image-2`
  - Latest generation image synthesis
  - Azure OpenAI Service integration
  - Identical CLI experience as other providers
  - Fixed sizes: 1024×1024, 1792×1024, 1024×1792

### 🎯 Model Support

The CLI now supports **four cloud-based models** via `--provider` flag:

1. **foundry-gpt-image-2** (NEW) — Microsoft's latest model
2. **foundry-gpt-image-1p5** — DALL-E 3 via Azure OpenAI
3. **foundry-flux2** — Black Forest Labs (pro and flex)
4. **foundry-mai2** — Alibaba (standard and enhanced)

### 📊 Test Coverage

- **668 total tests passing**
- **net10.0** for CLI tool
- All models have comprehensive testing

## Installation

### Global .NET Tool

```bash
# Install
dotnet tool install --global ElBruno.Text2Image.Cli --version 0.12.0

# Update (if already installed)
dotnet tool update --global ElBruno.Text2Image.Cli --version 0.12.0
```

### Verify Installation

```bash
t2i --version
t2i --help
```

## Quick Start

### Generate with each model

```bash
# GPT-Image-2 (NEW)
t2i --provider foundry-gpt-image-2 "A serene mountain landscape"

# GPT-Image-1.5
t2i --provider foundry-gpt-image-1p5 "A futuristic city"

# FLUX.2
t2i --provider foundry-flux2 "A magical forest"

# MAI-Image-2
t2i --provider foundry-mai2 "A calm waterfall"
```

### With custom dimensions

```bash
t2i --provider foundry-flux2 --width 1024 --height 768 "Your prompt"
```

### Configure model variants

```bash
# Switch FLUX.2 variant
t2i config set foundry-flux2.model FLUX.2-flex

# Switch MAI-Image-2 variant
t2i config set foundry-mai2.model MAI-Image-2e

# View current config
t2i config show
```

## Setup

### Prerequisites

- **For GPT-Image-2 & GPT-Image-1.5:** Azure OpenAI Service deployment
- **For FLUX.2 & MAI-Image-2:** Microsoft Foundry API access

### Initialize Configuration

```bash
t2i init
```

This will guide you through setting up providers and credentials.

### Environment Variables

```bash
# GPT-Image models (Azure OpenAI)
export GPT_IMAGE_ENDPOINT="https://your-deployment.openai.azure.com/"
export GPT_IMAGE_API_KEY="your-azure-api-key"

# FLUX.2 (Microsoft Foundry)
export FOUNDRY_FLUX2_API_KEY="your-foundry-key"

# MAI-Image-2 (Microsoft Foundry)
export FOUNDRY_MAI2_API_KEY="your-foundry-key"
```

## Command Reference

### Basic Generation

```bash
# Simple prompt with defaults
t2i "Your image prompt"

# Specify provider
t2i --provider foundry-flux2 "Your prompt"

# Custom dimensions
t2i --provider foundry-flux2 --width 1024 --height 768 "Your prompt"

# Output to specific file
t2i --output my-image.png "Your prompt"

# Different format
t2i --format jpeg "Your prompt"
```

### Configuration

```bash
# Initialize interactive setup
t2i init

# Show current configuration
t2i config show

# Set specific values
t2i config set providers foundry-gpt-image-2 foundry-flux2
t2i config set foundry-flux2.model FLUX.2-flex
t2i config set output.format png

# Reset configuration
t2i config reset
```

### Advanced Usage

```bash
# Batch generation (multiple prompts)
t2i --batch prompts.txt

# Model comparison (generate with all models)
t2i --compare "Your prompt"

# Progress reporting
t2i --progress "Your prompt"

# Diagnostics
t2i diagnostics
```

## Documentation

- 📖 [README](https://github.com/elbruno/ElBruno.Text2Image) — Feature overview and getting started
- 📝 [Release Notes](https://github.com/elbruno/ElBruno.Text2Image/blob/main/RELEASE_NOTES_v0.11-v0.12.md) — Comprehensive multi-model guide
- 📚 [Samples](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples) — Complete examples for each model
- 🔧 [Setup Guides](https://github.com/elbruno/ElBruno.Text2Image/tree/main/docs) — Model-specific configuration

## Breaking Changes

**None.** Fully backward compatible with v0.11.0 CLI commands.

## Deprecations

**None.**

## Dependencies

| Component | Version |
|-----------|---------|
| .NET Runtime | net10.0 (RollForward: LatestMajor) |
| Spectre.Console | 0.49.1 |
| Azure.AI.OpenAI | 2.1.* |

## Tested Platforms

- ✅ Windows (x64, x86, ARM64)
- ✅ macOS (x64, ARM64)
- ✅ Linux (x64, ARM64, ARM32)

## Migration from v0.11.0

No code or configuration changes needed. Simply:

1. Update the tool: `dotnet tool update --global ElBruno.Text2Image.Cli`
2. Use new provider: `t2i --provider foundry-gpt-image-2 "prompt"`

All existing commands continue to work unchanged.

## Known Issues

None. All 668 tests passing across all platforms.

## Contributors

- **Bruno Capuano** — Project lead, GPT-Image-2 CLI integration

## Support

- 🐛 [Report Issues](https://github.com/elbruno/ElBruno.Text2Image/issues)
- 💬 [Join Discussions](https://github.com/elbruno/ElBruno.Text2Image/discussions)
- 📧 [Email](mailto:bruno@elbruno.com)

---

**Thank you for using t2i! 🚀**

For detailed information on all four models, see [RELEASE_NOTES_v0.11-v0.12.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/RELEASE_NOTES_v0.11-v0.12.md)
