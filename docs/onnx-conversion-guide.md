# ONNX Model Conversion Guide

This document describes how to convert Hugging Face vision-language models to ONNX format for use with the ElBruno.Text2Image library.

## Prerequisites

```bash
pip install optimum[onnxruntime] transformers torch pillow
```

## Models Already in ONNX Format

These models already have ONNX weights available on Hugging Face and do **not** need conversion:

| Model | ONNX Source | Notes |
|-------|------------|-------|
| ViT-GPT2 | [Xenova/vit-gpt2-image-captioning](https://huggingface.co/Xenova/vit-gpt2-image-captioning) | Encoder + decoder, quantized variants available |
| BLIP Base | [onnx-community/Salesforce_blip-image-captioning-base](https://huggingface.co/onnx-community/Salesforce_blip-image-captioning-base) | Vision model + text decoder split |
| Florence-2 Base | [onnx-community/Florence-2-base](https://huggingface.co/onnx-community/Florence-2-base) | Multi-part encoder/decoder |

## Converting GIT (GenerativeImage2Text) to ONNX

The Microsoft GIT model can be exported using `optimum-cli`:

```bash
# Install optimum
pip install optimum[onnxruntime]

# Export GIT base COCO to ONNX
optimum-cli export onnx \
  --model microsoft/git-base-coco \
  --task image-to-text \
  ./git-base-coco-onnx/

# Validate the export
python scripts/validate_onnx_export.py --model-path ./git-base-coco-onnx/ --original microsoft/git-base-coco
```

Alternatively, use the provided script:

```bash
python scripts/export_git_onnx.py
```

### GIT ONNX Structure

After export, you'll find:
- `encoder_model.onnx` — CLIP ViT image encoder
- `decoder_model.onnx` / `decoder_model_merged.onnx` — Text decoder with optional KV-cache
- `preprocessor_config.json` — Image preprocessing config
- `tokenizer.json` — Tokenizer vocabulary

### GIT Input/Output Details

**Encoder:**
- Input: `pixel_values` — shape `[batch, 3, 224, 224]`, float32
- Output: `last_hidden_state` — shape `[batch, seq_len, hidden_size]`

**Decoder:**
- Inputs: `input_ids` (long), `encoder_hidden_states` (float32), optional KV-cache
- Output: `logits` — shape `[batch, seq_len, vocab_size]`

## Converting Moondream to ONNX

> ⚠️ **Complex** — Moondream uses a custom architecture with `trust_remote_code=True`, making standard `optimum-cli export` unreliable. A custom export script is needed.

```bash
python scripts/export_moondream_onnx.py
```

See `scripts/export_moondream_onnx.py` for the full conversion pipeline.

### Moondream Export Challenges

1. **Custom architecture**: Requires `trust_remote_code=True`
2. **Large model size**: ~1.8B params, consider quantization
3. **Dynamic shapes**: Variable-length text generation requires careful dynamic axis configuration
4. **Multi-modal inputs**: Both image and text embeddings need to be handled

## Uploading ONNX Models to Hugging Face

After validating the exported model, upload to a Hugging Face repository:

```bash
python scripts/upload_to_huggingface.py \
  --model-path ./git-base-coco-onnx/ \
  --repo-id elbruno/git-base-coco-onnx \
  --commit-message "Add ONNX weights for git-base-coco"
```

Or manually:

```bash
# Install huggingface_hub CLI
pip install huggingface_hub

# Login
huggingface-cli login

# Create repo and upload
huggingface-cli repo create git-base-coco-onnx --type model
huggingface-cli upload elbruno/git-base-coco-onnx ./git-base-coco-onnx/
```

## Validating ONNX Exports

Always validate that the ONNX model produces the same output as the original PyTorch model:

```python
import numpy as np
from PIL import Image
from transformers import AutoProcessor, AutoModelForCausalLM
import onnxruntime as ort

# Load original model
processor = AutoProcessor.from_pretrained("microsoft/git-base-coco")
model = AutoModelForCausalLM.from_pretrained("microsoft/git-base-coco")

# Run PyTorch inference
image = Image.open("test.jpg")
inputs = processor(images=image, return_tensors="pt")
with torch.no_grad():
    outputs = model.generate(**inputs, max_length=50)
pytorch_caption = processor.decode(outputs[0], skip_special_tokens=True)

# Run ONNX inference
encoder_session = ort.InferenceSession("git-base-coco-onnx/encoder_model.onnx")
decoder_session = ort.InferenceSession("git-base-coco-onnx/decoder_model.onnx")
# ... run inference ...

# Compare outputs
print(f"PyTorch: {pytorch_caption}")
print(f"ONNX: {onnx_caption}")
```

## Image Preprocessing Requirements

Each model expects specific image preprocessing:

| Model | Size | Mean | Std | Notes |
|-------|------|------|-----|-------|
| ViT-GPT2 | 224×224 | [0.5, 0.5, 0.5] | [0.5, 0.5, 0.5] | Resize shortest edge, center crop |
| BLIP | 384×384 | [0.48145, 0.45783, 0.40821] | [0.26863, 0.26130, 0.27578] | CLIP normalization |
| GIT | 224×224 | [0.48145, 0.45783, 0.40821] | [0.26863, 0.26130, 0.27578] | CLIP normalization |
| Florence-2 | 768×768 | [0.485, 0.456, 0.406] | [0.229, 0.224, 0.225] | ImageNet normalization |

## Quantization

For smaller model sizes and faster CPU inference, consider quantizing ONNX models:

```bash
# Dynamic quantization (easiest, good for CPU)
python -m onnxruntime.quantization.quantize \
  --input encoder_model.onnx \
  --output encoder_model_quantized.onnx \
  --per_channel

# Or use optimum
optimum-cli export onnx \
  --model microsoft/git-base-coco \
  --task image-to-text \
  --optimize O2 \
  ./git-base-coco-onnx-optimized/
```

## Troubleshooting

### Common Issues

1. **"Model type X is not supported"** — Check if Optimum supports the model. Florence-2 and Moondream may require custom export configs.

2. **Shape mismatch errors** — Ensure dynamic axes are configured correctly for variable sequence lengths.

3. **Large model files** — Use Git LFS for files > 50MB when uploading to Hugging Face:
   ```bash
   git lfs track "*.onnx"
   git lfs track "*.onnx.data"
   ```

4. **Missing KV-cache outputs** — Use `--task image-to-text-with-past` with optimum-cli to export models with KV-cache support for efficient autoregressive decoding.
