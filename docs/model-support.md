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
| `foundry-mai2` | MAI-Image-2 (retired) | Microsoft Foundry | Migration entry only |
| `foundry-mai25` | MAI-Image-2.5 (Preview) | Microsoft Foundry | `MAI-Image-2.5` |
| `foundry-mai25-flash` | MAI-Image-2.5-Flash (Preview) | Microsoft Foundry | `MAI-Image-2.5-Flash` |
| `foundry-mai26` | MAI-Image-2.6 (Preview) | Microsoft Foundry | `MAI-Image-2.6` |
| `foundry-mai26-flash` | MAI-Image-2.6-Flash (Preview) | Microsoft Foundry | `MAI-Image-2.6-Flash` |
| `foundry-gpt-image-1p5` | GPT-Image-1.5 | Azure OpenAI | `gpt-image-1.5` |
| `foundry-gpt-image-2` | GPT-Image-2 (compatibility) | Azure OpenAI | `gpt-image-2` |
| `foundry-gpt-image-25-sunburst` | GPT-Image-2.5-Sunburst | Azure OpenAI | `gpt-image-2.5-sunburst` |
| `foundry-gpt-image-25-flare` | GPT-Image-2.5-Flare | Azure OpenAI | `gpt-image-2.5-flare` |

All listed MAI models are currently Preview. The CLI exposes the active providers shown above; `foundry-mai2` remains only as a compatibility migration entry and fails clearly without fallback. Configure each provider's `model` setting with the deployment name from Microsoft Foundry.

### MAI Image request limits

Microsoft's current MAI Image API specification applies these generation limits:

- `width` and `height` must each be at least 768 pixels.
- MAI-Image-2.5 and 2.5-Flash: `width × height` must not exceed 1,048,576 pixels.
- MAI-Image-2.6 and 2.6-Flash: `width × height` must not exceed 2,359,296 pixels.
- The output format is PNG.
- A prompt has a maximum context length of 32,000 tokens.
- The 2.6 models additionally accept `auto_aspect_ratio` and `web_grounding`. These are opt-in and are not sent to the 2.5 models.

The 2.5 and 2.5-Flash providers support image-to-image edits. The 2.6 providers currently expose text-to-image only, despite Foundry's support for editing on the service.

### GPT Image notes

GPT-Image-1.5 and GPT-Image-2 are Azure OpenAI image models, not DALL-E 3 deployments. DALL-E 3 was retired for Azure OpenAI deployments on March 4, 2026. Check your region, access level, and deployment availability before configuring either GPT Image provider.

## Official references

- [Deploy and use MAI image models in Microsoft Foundry](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/use-foundry-models-mai-image)
- [Azure OpenAI image generation models](https://learn.microsoft.com/azure/foundry/openai/how-to/dall-e)
- [FLUX.2 setup guide](flux2-setup-guide.md)
- [MAI-Image-2 setup guide](mai-image-2-setup-guide.md)
