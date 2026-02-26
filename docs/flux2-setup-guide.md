# FLUX.2 Setup Guide — Microsoft Foundry

This guide explains how to set up and use the **FLUX.2** text-to-image models via Microsoft Foundry with the `Flux2Generator` class.

## Overview

FLUX.2 is a cloud-based text-to-image model family available through [Microsoft Foundry](https://ai.azure.com). Unlike the local Stable Diffusion models, FLUX.2 runs on Azure infrastructure — no local GPU or ONNX models are needed.

**Available variants:**

| Variant | Model ID | Best for | Pricing |
|---------|----------|----------|---------|
| **FLUX.2 Flex** (default) | `FLUX.2-flex` | Text-heavy design, UI prototyping, logos | $0.05 per megapixel |
| **FLUX.2 Pro** | `FLUX.2-pro` | Photorealistic and cinematic-quality images | See [pricing page](https://azure.microsoft.com/en-us/pricing/details/ai-foundry-models/black-forest-labs/) |

### About FLUX.2 Flex

> 📢 **[Meet FLUX.2 Flex for text‑heavy design and UI prototyping — now available on Microsoft Foundry](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/meet-flux-2-flex-for-text%E2%80%91heavy-design-and-ui-prototyping-now-available-on-micro/4496041)**

**FLUX.2 [flex]** is purpose-built for typography and text-forward workflows. Key capabilities:

- **Best-in-class text rendering** — Logos, UI copy, product packaging, and social graphics with readable typography
- **Fine-grained control** — Adjust inference steps and guidance scale for precise production outputs
- **Detail preservation** — Maintains fine details and small elements in complex scenes at any resolution
- **Multi-reference editing** — Supports up to 8 reference images via API for complex multi-image compositing

Use cases: brand design, product UI prototyping, social graphics, marketing collateral, editorial layouts, and any project requiring accurate text rendering.

> 💡 **Tip:** Use FLUX.2 [pro] when your workflow prioritizes photorealistic imagery or cinematic-quality renders rather than text-forward or graphic design outputs.

## Prerequisites

1. An **Azure subscription**
2. An **Microsoft Foundry** resource (formerly Azure OpenAI Service)
3. A **FLUX.2 model deployment** (Pro or Flex)
4. The deployment **endpoint URL** and **API key**

## Step 1: Create an Microsoft Foundry Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → search for **Microsoft Foundry**
3. Select your subscription, resource group, and region
4. Click **Review + create** → **Create**

## Step 2: Deploy a FLUX.2 Model

1. Go to [Microsoft Foundry](https://ai.azure.com)
2. Open your resource → **Model catalog**
3. Search for **FLUX.2** (Pro or Flex)
4. Click **Deploy** and follow the prompts
5. Note the **deployment name** (e.g., `flux-2-pro`)

## Step 3: Get the Endpoint and API Key

1. In Microsoft Foundry, go to your deployment
2. Copy the **Endpoint URL** — it will look like:
   ```
   https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview
   ```
3. Copy the **API key** from the **Keys and Endpoint** section

## Step 4: Configure Credentials

You have three options for providing the endpoint and API key. **User Secrets is recommended for local development** because it keeps secrets out of source control.

### Option A: User Secrets (Recommended)

Navigate to the sample project directory and initialize secrets:

```bash
cd src/samples/scenario-03-flux2-cloud

# Required: endpoint and API key
dotnet user-secrets set FLUX2_ENDPOINT "https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview"
dotnet user-secrets set FLUX2_API_KEY "your-api-key-here"

# Optional: model name and ID (defaults to FLUX.2-flex)
dotnet user-secrets set FLUX2_MODEL_NAME "FLUX.2-flex"
dotnet user-secrets set FLUX2_MODEL_ID "FLUX.2-flex"
```

Secrets are stored in your user profile at:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\elbruno-text2image-flux2\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/elbruno-text2image-flux2/secrets.json`

To verify or list stored secrets:

```bash
dotnet user-secrets list
```

### Option B: Environment Variables

```bash
# Windows
set FLUX2_ENDPOINT=https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview
set FLUX2_API_KEY=your-api-key-here

# Linux / macOS
export FLUX2_ENDPOINT="https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview"
export FLUX2_API_KEY="your-api-key-here"
```

### Option C: appsettings.json (Not recommended — don't commit secrets)

Create `appsettings.json` in the sample project directory:

```json
{
  "FLUX2_ENDPOINT": "https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview",
  "FLUX2_API_KEY": "your-api-key-here"
}
```

> ⚠️ If using this method, ensure `appsettings.json` is in `.gitignore` to avoid leaking credentials.

### Configuration Priority

The sample uses `Microsoft.Extensions.Configuration` and loads settings in this order (last wins):

1. `appsettings.json`
2. Environment variables
3. User Secrets

This means user secrets override environment variables, which override appsettings.json.

## Step 5: Use in C#

### Basic Usage

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

// Build configuration with User Secrets support
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["FLUX2_ENDPOINT"]
    ?? throw new InvalidOperationException("Set FLUX2_ENDPOINT via user secrets or environment variable");
var apiKey = config["FLUX2_API_KEY"]
    ?? throw new InvalidOperationException("Set FLUX2_API_KEY via user secrets or environment variable");

// Model ID selects the variant: "FLUX.2-pro" or "FLUX.2-flex"
// Required for model-based endpoints; optional for deployment-based endpoints (model in URL)
var modelId = config["FLUX2_MODEL_ID"]; // e.g., "FLUX.2-pro"

using var generator = new Flux2Generator(endpoint, apiKey,
    modelName: "FLUX.2 Pro",   // Display name (for logging/UI)
    modelId: modelId);          // API model identifier (sent in request body)

var result = await generator.GenerateAsync("a futuristic cityscape at sunset, photorealistic");
await result.SaveAsync("flux2-output.png");

Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

### Choosing a Model Variant

| Model ID | Display Name | Best for |
|---|---|---|
| `FLUX.2-flex` (default) | FLUX.2 Flex | Text-heavy design, logos, UI prototyping |
| `FLUX.2-pro` | FLUX.2 Pro | Photorealistic image generation |

```csharp
// FLUX.2 Flex — text-heavy design (default)
using var flexPipeline = new Flux2Generator(endpoint, apiKey,
    modelName: "FLUX.2 Flex", modelId: "FLUX.2-flex");

// FLUX.2 Pro — photorealistic
using var proPipeline = new Flux2Generator(endpoint, apiKey,
    modelName: "FLUX.2 Pro", modelId: "FLUX.2-pro");
```

> **Note:** The `modelId` is sent as the `"model"` field in the API request body. If your endpoint
> is deployment-based (the model is embedded in the URL path), you can omit `modelId`.

### With Custom Options

```csharp
var result = await generator.GenerateAsync(
    "a coffee shop logo with the text 'Brew & Code'",
    new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });
```

### Dependency Injection

```csharp
// Deployment-based endpoint (model in URL)
services.AddFlux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com/...",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Pro");

// Model-based endpoint (model in request body)
services.AddFlux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Flex",
    modelId: "FLUX.2-flex");
