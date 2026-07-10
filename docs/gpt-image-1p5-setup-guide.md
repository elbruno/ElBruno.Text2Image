# GPT-Image-1.5 setup

GPT-Image-1.5 is an Azure OpenAI image-generation model. It is not DALL-E 3: Azure OpenAI retired DALL-E 3 deployments on March 4, 2026. Confirm model access, availability, and deployment requirements in the [official Azure OpenAI image-generation documentation](https://learn.microsoft.com/azure/foundry/openai/how-to/dall-e).

## Configure t2i

Deploy GPT-Image-1.5 in Azure OpenAI, then use the resource's bare endpoint and your deployment name:

```bash
t2i config set foundry-gpt-image-1p5.endpoint "https://your-resource.openai.azure.com"
t2i config set foundry-gpt-image-1p5.model "<deployment-name>"
t2i secrets set foundry-gpt-image-1p5
```

Generate an image:

```bash
t2i "an editorial illustration of a sustainable city" --provider foundry-gpt-image-1p5 --width 1024 --height 1024 --out city.png
```

Use `t2i doctor` to review local configuration and `t2i providers` to list the installed provider IDs. For a one-command credential override, use `--api-key`; prefer the interactive secret store or CI-secret environment variables for routine use.

## Model notes

Microsoft documents GPT-Image-1.5 as a Limited Access Preview image model. Generated images are returned as base64 image data. Azure model availability, supported parameters, safety behavior, pricing, and access restrictions change independently of the CLI, so rely on the official documentation and your deployment's model card for current details.
