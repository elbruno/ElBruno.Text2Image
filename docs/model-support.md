# Model Support Matrix

This document lists all models supported by ElBruno.Text2Image and their status.

## Supported Models

| Model | Class | ONNX Source | Steps | VRAM | Status |
|-------|-------|------------|-------|------|--------|
| **Stable Diffusion 1.5** | `StableDiffusion15` | `onnx-community/stable-diffusion-v1-5-ONNX` | 15-50 | ~4 GB | ✅ Implemented |
| **LCM Dreamshaper v7** | *(planned)* | `TheyCallMeHex/LCM-Dreamshaper-V7-ONNX` | 2-4 | ~4 GB | 🔜 Planned |
| **SDXL Turbo** | *(planned)* | Needs ONNX export | 1-4 | ~8 GB | 🔜 Planned |
| **SD 2.1 Base** | *(planned)* | Needs ONNX export | 15-50 | ~5 GB | 🔜 Planned |

## Model Details

### Stable Diffusion 1.5

- **Resolution**: 512×512 (default)
- **Embedding dimension**: 768
- **Scheduler**: Euler Ancestral Discrete / LMS Discrete
- **License**: CreativeML OpenRAIL-M
- **Download size**: ~5.1 GB
- **Auto-download**: Yes (from HuggingFace)

### LCM Dreamshaper v7 (Planned)

- **Resolution**: 512×512
- **Key advantage**: Only 2-4 inference steps needed (near-instant generation)
- **No CFG needed**: guidance_scale = 1.0
- **License**: CreativeML OpenRAIL-M
- **Based on**: Dreamshaper v7 fine-tune of SD 1.5

### SDXL Turbo (Planned)

- **Resolution**: 512×512 (native) to 1024×1024
- **Key advantage**: 1-4 inference steps
- **VRAM requirement**: ~8 GB minimum
- **License**: Stability AI Community License

### SD 2.1 Base (Planned)

- **Resolution**: 512×512 (base) or 768×768
- **Embedding dimension**: 1024 (uses OpenCLIP ViT-H)
- **License**: CreativeML OpenRAIL-M
- **Notes**: Better text understanding than SD 1.5

## Execution Providers

| Provider | GPU Required | Platform | Notes |
|----------|-------------|----------|-------|
| **CPU** | No | All | Works everywhere, slower |
| **CUDA** | NVIDIA GPU | All | Fastest for NVIDIA GPUs |
| **DirectML** | Any GPU | Windows | AMD, Intel, NVIDIA on Windows |

## Default Options

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `NumInferenceSteps` | 20 | 1-100 | More steps = better quality, slower |
| `GuidanceScale` | 7.5 | 1.0-20.0 | Higher = follows prompt more closely |
| `Width` | 512 | Multiple of 8 | Image width in pixels |
| `Height` | 512 | Multiple of 8 | Image height in pixels |
| `Seed` | random | Any int | For reproducible generation |
