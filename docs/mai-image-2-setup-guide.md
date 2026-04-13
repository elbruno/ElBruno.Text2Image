# MAI-Image-2 Setup Guide — Microsoft Foundry

This guide explains how to set up and use the **MAI-Image-2** text-to-image model via Microsoft Foundry with the `MaiImage2Generator` class.

## Overview

MAI-Image-2 is Microsoft's cloud-based text-to-image model available through [Microsoft Foundry](https://ai.azure.com). Unlike the local Stable Diffusion models, MAI-Image-2 runs on Azure infrastructure — no local GPU or ONNX models are needed.

> 📢 **[Introducing MAI-Image-2](https://microsoft.ai/news/introducing-MAI-Image-2/)** — Microsoft's high-quality image generation model, now available on Microsoft Foundry.

**Key highlights:**

- **High-quality generation** — Produces detailed, high-fidelity images from text prompts
- **Synchronous API** — Returns results directly (no 202 polling needed, unlike FLUX.2)
- **Large prompt support** — Up to 32,000 characters per prompt
- **Flexible dimensions** — Min 768px per dimension, max 1,048,576 total pixels (width × height)

## Prerequisites

1. An **Azure subscription**
2. A **Microsoft Foundry** resource (formerly Azure OpenAI Service)
3. An **MAI-Image-2 model deployment**
4. The deployment **endpoint URL** and **API key**

## Step 1: Create a Microsoft Foundry Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → search for **Microsoft Foundry**
3. Select your subscription, resource group, and region
4. Click **Review + create** → **Create**

## Step 2: Deploy the MAI-Image-2 Model

1. Go to [Microsoft Foundry](https://ai.azure.com)
2. Open your resource → **Model catalog**
3. Search for **MAI-Image-2**
4. Click **Deploy** and follow the prompts
5. Note the **deployment name** (e.g., `MAI-Image-2`)

## Step 3: Get the Endpoint and API Key

1. In Microsoft Foundry, go to your deployment
2. Copy the **Endpoint URL**:
   - **Recommended**: Use the `.services.ai.azure.com` base URL:
     ```
     https://your-resource.services.ai.azure.com
     ```
   - **Also supported**: A `.openai.azure.com` URL — the library auto-converts it:
     ```
     https://your-resource.openai.azure.com
     ```
3. Copy the **API key** from the **Keys and Endpoint** section

> ⚠️ **Important:** MAI-Image-2 uses the `/mai/v1/images/generations` API path.
> The correct endpoint domain is `.services.ai.azure.com`, not `.openai.azure.com`. The library handles this
> conversion automatically, but using `.services.ai.azure.com` directly is recommended.

## Step 4: Configure Credentials

You have three options for providing the endpoint and API key. **User Secrets is recommended for local development** because it keeps secrets out of source control.

### Option A: User Secrets (Recommended)

Navigate to the sample project directory and initialize secrets:

```bash
cd src/samples/scenario-13-mai-image2-cloud

# Required: endpoint and API key
dotnet user-secrets set MAI_IMAGE2_ENDPOINT "https://your-resource.services.ai.azure.com"
dotnet user-secrets set MAI_IMAGE2_API_KEY "your-api-key-here"

# Model configuration (defaults to MAI-Image-2)
dotnet user-secrets set MAI_IMAGE2_MODEL_NAME "MAI-Image-2"
dotnet user-secrets set MAI_IMAGE2_MODEL_ID "MAI-Image-2"
```

> **Note on MAI_IMAGE2_ENDPOINT:** You can provide either:
> - A **`.services.ai.azure.com` base URL** (recommended) — the library auto-builds the API path
> - A **`.openai.azure.com` base URL** — auto-converted to `.services.ai.azure.com`

Secrets are stored in your user profile at:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\elbruno-text2image-mai-image2\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/elbruno-text2image-mai-image2/secrets.json`

To verify or list stored secrets:

```bash
dotnet user-secrets list
```

### Option B: Environment Variables

```bash
# Windows
set MAI_IMAGE2_ENDPOINT=https://your-resource.services.ai.azure.com
set MAI_IMAGE2_API_KEY=your-api-key-here

# Linux / macOS
export MAI_IMAGE2_ENDPOINT="https://your-resource.services.ai.azure.com"
export MAI_IMAGE2_API_KEY="your-api-key-here"
```

### Option C: appsettings.json (Not recommended — don't commit secrets)

Create `appsettings.json` in the sample project directory:

```json
{
  "MAI_IMAGE2_ENDPOINT": "https://your-resource.services.ai.azure.com",
  "MAI_IMAGE2_API_KEY": "your-api-key-here",
  "MAI_IMAGE2_MODEL_ID": "MAI-Image-2"
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

var endpoint = config["MAI_IMAGE2_ENDPOINT"]
    ?? throw new InvalidOperationException("Set MAI_IMAGE2_ENDPOINT via user secrets or environment variable");
var apiKey = config["MAI_IMAGE2_API_KEY"]
    ?? throw new InvalidOperationException("Set MAI_IMAGE2_API_KEY via user secrets or environment variable");

var modelId = config["MAI_IMAGE2_MODEL_ID"] ?? "MAI-Image-2";

using var generator = new MaiImage2Generator(endpoint, apiKey,
    modelName: "MAI-Image-2",       // Display name (for logging/UI)
    modelId: modelId);               // Model/deployment name (sent in request body)

var result = await generator.GenerateAsync("a futuristic cityscape at sunset, photorealistic");
await result.SaveAsync("mai-image2-output.png");

Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

### With Custom Options

```csharp
var result = await generator.GenerateAsync(
    "a serene mountain landscape at golden hour",
    new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });
```

### Dependency Injection

```csharp
services.AddMaiImage2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelName: "MAI-Image-2",
    modelId: "MAI-Image-2");
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
dotnet user-secrets set MAI_IMAGE2_ENDPOINT "https://..."
dotnet user-secrets set MAI_IMAGE2_API_KEY "your-key"
```

Then load them in code:

```csharp
var config = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["MAI_IMAGE2_ENDPOINT"]!;
var apiKey = config["MAI_IMAGE2_API_KEY"]!;
using var generator = new MaiImage2Generator(endpoint, apiKey);
```

## API Details

The `MaiImage2Generator` sends HTTP POST requests to the Microsoft Foundry endpoint at `/mai/v1/images/generations`.

**Authentication:** `api-key` header

**Request:**
```json
{
  "model": "MAI-Image-2",
  "prompt": "your text prompt",
  "width": 1024,
  "height": 1024
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

**Dimension constraints:**
- Minimum: 768px per dimension
- Maximum total pixels: 1,048,576 (width × height)
- Default: 1024×1024

**Prompt limit:** 32,000 characters

> 💡 **Note:** Unlike FLUX.2, MAI-Image-2 uses a **synchronous API** — the response contains the generated image directly, with no 202 polling required.

## Same Interface as Local Models

`MaiImage2Generator` implements the same `IImageGenerator` interface as `StableDiffusion15`, `LcmDreamshaperV7`, and `Flux2Generator`. This means you can swap between local and cloud models without changing your application code:

```csharp
// Local model
IImageGenerator generator = new StableDiffusion15();

// FLUX.2 cloud model
IImageGenerator generator = new Flux2Generator(endpoint, apiKey);

// MAI-Image-2 cloud model — same interface
IImageGenerator generator = new MaiImage2Generator(endpoint, apiKey);

// All use the same method
var result = await generator.GenerateAsync("a beautiful landscape");
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `401 Unauthorized` | Verify your API key is correct |
| `404 Not Found` | Check the endpoint URL and deployment name |
| `429 Too Many Requests` | You've hit the rate limit — add retry logic or wait |
| Empty response | Ensure the prompt is not empty and within the 32,000 character limit |
| `400 Bad Request` — invalid dimensions | Ensure each dimension ≥ 768px and width × height ≤ 1,048,576 |
| Timeout | Cloud inference can take 10-30 seconds — increase your HttpClient timeout |
| Wrong endpoint domain | Use `.services.ai.azure.com` (the library auto-converts `.openai.azure.com`) |
