# GPT-Image-1.5 Setup Guide — Azure OpenAI DALL-E 3

This guide explains how to set up and use **GPT-Image-1.5** (DALL-E 3) for text-to-image generation via Azure OpenAI Service with the `GptImage1p5Generator` class.

## What is GPT-Image-1.5?

**GPT-Image-1.5** is Azure OpenAI's implementation of OpenAI's DALL-E 3 model, providing enterprise-grade image generation with:

- **Advanced understanding** — Detailed instruction following and high-quality image synthesis
- **Artistic control** — Fine-tuned prompts produce consistent, professional results
- **Azure integration** — Managed through Azure OpenAI Service with billing and usage tracking
- **Flexible sizing** — Three fixed sizes: 1024×1024, 1792×1024, 1024×1792

**Comparison with other providers:**

| Feature | GPT-Image-1.5 | FLUX.2 Pro | MAI-Image-2 |
|---------|---------------|-----------|------------|
| Provider | Azure OpenAI | Microsoft Foundry (BFL) | Microsoft Foundry |
| Model | DALL-E 3 | Black Forest Labs FLUX | Microsoft AI Image 2 |
| Best For | General-purpose, detailed prompts | Photorealistic, cinematic | High-quality artistic |
| Available Sizes | 3 fixed | 4 flexible | 3 fixed |
| Base Cost | ~$0.15 per image | ~$0.025 per image | $0.06 per image |
| Setup Complexity | Medium | Low | Medium |

---

## Prerequisites

1. **Azure subscription** — Active Azure account with billing enabled
2. **Azure OpenAI resource** — GPT-Image-1.5 deployment configured
3. **Credentials** — Endpoint URL and API key
4. **Deployment name** — The name of your gpt-image-1.5 deployment

## Step 1: Create an Azure OpenAI Resource

### Using Azure Portal

