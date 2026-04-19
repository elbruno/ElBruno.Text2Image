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

First-time setup wizard:

```bash
t2i config
```

Generate an image:

```bash
t2i "a robot painting a landscape"
```

The image is saved as `output.png` (or a timestamped file).

## Providers (Lite Edition)

This **Lite edition** includes **cloud providers only**:

| Provider ID | Name | Requirements |
|-------------|------|--------------|
| `foundry-flux2` | FLUX.2 Pro (Cloud) | Microsoft Foundry endpoint + API key |
| `foundry-mai2` | MAI-Image-2 (Cloud) | Microsoft Foundry endpoint + API key |

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

# Specify provider
t2i "a mountain landscape" --provider foundry-flux2

# Custom dimensions
t2i "abstract art" --width 1024 --height 1024

# Custom output path
t2i "a sunset" --out sunset.png
```

## Configuration

Set up cloud providers:

```bash
# Interactive setup
t2i secrets set foundry-flux2

# Environment variables
export T2I_FOUNDRY_FLUX2_ENDPOINT="https://your-resource.services.ai.azure.com"
export T2I_FOUNDRY_FLUX2_APIKEY="your-api-key"
```

List available providers:

```bash
t2i providers
```

Run diagnostics:

```bash
t2i doctor
```

## Documentation

Full documentation: [docs/cli-tool.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)

Repository: [github.com/elbruno/ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image)

## License

MIT — see [LICENSE](https://github.com/elbruno/ElBruno.Text2Image/blob/main/LICENSE)