```

### Using User Secrets in Your Own Project

To add user secrets support to your own project:

```bash
# 1. Add the UserSecretsId to your csproj
dotnet user-secrets init

# 2. Add the configuration packages
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.UserSecrets
dotnet add package Microsoft.Extensions.Configuration.EnvironmentVariables

# 3. Store your secrets
dotnet user-secrets set FLUX2_ENDPOINT "https://..."
dotnet user-secrets set FLUX2_API_KEY "your-key"
```

Then load them in code:

```csharp
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["FLUX2_ENDPOINT"]!;
var apiKey = config["FLUX2_API_KEY"]!;
using var generator = new Flux2Generator(endpoint, apiKey);
```

## API Details

The `Flux2Generator` sends HTTP POST requests to the Microsoft Foundry endpoint:

**Request:**
```json
{
  "prompt": "your text prompt",
  "n": 1,
  "size": "1024x1024",
  "response_format": "b64_json"
}
```

**Response:**
```json
{
  "created": 1234567890,
  "data": [
    {
      "b64_json": "<base64-encoded PNG>"
    }
  ]
}
```

The generator supports both `b64_json` (inline base64) and `url` (download link) response formats.

## Same Interface as Local Models

`Flux2Generator` implements the same `IImageGenerator` interface as `StableDiffusion15` and `LcmDreamshaperV7`. This means you can swap between local and cloud models without changing your application code:

```csharp
// Local model
IImageGenerator generator = new StableDiffusion15();

// Cloud model — same interface
IImageGenerator generator = new Flux2Generator(endpoint, apiKey);

// Both use the same method
var result = await generator.GenerateAsync("a beautiful landscape");
```

## Pricing

FLUX.2 on Microsoft Foundry is a pay-per-use service. Pricing depends on:
- The model variant (Pro vs Flex)
- Image resolution
- Your Azure region

Check the [Microsoft Foundry pricing page](https://azure.microsoft.com/pricing/details/cognitive-services/openai-service/) for current rates.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `401 Unauthorized` | Verify your API key is correct |
| `404 Not Found` | Check the endpoint URL and deployment name |
| `429 Too Many Requests` | You've hit the rate limit — add retry logic or wait |
| Empty response | Ensure `response_format` is set (defaults to `b64_json`) |
| Timeout | Cloud inference can take 10-30 seconds — increase your HttpClient timeout |
