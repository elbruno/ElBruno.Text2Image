# FLUX.2 Setup Guide — Microsoft Foundry

This guide explains how to set up and use the **FLUX.2** text-to-image model via Microsoft Microsoft Foundry with the `Flux2Generator` class.

## Overview

FLUX.2 is a cloud-based text-to-image model available through [Microsoft Microsoft Foundry](https://ai.azure.com). Unlike the local Stable Diffusion models, FLUX.2 runs on Azure infrastructure — no local GPU or ONNX models are needed.

**Available variants:**

| Variant | Best for |
|---------|----------|
| **FLUX.2 Pro** | Photorealistic image generation |
| **FLUX.2 Flex** | Text-heavy design and UI prototyping |

> Reference: [Meet FLUX.2 Flex on Microsoft Foundry](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/meet-flux-2-flex-for-text%E2%80%91heavy-design-and-ui-prototyping-now-available-on-micro/4496041)

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

# Store the endpoint and API key securely (not committed to git)
dotnet user-secrets set FLUX2_ENDPOINT "https://your-resource.services.ai.azure.com/images/generations:submit?api-version=2025-04-01-preview"
dotnet user-secrets set FLUX2_API_KEY "your-api-key-here"
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

using var generator = new Flux2Generator(endpoint, apiKey, modelName: "FLUX.2 Pro");

var result = await generator.GenerateAsync("a futuristic cityscape at sunset, photorealistic");
await result.SaveAsync("flux2-output.png");

Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

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
services.AddFlux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com/...",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Pro");
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
