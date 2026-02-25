# ONNX Conversion Guide for Stable Diffusion Models

This document describes how to convert Stable Diffusion models to ONNX format for use with ElBruno.Text2Image.

## Prerequisites

```bash
pip install optimum[onnxruntime] diffusers transformers torch
```

## Pre-exported ONNX Models

Some models already have ONNX exports available on HuggingFace. The library uses these by default:

| Model | ONNX Source | Size |
|-------|------------|------|
| SD 1.5 | `onnx-community/stable-diffusion-v1-5-ONNX` | ~5.1 GB |
| LCM Dreamshaper v7 | `TheyCallMeHex/LCM-Dreamshaper-V7-ONNX` | ~5 GB |

## Exporting to ONNX with Optimum CLI

For models without pre-exported ONNX versions, use the Hugging Face Optimum CLI:

### Stable Diffusion 1.5

```bash
optimum-cli export onnx \
  --model stable-diffusion-v1-5/stable-diffusion-v1-5 \
  --task stable-diffusion \
  sd_v15_onnx/
```

### Stable Diffusion 2.1

```bash
optimum-cli export onnx \
  --model stabilityai/stable-diffusion-2-1-base \
  --task stable-diffusion \
  sd_v21_onnx/
```

### SDXL Turbo

```bash
optimum-cli export onnx \
  --model stabilityai/sdxl-turbo \
  --task stable-diffusion-xl \
  sdxl_turbo_onnx/
```

## Expected ONNX File Structure

After export, you should have the following directory structure:

```
model_onnx/
├── text_encoder/
│   └── model.onnx          # CLIP text encoder
├── unet/
│   ├── model.onnx           # UNet denoiser (largest file, ~1.6-3.4 GB)
│   └── weights.pb           # External weights (if separated)
├── vae_decoder/
│   └── model.onnx           # VAE decoder (latents → image)
├── vae_encoder/
│   └── model.onnx           # VAE encoder (image → latents, optional)
├── tokenizer/
│   ├── vocab.json            # CLIP tokenizer vocabulary
│   ├── merges.txt            # BPE merge rules
│   ├── special_tokens_map.json
│   └── tokenizer_config.json
├── scheduler/
│   └── scheduler_config.json # Scheduler configuration
├── safety_checker/
│   └── model.onnx           # NSFW safety checker (optional)
└── model_index.json          # Pipeline configuration
```

## FP16 Export (Smaller, Faster on GPU)

To export in FP16 for GPU-accelerated inference:

```bash
optimum-cli export onnx \
  --model stable-diffusion-v1-5/stable-diffusion-v1-5 \
  --task stable-diffusion \
  --fp16 \
  sd_v15_fp16_onnx/
```

## Validating the Export

Run a quick test with the Python diffusers library:

```python
from optimum.onnxruntime import ORTStableDiffusionPipeline

pipe = ORTStableDiffusionPipeline.from_pretrained("./sd_v15_onnx/")
image = pipe("a cat sitting on a windowsill").images[0]
image.save("test_output.png")
```

## Uploading to HuggingFace

After validating, upload the ONNX model to HuggingFace:

```bash
python scripts/upload_to_huggingface.py \
  --model-dir ./sd_v15_onnx/ \
  --repo-id elbruno/stable-diffusion-v1-5-onnx \
  --token $HF_TOKEN
```

See `scripts/upload_to_huggingface.py` for the full upload script.

## Troubleshooting

### OOM during export
The export requires ~16GB RAM. If you run out of memory, try:
- Close other applications
- Use a machine with more RAM
- Export individual components separately

### Missing `weights.pb`
Some exports separate large weights into a `.pb` file alongside `model.onnx`. Both files must be present.

### Tokenizer files
The tokenizer directory must contain `vocab.json` and `merges.txt`. These are critical for the C# CLIP tokenizer implementation.