1. Go to **[Azure Portal](https://portal.azure.com)**
2. Click **+ Create a resource**
3. Search for **Azure OpenAI** and click **Create**
4. Fill in details:
   - **Subscription** — Select your subscription
   - **Resource group** — Create new or select existing
   - **Region** — Choose a region where GPT-Image-1.5 is available (e.g., `East US`, `Sweden Central`)
   - **Name** — e.g., `my-gpt-image-resource`
   - **Pricing tier** — Standard (S0)
5. Review and click **Create** — wait for deployment (5-10 minutes)

### Using Azure CLI

```bash
az cognitiveservices account create \
  --name my-gpt-image-resource \
  --resource-group my-resource-group \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

---

## Step 2: Deploy GPT-Image-1.5 Model

### Using Azure OpenAI Studio

1. Go to **[Azure OpenAI Studio](https://oai.azure.com)**
2. Select your resource
3. Click **Deployments** in the left sidebar
4. Click **+ Create new deployment**
5. Select **GPT-Image-1.5** from model list
6. Enter deployment name: `gpt-image-15` (or your choice)
7. Select version (default recommended)
8. Click **Create** — deployment initializes

### Using Azure CLI

```bash
az cognitiveservices account deployment create \
  --resource-group my-resource-group \
  --name my-gpt-image-resource \
  --deployment-name gpt-image-15 \
  --model-name gpt-image-1.5 \
  --model-version 2024-06-01
```

---

## Step 3: Get Endpoint and API Key

### Find Credentials in Portal

1. In **[Azure Portal](https://portal.azure.com)**, go to your Azure OpenAI resource
2. Click **Keys and Endpoint** in the left sidebar
3. Copy:
   - **Endpoint** — e.g., `https://my-gpt-image-resource.openai.azure.com/`
   - **Key 1** or **Key 2** — Either key works; keep it secret

### Using Azure CLI

```bash
az cognitiveservices account keys list \
  --name my-gpt-image-resource \
  --resource-group my-resource-group

az cognitiveservices account show \
  --name my-gpt-image-resource \
  --resource-group my-resource-group \
  --query properties.endpoint
```

---

## Step 4: Configure CLI Credentials

### Option A: Environment Variables (Quickstart)

Set environment variables before running `t2i`:

**Windows (Command Prompt):**
```cmd
set GPT_IMAGE_1P5_ENDPOINT=https://my-gpt-image-resource.openai.azure.com/
set GPT_IMAGE_1P5_API_KEY=your-api-key-here
set GPT_IMAGE_1P5_MODEL=gpt-image-15
```

**Windows (PowerShell):**
```powershell
$env:GPT_IMAGE_1P5_ENDPOINT = "https://my-gpt-image-resource.openai.azure.com/"
$env:GPT_IMAGE_1P5_API_KEY = "your-api-key-here"
$env:GPT_IMAGE_1P5_MODEL = "gpt-image-15"
```

**Linux/macOS:**
```bash
export GPT_IMAGE_1P5_ENDPOINT="https://my-gpt-image-resource.openai.azure.com/"
export GPT_IMAGE_1P5_API_KEY="your-api-key-here"
export GPT_IMAGE_1P5_MODEL="gpt-image-15"
```

### Option B: User Secrets (Recommended for Development)

Navigate to your sample project and configure secrets:

```bash
cd src/samples/scenario-15-gpt-image-1p5-cloud

# Set credentials
dotnet user-secrets set GPT_IMAGE_1P5_ENDPOINT "https://my-gpt-image-resource.openai.azure.com/"
dotnet user-secrets set GPT_IMAGE_1P5_API_KEY "your-api-key-here"
dotnet user-secrets set GPT_IMAGE_1P5_MODEL "gpt-image-15"

# List stored secrets
dotnet user-secrets list
```

Secrets are stored securely in:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\elbruno-text2image-gpt1p5\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/elbruno-text2image-gpt1p5/secrets.json`

### Option C: appsettings.json (Not Recommended)

Create `appsettings.json` in your project:

```json
{
  "GPT_IMAGE_1P5_ENDPOINT": "https://my-gpt-image-resource.openai.azure.com/",
  "GPT_IMAGE_1P5_API_KEY": "your-api-key-here",
  "GPT_IMAGE_1P5_MODEL": "gpt-image-15"
}
```

> ⚠️ **Warning:** Never commit `appsettings.json` with real credentials. Add to `.gitignore`:
> ```
> appsettings.json
> ```

---

## Step 5: Verify Setup

### Health Check

```bash
t2i health
```

Expected output:
```
✅ GPT-Image-1.5: Endpoint responding
   Deployment: gpt-image-15
   Region: eastus
```

### Generate First Image

```bash
t2i --provider foundry-gpt-image-1p5 "a serene mountain landscape"
```

---

## Generate Your First Image

### Using the CLI

```bash
# Simple generation
t2i "a robot painting a landscape"

# With specific provider and size
t2i --provider foundry-gpt-image-1p5 \
    --width 1792 \
    --height 1024 \
    "a futuristic cityscape at sunset, cyberpunk style"

# With output file
t2i "a beautiful sunset over the ocean" -o my-image.png
```

### Using the C# Library

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

// Create generator
using var generator = new GptImage1p5Generator(
    endpoint: "https://my-gpt-image-resource.openai.azure.com/",
    apiKey: "your-api-key-here",
    deploymentName: "gpt-image-15");

// Generate image
var result = await generator.GenerateAsync(
    "a serene mountain landscape, oil painting style");

// Save to file
await result.SaveAsync("landscape.png");
Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

### With Custom Size

```csharp
using var generator = new GptImage1p5Generator(endpoint, apiKey, "gpt-image-15");

var options = new ImageGenerationOptions
{
    Width = 1792,
    Height = 1024
};

var result = await generator.GenerateAsync(
    "a wide panoramic landscape at sunset", options);

await result.SaveAsync("panorama.png");
```

---

## Size Constraints

⚠️ **Important:** GPT-Image-1.5 supports **only 3 fixed sizes**. Requests are automatically mapped to the nearest supported size.

### Supported Sizes

| Size | Aspect Ratio | Best For |
|------|--------------|----------|
| **1024×1024** | 1:1 (Square) | Default, general purpose |
| **1792×1024** | 16:9 (Landscape) | Panoramas, wide scenes |
| **1024×1792** | 9:16 (Portrait) | Tall compositions, character art |

### Automatic Size Mapping

When you request an unsupported size, the generator automatically maps to the nearest supported option:

```
Request 512×512    → Mapped to 1024×1024 (smallest)
Request 1024×768   → Mapped to 1024×1024 (closest match)
Request 1600×1200  → Mapped to 1792×1024 (landscape)
Request 800×2000   → Mapped to 1024×1792 (portrait)
Request 2000×2000  → Mapped to 1792×1024 (largest)
```

### Example: Requesting Different Sizes

```csharp
// Square (1:1) — default for general images
var square = await generator.GenerateAsync("a cat sitting");
// Result: 1024×1024

// Landscape (16:9) — wide panoramas
var landscape = await generator.GenerateAsync(
    "mountains and valley",
    new ImageGenerationOptions { Width = 1792, Height = 1024 });
// Result: 1792×1024

// Portrait (9:16) — character art, tall compositions
var portrait = await generator.GenerateAsync(
    "an astronaut in space",
    new ImageGenerationOptions { Width = 1024, Height = 1792 });
// Result: 1024×1792
```

---

## Pricing & Cost Tracking

### Cost Per Image

As of January 2025:

| Size | Cost |
|------|------|
| **1024×1024** | $0.08 |
| **1792×1024** | $0.12 |
| **1024×1792** | $0.12 |

> Pricing from [Azure OpenAI Pricing](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/)

### Example: Monthly Budget

For 30 images at 1024×1024:
```
30 images × $0.08 = $2.40 per month
```

For 10 landscape + 10 portrait + 10 square:
```
(10 × $0.12) + (10 × $0.12) + (10 × $0.08) = $3.20 per month
```

### Monitor Usage in Azure Portal

1. Go to **[Azure Portal](https://portal.azure.com)**
2. Open your Azure OpenAI resource
3. Click **Metrics** → View usage graphs
4. Set timespan and granularity:
   - **Timespan:** Last month
   - **Metric:** Tokens used
   - **Granularity:** Daily

### Track via CLI

```bash
# List recent generation calls (if logging enabled)
t2i logs --limit 20

# Show usage summary
t2i stats
```

---

## Troubleshooting

### "Authentication failed (401)"

**Cause:** Invalid API key or endpoint

**Fix:**
1. Verify API key in Azure Portal → Keys and Endpoint
2. Check credentials are set correctly:
   ```bash
   t2i config show
   ```
3. Ensure key hasn't expired (rotate if needed)
4. Verify credentials are not accidentally truncated

---

### "Endpoint not found (404)"

**Cause:** Incorrect endpoint URL or deployment name

**Fix:**
1. Verify endpoint URL ends with `/`:
   ```
   ✅ https://my-resource.openai.azure.com/
   ❌ https://my-resource.openai.azure.com
   ```
2. Check deployment name exists in Azure Portal
3. Ensure resource region has GPT-Image-1.5 available
4. Try endpoint without trailing slash if above fails

---

### "Request timeout (>30s)"

**Cause:** Slow network, high server load, or large image request

**Fix:**
1. **Increase timeout** in code:
   ```csharp
   var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
   var result = await generator.GenerateAsync(prompt, 
       cancellationToken: cts.Token);
   ```
2. **Reduce complexity** of prompt
3. **Try again later** if server is under load
4. **Check network connection** — try from different network

---

### "Quota exceeded"

**Cause:** Hit rate limit or monthly quota

**Fix:**
1. View quota in Azure Portal:
   - Resource → Quotas and limits
   - Current usage shown on dashboard
2. **Wait for quota reset** (typically monthly)
3. **Request quota increase**:
   - Resource → Quotas
   - Click "Request quota increase"
   - Select new limit and submit
4. **Review pricing** — consider reducing generation frequency

---

### "Deployment not found (400)"

**Cause:** Deployment name doesn't match Azure configuration

**Fix:**
1. Verify deployment name in Azure Portal:
   - Go to Deployments section
   - Copy exact deployment name
2. Update configuration:
   ```bash
   t2i config set foundry-gpt-image-1p5.model your-deployment-name
   ```
3. Case is sensitive — match exactly

---

### "No available regions have GPT-Image-1.5"

**Cause:** Resource region doesn't support the model

**Fix:**
1. Create resource in a supported region:
   - ✅ East US
   - ✅ Sweden Central
   - ✅ Japan East
   - Check [Model Availability](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models) for current regions
2. If using existing resource in unsupported region:
   - Create new resource in supported region
   - Copy endpoint and re-deploy model

---

### "Rate limited (429)"

**Cause:** Too many requests in short time window

**Fix:**
1. **Implement backoff**:
   ```csharp
   int retries = 3;
   while (retries > 0)
   {
       try
       {
           return await generator.GenerateAsync(prompt);
       }
       catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
       {
           retries--;
           await Task.Delay(5000); // Wait 5 seconds
       }
   }
   ```
2. **Space out requests** — add delays between calls
3. **Request quota increase** for higher rate limits

---

### "Invalid prompt format"

**Cause:** Prompt contains unsupported characters or exceeds length limit

**Fix:**
1. **Max prompt length:** 4000 characters
2. **Avoid:**
   - Null bytes
   - Very long repeated characters
   - Incompatible encoding
3. **Example valid prompt:**
   ```
   ✅ "a serene mountain landscape at sunset, oil painting style"
   ❌ "generate an image" (too vague for DALL-E)
   ❌ [prompt exceeding 4000 chars]
   ```

---

## Managed Identity Support (Future)

Future versions will support **Managed Identity** authentication (no need to store API keys):

```csharp
// Coming in Phase 2
using var credential = new DefaultAzureCredential();
using var generator = new GptImage1p5Generator(
    endpoint: "https://my-resource.openai.azure.com/",
    credential: credential,
    deploymentName: "gpt-image-15");
```

This will enable:
- **Secure authentication** without storing secrets
- **RBAC integration** with Azure Active Directory
- **Audit logging** of who generated images
- **CI/CD automation** without secret management

---

## Additional Resources

- **[Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)**
- **[DALL-E 3 Guide](https://platform.openai.com/docs/guides/vision)**
- **[Azure OpenAI Pricing](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/)**
- **[Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net)**

---

## Next Steps

- 📖 See [docs/architecture.md](architecture.md) for library design
- 🔧 Check [docs/cli-tool.md](cli-tool.md) for CLI reference
- 🤖 Explore [src/samples/scenario-15-gpt-image-1p5-cloud/](../src/samples/scenario-15-gpt-image-1p5-cloud/) for examples
