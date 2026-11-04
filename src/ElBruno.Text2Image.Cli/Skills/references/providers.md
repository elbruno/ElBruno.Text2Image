<!-- t2i:managed-skill-reference -->

# Provider selection

Nine current cloud providers are available in the Lite CLI. Run the installed
tool's `t2i providers` command to see providers and their configuration status.

| Provider ID | Model/service | Guidance |
|---|---|---|
| `foundry-flux2` | FLUX.2 via Microsoft Foundry | General high-quality generation; supports model selection such as FLUX.2 Pro or Flex. |
| `foundry-mai25` | MAI-Image-2.5 via Microsoft Foundry | High-quality image generation. |
| `foundry-mai25-flash` | MAI-Image-2.5-Flash via Microsoft Foundry | Choose for faster iteration. |
| `foundry-mai26` | MAI-Image-2.6 via Microsoft Foundry | Preview text-to-image provider; supports optional automatic aspect ratio and web grounding. |
| `foundry-mai26-flash` | MAI-Image-2.6-Flash via Microsoft Foundry | Preview text-to-image provider for faster iteration; supports optional automatic aspect ratio and web grounding. |
| `foundry-gpt-image-1p5` | GPT-Image-1.5 via Azure OpenAI | Use when the user's Azure OpenAI deployment provides this model. |
| `foundry-gpt-image-2` | GPT-Image-2 via Azure OpenAI | Compatibility provider for deployments using this model. |
| `foundry-gpt-image-25-sunburst` | GPT-Image-2.5-Sunburst via Azure OpenAI | Use for high-fidelity results when available in the user's region and deployment. |
| `foundry-gpt-image-25-flare` | GPT-Image-2.5-Flare via Azure OpenAI | Use for faster GPT-Image-2.5 iteration when available. |

`foundry-mai2` is retired. Do not use it; choose a current provider instead.
Do not assume preview models are enabled in the user's Azure region or
subscription. Check the deployment configuration and keep an available
provider as a fallback.

## Reference images and model options

| Provider | Reference images | Additional options |
|---|---|---|
| `foundry-flux2` | Up to 8 | Reference-image generation |
| `foundry-gpt-image-*` | Up to 16 | Optional PNG `--mask` and `--input-fidelity low|high` |
| `foundry-mai25`, `foundry-mai25-flash` | Up to 5 PNG/JPEG images | Reference-image generation |
| `foundry-mai26`, `foundry-mai26-flash` | Not currently supported | Optional `--auto-aspect-ratio` and `--web-grounding` |

Use `--image` (repeatable) with a local PNG/JPEG/WebP file, an HTTPS URL, or
a supported data URI. For exact option and size limits, see the installed
CLI's `t2i providers` output and the [CLI guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md).

Examples:

```sh
t2i "a product landing page with readable headline text" \
  --provider foundry-flux2 \
  --width 1024 --height 1024 \
  --out landing-page.png

t2i "a space station in orbit" \
  --provider foundry-gpt-image-25-sunburst \
  --timeout 300 \
  --out station.png

t2i "a current city event poster" \
  --provider foundry-mai26 \
  --auto-aspect-ratio --web-grounding \
  --out poster.png
```

Provider parameters and model availability can change between CLI releases.
Use `t2i providers` and the [CLI guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)
as the authoritative sources for the installed version.
