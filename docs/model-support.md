# Model Support Matrix

## Supported Models

| Model | Status | ONNX Source | Parameters | Task | Quality |
|-------|--------|------------|------------|------|---------|
| **ViT-GPT2** | ✅ Ready | [Xenova/vit-gpt2-image-captioning](https://huggingface.co/Xenova/vit-gpt2-image-captioning) | ~300M | Caption | Good for simple captions |
| **BLIP Base** | ✅ Ready | [onnx-community/Salesforce_blip-image-captioning-base](https://huggingface.co/onnx-community/Salesforce_blip-image-captioning-base) | ~385M | Caption (conditional/unconditional) | Great for captioning |
| **Florence-2 Base** | 🔜 Planned | [onnx-community/Florence-2-base](https://huggingface.co/onnx-community/Florence-2-base) | ~230M | Multi-task (caption, OD, OCR) | Excellent multi-task |
| **GIT Base COCO** | 🔜 Planned | Needs ONNX export | ~130M | Caption | Good for COCO-style |
| **Moondream 2** | 🔜 Planned | Needs ONNX export | ~1.8B | Caption, VQA, Detection | Best quality, largest |

## Model Comparison

### ViT-GPT2
- **Best for**: Quick, simple image captioning
- **Architecture**: ViT encoder + GPT-2 decoder
- **Preprocessing**: 224×224, mean/std = 0.5
- **Tokenizer**: GPT-2 BPE
- **ONNX files**: `encoder_model.onnx` + `decoder_model_merged.onnx`
- **Quantized variants**: ✅ Available

### BLIP Base (Salesforce)
- **Best for**: Flexible captioning with optional text prompts
- **Architecture**: ViT encoder + text decoder
- **Preprocessing**: 384×384, CLIP normalization
- **Tokenizer**: BERT WordPiece
- **ONNX files**: `split_0.onnx` (vision) + `split_1.onnx` (decoder)
- **Special feature**: Conditional captioning (provide a text prompt to guide the caption)

### Florence-2 (Microsoft)
- **Best for**: Multi-task vision understanding
- **Architecture**: DaViT encoder + sequence-to-sequence decoder
- **Tasks**: `<CAPTION>`, `<DETAILED_CAPTION>`, `<OD>`, `<OCR>`, `<OCR_WITH_REGION>`
- **Preprocessing**: 768×768, ImageNet normalization
- **Special feature**: Prompt-based multi-task — one model for many vision tasks

### GIT Base COCO (Microsoft)
- **Best for**: Image captioning with CLIP-based vision
- **Architecture**: CLIP ViT encoder + GPT-2-style decoder
- **Preprocessing**: 224×224, CLIP normalization
- **Special feature**: Strong zero-shot performance

### Moondream 2
- **Best for**: Highest quality vision-language understanding
- **Architecture**: SigLIP encoder + Phi-1.5 decoder
- **Tasks**: Captioning, VQA, object detection, pointing
- **Size**: ~1.8B parameters (quantized variants available)
- **Special feature**: Runs efficiently with quantization, supports complex queries

## Choosing a Model

| Use Case | Recommended Model | Why |
|----------|------------------|-----|
| Quick prototype | ViT-GPT2 | Smallest, fastest download, simplest API |
| Production captioning | BLIP Base | Best quality/size ratio, conditional captioning |
| Multi-task vision | Florence-2 | One model for captioning, detection, OCR |
| Highest quality | Moondream 2 | Best understanding, but largest |
| Edge/mobile | ViT-GPT2 (quantized) | Smallest footprint |
