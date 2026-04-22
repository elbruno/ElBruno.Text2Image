# t2i — AI Text-to-Image CLI

[![NuGet](https://img.shields.io/nuget/v/ElBruno.Text2Image.Cli?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.Text2Image.Cli)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://github.com/elbruno/ElBruno.Text2Image/blob/main/LICENSE)

Cross-platform CLI for AI text-to-image generation. This is the **Lite edition** — cloud providers only, no local inference.

## Quick Install

```bash
dotnet tool install --global ElBruno.Text2Image.Cli
```

Verify installation:

```bash
t2i --version
```

## Quick Start

First-time setup wizard (interactive):

```bash
t2i config
```

Generate an image with your configured provider:

```bash
t2i "a robot painting a landscape"
```

The image is saved as `output.png` (or a timestamped file). Use `--provider` to switch between FLUX.2, MAI-Image-2, GPT-Image-1.5, or GPT-Image-2.

## Providers (Lite Edition)

This **Lite edition** includes **cloud providers only**:

| Provider ID | Name | Provider | Requirements |
|-------------|------|----------|--------------|
| `foundry-flux2` | FLUX.2 Pro (Photorealistic) | Microsoft Foundry | Endpoint + API key |
| `foundry-flux2` (model: FLUX.2-flex) | FLUX.2 Flex (Text-Heavy Design) | Microsoft Foundry | Endpoint + API key |
| `foundry-mai2` | MAI-Image-2 (High Quality) | Microsoft Foundry | Endpoint + API key |
| `foundry-gpt-image-1p5` | GPT-Image-1.5 / DALL-E 3 | Azure OpenAI | Endpoint + API key |
| `foundry-gpt-image-2` | GPT-Image-2 (Next-Gen) | Azure OpenAI | Endpoint + API key |

### Why Lite?

- **Small package**: ~30 MB (vs ~200 MB with local ONNX Runtime providers)
- **No ONNX Runtime native libraries**: Keeps installation fast and lightweight
- **Cloud-first**: Ideal for CI/CD, containers, and environments without local GPU

### Want Local Inference?

For local CPU/GPU inference with Stable Diffusion models, see the upcoming **ElBruno.Text2Image.Cli.Full** package (planned for v0.2.0).

## Usage

```bash
# Generate with default provider
t2i "a futuristic cityscape"

# FLUX.2 Pro (photorealistic)
t2i --provider foundry-flux2 "a robot painting a landscape"

# FLUX.2 Flex (text-heavy design)
t2i --provider foundry-flux2 --set foundry-flux2.model=FLUX.2-flex "a business card with modern design"

# MAI-Image-2 (high-quality)
t2i --provider foundry-mai2 "a serene mountain landscape at sunrise"

# GPT-Image-1.5 / DALL-E 3 (Azure OpenAI)
t2i --provider foundry-gpt-image-1p5 "an impressionist painting of a garden"

# GPT-Image-2 (next-gen model)
t2i --provider foundry-gpt-image-2 "a sci-fi space station in orbit"

# Custom dimensions
t2i "abstract art" --width 1024 --height 1024

# Custom output path
t2i "a sunset" --out sunset.png
```

## Configuration

Set up cloud providers:

```bash
# Interactive setup
t2i config                       # Wizard for all providers
t2i config set foundry-flux2     # Setup FLUX.2
t2i config set foundry-mai2      # Setup MAI-Image-2
t2i config set foundry-gpt-image-1p5  # Setup GPT-Image-1.5
t2i config set foundry-gpt-image-2    # Setup GPT-Image-2

# Microsoft Foundry providers (FLUX.2, MAI-Image-2)
export T2I_FOUNDRY_FLUX2_ENDPOINT="https://your-resource.services.ai.azure.com"
export T2I_FOUNDRY_FLUX2_APIKEY="your-api-key"

# Azure OpenAI providers (GPT-Image-1.5, GPT-Image-2)
export T2I_FOUNDRY_GPT_IMAGE_1P5_ENDPOINT="https://your-resource.openai.azure.com/"
export T2I_FOUNDRY_GPT_IMAGE_1P5_APIKEY="your-api-key"
export T2I_FOUNDRY_GPT_IMAGE_2_ENDPOINT="https://your-resource.openai.azure.com/"
export T2I_FOUNDRY_GPT_IMAGE_2_APIKEY="your-api-key"
```

List available providers:

```bash
t2i providers
```

Run diagnostics:

```bash
t2i doctor
```

Initialize AI agent skill file:

```bash
t2i init
```

This creates `.github/skills/t2i/SKILL.md` and `.claude/skills/t2i/SKILL.md` in your repository so AI coding agents (GitHub Copilot, Claude Code) automatically know how to use the t2i CLI.

## Documentation

Full documentation: [docs/cli-tool.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)

Repository: [github.com/elbruno/ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image)

## License

MIT — see [LICENSE](https://github.com/elbruno/ElBruno.Text2Image/blob/main/LICENSE)
