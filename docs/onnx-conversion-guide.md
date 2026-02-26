# ONNX Conversion Guide for Stable Diffusion Models

This document describes how to convert Stable Diffusion models to ONNX format for use with ElBruno.Text2Image.

## Prerequisites

```bash
python -m venv .venv
.venv\Scripts\Activate.ps1  # Windows
# source .venv/bin/activate  # Linux/macOS

pip install optimum[onnxruntime] diffusers transformers torch huggingface_hub
```

## Pre-exported ONNX Models

The following models have ONNX exports hosted on HuggingFace and are used by default:

| Model | ONNX Source | Size | Exported by |
|-------|------------|------|-------------|
| SD 1.5 | `onnx-community/stable-diffusion-v1-5-ONNX` | ~5.1 GB | Community |
| LCM Dreamshaper v7 | `TheyCallMeHex/LCM-Dreamshaper-V7-ONNX` | ~5 GB | Community |
| SD 2.1 | `elbruno/stable-diffusion-2-1-ONNX` | ~4.9 GB | ElBruno (this project) |
| SDXL Turbo | `elbruno/sdxl-turbo-ONNX` | ~13 GB | ElBruno (this project) |

## How We Exported SD 2.1 and SDXL Turbo

The following commands were used to export the models in this project:

### Stable Diffusion 2.1

```bash
python -m optimum.exporters.onnx \
  --model sd2-community/stable-diffusion-2-1 \
  --task text-to-image \
  onnx_exports/sd21
```

**Source model:** `sd2-community/stable-diffusion-2-1` (the original `stabilityai/stable-diffusion-2-1-base` repo was removed)

**Output structure:**
```
sd21/
├── text_encoder/model.onnx         # ~1.3 GB
├── unet/model.onnx                 # ~1 MB (metadata)
├── unet/model.onnx_data            # ~3.3 GB (weights)
├── vae_decoder/model.onnx          # ~189 MB
├── vae_encoder/model.onnx          # ~130 MB
├── tokenizer/{vocab.json, merges.txt, ...}
├── scheduler/scheduler_config.json
└── model_index.json
```

### SDXL Turbo

```bash
python -m optimum.exporters.onnx \
  --model stabilityai/sdxl-turbo \
  --task text-to-image \
  onnx_exports/sdxl-turbo
```

**Output structure:**
```
sdxl-turbo/
├── text_encoder/model.onnx         # ~470 MB (primary CLIP)
├── text_encoder_2/model.onnx       # ~0.7 MB (metadata)
├── text_encoder_2/model.onnx_data  # ~2.6 GB (OpenCLIP ViT-bigG)
├── unet/model.onnx                 # ~3.4 MB (metadata)
├── unet/model.onnx_data            # ~9.8 GB (weights)
├── vae_decoder/model.onnx          # ~189 MB
├── vae_encoder/model.onnx          # ~130 MB
├── tokenizer/{vocab.json, merges.txt, ...}
├── tokenizer_2/{vocab.json, merges.txt, ...}
├── scheduler/scheduler_config.json
└── model_index.json
```

**Note:** SDXL uses a dual text encoder architecture (`text_encoder` + `text_encoder_2`).

### Uploading to HuggingFace

After export, upload using the Python script or the huggingface_hub library:

```python
from huggingface_hub import HfApi
api = HfApi()

api.create_repo(repo_id="elbruno/stable-diffusion-2-1-ONNX", repo_type="model", exist_ok=True)
api.upload_folder(
    folder_path="onnx_exports/sd21",
    repo_id="elbruno/stable-diffusion-2-1-ONNX",
    repo_type="model",
    commit_message="feat: add ONNX export of Stable Diffusion 2.1 base"
)
```

Or use the provided script:

```bash
python scripts/upload_to_huggingface.py \
  --model-dir ./onnx_exports/sd21 \
  --repo-id elbruno/stable-diffusion-2-1-ONNX \
  --token $HF_TOKEN
```

## Exporting Other Models

For models without pre-exported ONNX versions, use the Hugging Face Optimum exporter:

### Standard Stable Diffusion

```bash
python -m optimum.exporters.onnx \
  --model <model-id> \
  --task text-to-image \
  output_dir/
```

### FP16 Export (Smaller, Faster on GPU)

```bash
python -m optimum.exporters.onnx \
  --model <model-id> \
  --task text-to-image \
  --dtype fp16 \
  output_dir/
```

## Validating the Export

Run a quick test with the Python diffusers library:

```python
from optimum.onnxruntime import ORTStableDiffusionPipeline

pipe = ORTStableDiffusionPipeline.from_pretrained("./onnx_exports/sd21/")
image = pipe("a cat sitting on a windowsill").images[0]
image.save("test_output.png")
```

**Note:** Validation warnings about small numerical differences (atol) are normal for diffusion models and do not affect generation quality.

## Troubleshooting

### OOM during export
The export requires ~16GB RAM. If you run out of memory, try:
- Close other applications
- Use a machine with more RAM
- Install `accelerate`: `pip install accelerate` (enables low-memory mode)

### Missing `model.onnx_data`
Some exports separate large weights into a `.onnx_data` file alongside `model.onnx`. Both files must be present.

### Tokenizer files
The tokenizer directory must contain `vocab.json` and `merges.txt`. These are critical for the C# CLIP tokenizer implementation.

### Model not found on HuggingFace
Some original model repos may be removed or gated. Check for community mirrors (e.g., `sd2-community/stable-diffusion-2-1` instead of `stabilityai/stable-diffusion-2-1-base`).
