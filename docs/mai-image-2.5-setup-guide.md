# MAI-Image-2.5 and MAI-Image-2.5-Flash setup

MAI-Image-2.5 and MAI-Image-2.5-Flash are Preview Microsoft Foundry models. Both support text-to-image generation and image-to-image edits in the Foundry MAI Image API. The `t2i` CLI supports text-to-image generation.

## Deploy a model

1. Create or select a Microsoft Foundry project.
2. Deploy `MAI-Image-2.5` or `MAI-Image-2.5-Flash` as a **Global Standard** deployment.
3. Record the resource endpoint, API key, and your deployment name.

The current documented model version for both 2.5 variants is `2026-06-02`. Availability and Preview terms can change; use the Foundry portal and the [official MAI documentation](https://learn.microsoft.com/azure/foundry/foundry-models/how-to/use-foundry-models-mai) to verify your region and deployment.

## Configure t2i

```bash
# Standard model
t2i config set foundry-mai25.endpoint "https://your-resource.services.ai.azure.com"
t2i config set foundry-mai25.model "<deployment-name>"
t2i secrets set foundry-mai25

# Flash model
t2i config set foundry-mai25-flash.endpoint "https://your-resource.services.ai.azure.com"
t2i config set foundry-mai25-flash.model "<deployment-name>"
t2i secrets set foundry-mai25-flash
```

Generate an image:

```bash
t2i "a product photo of a red fox figurine" --provider foundry-mai25 --width 1024 --height 1024 --out fox.png
```

## API limits

The MAI Image generations endpoint is:

```text
https://<resource-name>.services.ai.azure.com/mai/v1/images/generations
```

It accepts a deployment name in `model`, plus `prompt`, `width`, and `height`. Both dimensions must be at least 768 pixels and their product cannot exceed 1,048,576 pixels. Responses are PNG images encoded as base64.

MAI-Image-2.5 and MAI-Image-2.5-Flash also support image edits at `/mai/v1/images/edits`, but that API is not exposed by `t2i`.
