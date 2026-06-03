# MAI-Image-2.5 Setup Guide — Microsoft Foundry

This guide explains how to set up and use the **MAI-Image-2.5** and **MAI-Image-2.5-Flash**
text-to-image models via Microsoft Foundry with the `MaiImage25Generator` class.

## Overview

MAI-Image-2.5 is Microsoft's latest cloud-based text-to-image model family, available through
[Microsoft Foundry](https://ai.azure.com). Like other cloud models, it runs on Azure
infrastructure — no local GPU or ONNX models are needed.

| Model | Focus | Model ID |
|-------|-------|----------|
| **MAI-Image-2.5** | Highest fidelity & creative detail | `MAI-Image-2.5` |
| **MAI-Image-2.5-Flash** | Speed-optimized / low latency | `MAI-Image-2.5-Flash` |

Both variants are served by the same `MaiImage25Generator` class — select the model with the
`modelId` parameter.

**Key highlights:**

- **OpenAI-compatible API** — uses the `/openai/v1/images/generations` endpoint
- **Synchronous** — returns the generated image directly (no 202 polling)
- **Large prompt support** — up to 32,000 characters per prompt
- **Fixed sizes** — `1024x1024` (square), `1024x1536` (portrait), `1536x1024` (landscape)

## Prerequisites

1. An **Azure subscription**
2. A **Microsoft Foundry** resource
3. A **MAI-Image-2.5** or **MAI-Image-2.5-Flash** model deployment
4. The deployment **endpoint URL** and **API key**

## Step 1: Deploy the Model

1. Go to [Microsoft Foundry](https://ai.azure.com)
2. Open your resource → **Model catalog**
3. Search for **MAI-Image-2.5** or **MAI-Image-2.5-Flash**
4. Click **Deploy** and follow the prompts

## Step 2: Get the Endpoint and API Key

1. In Microsoft Foundry, go to your deployment
2. Copy the **Endpoint URL** — the `.services.ai.azure.com` base URL is recommended:
   ```
   https://your-resource.services.ai.azure.com
   ```
   A `.openai.azure.com` URL is also supported (the library auto-converts it).
3. Copy the **API key** from the **Keys and Endpoint** section

> ⚠️ **Important:** MAI-Image-2.5 uses the `/openai/v1/images/generations` API path on the
> `.services.ai.azure.com` domain. The library builds this path automatically — supply either
> a base URL or a full URL.

## Step 3: Configure Credentials

### Option A: User Secrets (Recommended)

```bash
cd src/samples/scenario-18-mai-image25-cloud

dotnet user-secrets set MAI_IMAGE25_ENDPOINT "https://your-resource.services.ai.azure.com"
dotnet user-secrets set MAI_IMAGE25_API_KEY "your-api-key-here"
dotnet user-secrets set MAI_IMAGE25_MODEL_ID "MAI-Image-2.5"   # or MAI-Image-2.5-Flash
```

### Option B: Environment Variables

```bash
# Windows
set MAI_IMAGE25_ENDPOINT=https://your-resource.services.ai.azure.com
set MAI_IMAGE25_API_KEY=your-api-key-here

# Linux / macOS
export MAI_IMAGE25_ENDPOINT="https://your-resource.services.ai.azure.com"
export MAI_IMAGE25_API_KEY="your-api-key-here"
```

## Step 4: Use in C#

### Basic Usage

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

using var generator = new MaiImage25Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    httpClient: new HttpClient(),
    modelName: "MAI-Image-2.5",   // Display name (for logging/UI)
    modelId: "MAI-Image-2.5");     // Model name (sent in request body)

var result = await generator.GenerateAsync("A photograph of a red fox in an autumn forest");
await result.SaveAsync("mai-image25-output.png");
```

### Speed-optimized Flash variant

```csharp
using var generator = new MaiImage25Generator(
    endpoint, apiKey, new HttpClient(),
    modelName: "MAI-Image-2.5-Flash",
    modelId: "MAI-Image-2.5-Flash");
```

### Dependency Injection

```csharp
// Standard model
services.AddMaiImage25Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key");

// Flash variant
services.AddMaiImage25FlashGenerator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key");
```

### Via CLI (t2i)

```bash
# MAI-Image-2.5
t2i --provider foundry-mai25 "a serene mountain landscape at sunrise"

# MAI-Image-2.5-Flash (speed-optimized)
t2i --provider foundry-mai25-flash "a quick concept sketch of a city"

# Override the model name for a provider
t2i config set foundry-mai25.model MAI-Image-2.5
```

## API Details

The `MaiImage25Generator` sends HTTP POST requests to `/openai/v1/images/generations`.

**Authentication:** `api-key` and `Authorization: Bearer` headers

**Request:**
```json
{
  "model": "MAI-Image-2.5",
  "prompt": "your text prompt",
  "size": "1024x1024",
  "n": 1,
  "output_format": "png",
  "output_compression": 100
}
```

**Response:**
```json
{
  "data": [
    {
      "b64_json": "<base64-encoded PNG>"
    }
  ]
}
```

**Supported sizes:** `1024x1024`, `1024x1536`, `1536x1024` (default `1024x1024`).
Requested width/height are mapped to the nearest supported size by aspect ratio.

**Prompt limit:** 32,000 characters

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `401 Unauthorized` | Verify your API key is correct |
| `404 Not Found` | Check the endpoint URL and that the model is deployed |
| `429 Too Many Requests` | You've hit the rate limit — add retry logic or wait |
| Empty response | Ensure the prompt is not empty and within the 32,000 character limit |
| Timeout | Cloud inference can take several seconds — increase your HttpClient timeout |
| Wrong endpoint domain | Use `.services.ai.azure.com` (the library auto-converts `.openai.azure.com`) |
