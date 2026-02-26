# Model Support Matrix

This document lists all models supported by ElBruno.Text2Image and their status.

## Local Models (ONNX Runtime)

| Model | Class | ONNX Source | Steps | VRAM | Status |
|-------|-------|------------|-------|------|--------|
| **Stable Diffusion 1.5** | `StableDiffusion15` | `onnx-community/stable-diffusion-v1-5-ONNX` | 15-50 | ~4 GB | ✅ Implemented |
| **LCM Dreamshaper v7** | `LcmDreamshaperV7` | `TheyCallMeHex/LCM-Dreamshaper-V7-ONNX` | 2-4 | ~4 GB | ✅ Implemented |
| **SDXL Turbo** | `SdxlTurbo` | `elbruno/sdxl-turbo-ONNX` | 1-4 | ~8 GB | ✅ Available |
| **SD 2.1 Base** | `StableDiffusion21` | `elbruno/stable-diffusion-2-1-ONNX` | 15-50 | ~5 GB | ✅ Available |

## Cloud Models (REST API)

| Model | Model ID | Class | Provider | Resolution | Status |
|-------|----------|-------|----------|------------|--------|
| **FLUX.2 Flex** | `FLUX.2-flex` | `Flux2Generator` | Microsoft Foundry | 1024×1024 | ✅ Default |
| **FLUX.2 Pro** | `FLUX.2-pro` | `Flux2Generator` | Microsoft Foundry | 1024×1024 | ✅ Available |

## Model Details

### Stable Diffusion 1.5

- **Resolution**: 512×512 (default)
- **Embedding dimension**: 768
- **Scheduler**: Euler Ancestral Discrete / LMS Discrete
- **License**: CreativeML OpenRAIL-M
- **Download size**: ~5.1 GB
- **Auto-download**: Yes (from HuggingFace)

### LCM Dreamshaper v7

- **Resolution**: 512×512
- **Key advantage**: Only 2-4 inference steps needed (near-instant generation)
- **No CFG needed**: guidance_scale = 1.0
- **Scheduler**: LCM Scheduler
- **License**: CreativeML OpenRAIL-M
- **Based on**: Dreamshaper v7 fine-tune of SD 1.5
- **Auto-download**: Yes (from HuggingFace)

### SDXL Turbo (Available)

- **Class**: `SdxlTurbo`
- **HuggingFace repo**: [`elbruno/sdxl-turbo-ONNX`](https://huggingface.co/elbruno/sdxl-turbo-ONNX)
- **Resolution**: 512×512 (native) to 1024×1024
- **Key advantage**: 1-4 inference steps
- **VRAM requirement**: ~8 GB minimum
- **Architecture**: Dual text encoder (CLIP + OpenCLIP); pipeline uses primary encoder
- **License**: Stability AI Community License
- **Auto-download**: Yes (from HuggingFace)

### SD 2.1 Base (Available)

- **Class**: `StableDiffusion21`
- **HuggingFace repo**: [`elbruno/stable-diffusion-2-1-ONNX`](https://huggingface.co/elbruno/stable-diffusion-2-1-ONNX)
- **Resolution**: 512×512 (base) or 768×768
- **Embedding dimension**: 1024 (uses OpenCLIP ViT-H)
- **License**: CreativeML OpenRAIL-M
- **Auto-download**: Yes (from HuggingFace)

### FLUX.2 (Cloud API)

- **Class**: `Flux2Generator`
- **Provider**: Microsoft Foundry
- **Default model**: `FLUX.2-flex` (text-heavy design and UI prototyping)
- **Resolution**: 1024×1024 (default)
- **Variants**:
  - **FLUX.2 Flex** (`FLUX.2-flex`) — Best-in-class text rendering, logos, UI copy, product packaging. $0.05/megapixel.
  - **FLUX.2 Pro** (`FLUX.2-pro`) — Photorealistic and cinematic-quality image generation.
- **No local model needed**: Runs via REST API
- **Authentication**: API key from Microsoft Foundry
- **Announcement**: [Meet FLUX.2 Flex on Microsoft Foundry](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/meet-flux-2-flex-for-text%E2%80%91heavy-design-and-ui-prototyping-now-available-on-micro/4496041)
- **Setup guide**: [flux2-setup-guide.md](flux2-setup-guide.md)

## Execution Providers

| Provider | GPU Required | Platform | Notes |
|----------|-------------|----------|-------|
| **CPU** | No | All | Works everywhere, slower |
| **CUDA** | NVIDIA GPU | All | Fastest for NVIDIA GPUs |
| **DirectML** | Any GPU | Windows | AMD, Intel, NVIDIA on Windows |

> FLUX.2 does not use execution providers — it runs on Azure cloud infrastructure.

## Default Options

### Local Models

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `NumInferenceSteps` | 20 | 1-100 | More steps = better quality, slower |
| `GuidanceScale` | 7.5 | 1.0-20.0 | Higher = follows prompt more closely |
| `Width` | 512 | Multiple of 8 | Image width in pixels |
| `Height` | 512 | Multiple of 8 | Image height in pixels |
| `Seed` | random | Any int | For reproducible generation |

### Cloud Models (FLUX.2)

| Option | Default | Notes |
|--------|---------|-------|
| `Width` | 1024 | Recommended: 1024 |
| `Height` | 1024 | Recommended: 1024 |
| Response format | `b64_json` | Also supports URL |
