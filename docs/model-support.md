# Model support matrix

## Local models (library packages)

| Model | Class | Typical steps | Status |
|---|---|---:|---|
| Stable Diffusion 1.5 | `StableDiffusion15` | 15–50 | Available |
| LCM Dreamshaper v7 | `LcmDreamshaperV7` | 2–4 | Available |
| SDXL Turbo | `SdxlTurbo` | 1–4 | Available |
| SD 2.1 Base | `StableDiffusion21` | 15–50 | Available |

Local inference is available through the library's CPU, CUDA, and DirectML packages. It is not part of the cloud-first `ElBruno.Text2Image.Cli` Lite tool.

## Cloud models (CLI and Foundry package)

| CLI provider | Model | Service | Default model ID |
|---|---|---|---|
| `foundry-flux2` | FLUX.2 Pro/Flex | Microsoft Foundry | `FLUX.2-pro` |
| `foundry-mai2` | MAI-Image-2 | Microsoft Foundry | `MAI-Image-2` |
| `foundry-mai25` | MAI-Image-2.5 (Preview) | Microsoft Foundry | `MAI-Image-2.5` |
| `foundry-mai25-flash` | MAI-Image-2.5-Flash (Preview) | Microsoft Foundry | `MAI-Image-2.5-Flash` |
| `foundry-gpt-image-1p5` | GPT-Image-1.5 | Azure OpenAI | `gpt-image-1.5` |
| `foundry-gpt-image-2` | GPT-Image-2 | Azure OpenAI | `gpt-image-2` |

All MAI image models are currently Preview. Microsoft Foundry documents `MAI-Image-2`, `MAI-Image-2e`, `MAI-Image-2.5`, and `MAI-Image-2.5-Flash` through its MAI Image API. The CLI exposes the six providers shown above (including MAI-Image-2, 2.5, and 2.5-Flash); use a provider's `model` setting to select a deployed model compatible with that provider.

### MAI Image request limits

Microsoft's current MAI Image API specification applies these generation limits:

- `width` and `height` must each be at least 768 pixels.
- `width × height` must not exceed 1,048,576 pixels.
- The output format is PNG.
- A prompt has a maximum context length of 32,000 tokens.

The 2.5 and 2.5-Flash models also support image-to-image edits in the provider API. `t2i` currently generates text-to-image requests only.

### GPT Image notes

GPT-Image-1.5 and GPT-Image-2 are Azure OpenAI image models, not DALL-E 3 deployments. DALL-E 3 was retired for Azure OpenAI deployments on March 4, 2026. Check your region, access level, and deployment availability before configuring either GPT Image provider.

## Official references

- [Deploy and use MAI image models in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/foundry-models/how-to/use-foundry-models-mai)
- [Azure OpenAI image generation models](https://learn.microsoft.com/azure/foundry/openai/how-to/dall-e)
- [FLUX.2 setup guide](flux2-setup-guide.md)
- [MAI-Image-2 setup guide](mai-image-2-setup-guide.md)
