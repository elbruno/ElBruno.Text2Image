# MAI-Image-2.6 and MAI-Image-2.6-Flash setup

MAI-Image-2.6 and MAI-Image-2.6-Flash are Preview models available through Microsoft Foundry. The `t2i` CLI and `MaiImage26Generator` support text-to-image generation. Preview models have no service-level agreement and aren't recommended for production workloads.

## Deploy a model

1. Create or select a Microsoft Foundry project in a supported region.
2. Deploy `MAI-Image-2.6` or `MAI-Image-2.6-Flash` as a Global Standard deployment. The documented model version is `2026-07-31`.
3. Record the resource endpoint, API key, and the deployment name you assigned. The API's `model` field must contain that deployment name.

Check the Foundry model catalog and [official deployment guide](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/how-to/use-foundry-models-mai-image) for current availability, region, quota, and Preview details.

## Configure t2i

```bash
# Standard model
t2i config set foundry-mai26.endpoint "https://your-resource.services.ai.azure.com"
t2i config set foundry-mai26.model "<deployment-name>"
t2i secrets set foundry-mai26

# Flash model
t2i config set foundry-mai26-flash.endpoint "https://your-resource.services.ai.azure.com"
t2i config set foundry-mai26-flash.model "<deployment-name>"
t2i secrets set foundry-mai26-flash
```

Generate an image. The CLI raises either requested dimension below 768 to 768 and reports the adjustment. The .NET generator also raises undersized dimensions before sending the request.

```bash
t2i "a detailed product photo" --provider foundry-mai26 --width 1536 --height 1024 --out product.png
```

The 2.6-only options are disabled unless supplied:

```bash
t2i "a current city event poster" --provider foundry-mai26 --auto-aspect-ratio --web-grounding --out poster.png
```

`--auto-aspect-ratio` asks the model to select framing based on the prompt. `--web-grounding` opts in to current web context. These options are only accepted with the 2.6 providers.

## .NET API

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

using var httpClient = new HttpClient();
using var generator = new MaiImage26Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    httpClient: httpClient,
    modelName: "MAI-Image-2.6",
    modelId: "your-deployment-name");

var result = await generator.GenerateAsync("a detailed product photo", new ImageGenerationOptions
{
    Width = 1536,
    Height = 1024,
    AutoAspectRatio = true,
    WebGrounding = true
});
await result.SaveAsync("product.png");
```

For Flash, set `modelName` to `MAI-Image-2.6-Flash` and `modelId` to that deployment's name. `AddMaiImage26Generator` and `AddMaiImage26FlashGenerator` are also available for dependency injection.

## API limits and current scope

The generations endpoint is `https://<resource-name>.services.ai.azure.com/mai/v1/images/generations`. It accepts `model`, `prompt`, and optional `width` and `height`, returning PNG bytes in `data[].b64_json`. Both dimensions must be at least 768, and their product must not exceed 2,359,296 pixels. The generator sends the dimension fields and does not send the 2.5-only `size`, `output_format`, or `output_compression` fields.

The current `t2i` and .NET 2.6 generators support text-to-image only. Although Foundry documents image editing for these models, image-input editing has not been added to these 2.6 providers.
