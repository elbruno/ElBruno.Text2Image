<!-- t2i:managed-skill-reference -->

# Provider selection

The Lite CLI includes cloud providers. Run `t2i providers` to see the providers
available in the installed tool and their configuration status.

| Provider ID | Model/service | Guidance |
|---|---|---|
| `foundry-flux2` | FLUX.2 via Microsoft Foundry | General high-quality generation; supports model selection such as FLUX.2 Pro or Flex. |
| `foundry-mai25` | MAI-Image-2.5 via Microsoft Foundry | High-quality image generation. |
| `foundry-mai25-flash` | MAI-Image-2.5-Flash via Microsoft Foundry | Choose for faster iteration. |
| `foundry-gpt-image-1p5` | GPT-Image-1.5 via Azure OpenAI | Use when the user's Azure OpenAI deployment provides this model. |
| `foundry-gpt-image-2` | GPT-Image-2 via Azure OpenAI | Compatibility provider for deployments using this model. |
| `foundry-gpt-image-25-sunburst` | GPT-Image-2.5-Sunburst via Azure OpenAI | Use for high-fidelity results when available in the user's region and deployment. |
| `foundry-gpt-image-25-flare` | GPT-Image-2.5-Flare via Azure OpenAI | Use for faster GPT-Image-2.5 iteration when available. |

`foundry-mai2` is retired. Do not select it; use `foundry-mai25` or
`foundry-mai25-flash` instead. Do not assume a preview model is enabled in the
user's Azure region or subscription. Check their deployment configuration and
keep an available provider as a fallback.

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
```

Provider parameters and model availability can change between CLI releases.
Use `t2i providers` and the [CLI guide](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)
as the authoritative sources for the installed version.
