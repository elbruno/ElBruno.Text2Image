# Scenario 15: GPT-Image-1.5 Cloud Sample

Demonstrates text-to-image generation using **GPT-Image-1.5** via Azure OpenAI Service.

## What It Does

This sample generates **3 different images** showcasing GPT-Image-1.5 capabilities:

1. **Scenario A**: Basic generation at 1024×1024 — serene landscape
2. **Scenario B**: Landscape variation at 1792×1024 (wide) — modern city skyline
3. **Scenario C**: Abstract art style at 1024×1024 — geometric patterns with vibrant colors

All generated images are saved to the `output/` folder as PNG files.

## Requirements

- **.NET 8.0** or **.NET 10.0**
- Azure OpenAI resource with **gpt-image-1.5 deployment**
- Valid API key and endpoint URL

## Setup Instructions

### Step 1: Create Azure OpenAI Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Create or navigate to your **Azure OpenAI** resource
3. In **Deployments**, create a new deployment:
   - Model: `gpt-image-1.5`
   - Name: `gpt-image-1p5` (or your preferred name)
4. From **Keys + Endpoint**, copy:
   - **Endpoint URL** (e.g., `https://my-resource.openai.azure.com/`)
   - **API Key** (from Key 1 or Key 2)

### Step 2: Configure Credentials

#### Option A: User Secrets (Recommended for Development)

```bash
cd src/samples/scenario-15-gpt-image-1p5-cloud
dotnet user-secrets init
dotnet user-secrets set GptImage1p5:Endpoint "https://your-resource.openai.azure.com/"
dotnet user-secrets set GptImage1p5:ApiKey "your-api-key-here"
dotnet user-secrets set GptImage1p5:Model "gpt-image-1p5"
```

#### Option B: Environment Variables

```bash
set GptImage1p5__Endpoint=https://your-resource.openai.azure.com/
set GptImage1p5__ApiKey=your-api-key-here
set GptImage1p5__Model=gpt-image-1p5
```

#### Option C: appsettings.json

Edit `appsettings.json` and replace placeholders:

```json
{
  "GptImage1p5": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "Model": "gpt-image-1p5"
  }
}
```

### Step 3: Run the Sample

```bash
dotnet run
```

### Expected Output

```
=== ElBruno.Text2Image - GPT-Image-1.5 Cloud API Demo (Azure OpenAI) ===

Model: GPT-Image-1.5
Model ID: gpt-image-1p5
Endpoint: https://your-resource.openai.azure.com/
GPT-Image-1.5 cloud model ready

--- Scenario A: Basic Generation (1024x1024) ---
...
✓ Image saved to: C:\path\to\output\scenario-a-1024x1024.png
  Inference time: 8234ms

--- Scenario B: Landscape Variation (1792x1024) ---
...
✓ Image saved to: C:\path\to\output\scenario-b-1792x1024.png
  Inference time: 8456ms

--- Scenario C: Abstract Art Style (1024x1024) ---
...
✓ Image saved to: C:\path\to\output\scenario-c-abstract.png
  Inference time: 8122ms

=== Generation Complete ===
All images saved to: C:\path\to\output
```

## Size Constraints

GPT-Image-1.5 supports **only three fixed aspect ratios**:

| Size | Aspect Ratio |
|------|--------------|
| 1024×1024 | 1:1 (Square) |
| 1792×1024 | 16:9 (Landscape) |
| 1024×1792 | 9:16 (Portrait) |

Requests with other dimensions will be automatically mapped to the nearest supported size by the generator.

## Prompt Guidelines

- **Length**: Up to ~4000 characters
- **Quality**: More detailed prompts produce better results
- **Style keywords**: Include style descriptors (e.g., "digital art", "photorealistic", "oil painting")
- **Constraints**: Avoid requests that violate Azure OpenAI content policy

## Performance & Cost

### Generation Time
- **Typical latency**: 8-12 seconds per image
- **Timeout**: Default 5 minutes (can be customized via `HttpClient`)

### Pricing
- **Current rate**: ~$0.15 per image (1024×1024)
- **Landscape/Portrait**: Same cost as square
- Check [Azure OpenAI pricing](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/) for latest rates

### Cost Example
- 3 images in this sample: ~$0.45
- 100 images/month: ~$15

## Troubleshooting

### "ERROR: GptImage1p5:Endpoint and GptImage1p5:ApiKey are not configured"

**Solution**: Configure credentials using one of the methods in Step 2 above.

### "Unauthorized" or "401" errors

**Causes**:
- Invalid API key
- Endpoint URL is incorrect
- API key has expired or been revoked

**Solution**: Re-copy the API key and endpoint from Azure Portal.

### "Not Found" or "404" errors

**Causes**:
- Deployment name does not exist
- Wrong resource or region

**Solution**: Verify the deployment name matches your Azure OpenAI deployment.

### Generation takes longer than expected

- First request may take 10-15 seconds (model warm-up)
- Subsequent requests typically complete in 8-10 seconds
- Large batches may experience queueing

## Project Structure

```
scenario-15-gpt-image-1p5-cloud/
├── Program.cs                    # Main application logic
├── appsettings.json              # Configuration template
├── scenario-15-gpt-image-1p5-cloud.csproj  # Project file
├── README.md                     # This file
└── output/                       # Generated images (created at runtime)
    ├── scenario-a-1024x1024.png
    ├── scenario-b-1792x1024.png
    └── scenario-c-abstract.png
```

## References

- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Azure.AI.OpenAI SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI)
- [ElBruno.Text2Image on NuGet](https://www.nuget.org/packages/ElBruno.Text2Image.Foundry/)
- [ElBruno.Text2Image GitHub](https://github.com/elbruno/ElBruno.Text2Image)

## Notes

- This sample is **educational** — not production code
- Generated images are saved locally; no remote storage
- API key should never be committed to source control
- Use `dotnet user-secrets` for secure local development
