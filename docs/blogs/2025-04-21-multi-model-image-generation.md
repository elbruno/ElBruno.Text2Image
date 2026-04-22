# 🎨 Four Models, One API: Unified Multi-Model Image Generation in ElBruno.Text2Image

> **Now available:** GPT-Image-1.5, GPT-Image-2, FLUX.2, and MAI-Image-2 — all accessible through the same clean .NET interface. No more choosing between cloud and local. Use them all.

## The Hook

Text-to-image generation in .NET just leveled up. Today we're announcing unified support for **four production-grade AI models** across three cloud providers — all with the same API surface, the same developer experience, and the same promise: *generate beautiful images from text without leaving C#*.

Whether you're crafting marketing collateral, automating design workflows, or building the next generation of AI-powered applications, **ElBruno.Text2Image** now gives you the flexibility to choose the right model for the job:

- **GPT-Image-1.5** (Azure OpenAI) — Versatile, widely available
- **GPT-Image-2** (Azure OpenAI) — Enhanced quality and detail
- **FLUX.2 Pro & Flex** (Microsoft Foundry) — State-of-the-art photorealistic images + text-perfect designs
- **MAI-Image-2** (Microsoft Foundry) — High-quality, efficient generation

Pick one. Pick all. The choice is yours.

---

## Overview: Meeting the Models

### The Players

| Model | Provider | Introduced | Specialization |
|-------|----------|------------|-----------------|
| **GPT-Image-1.5** | Azure OpenAI | 2024 | Fast, reliable image generation. DALL-E 3 under the hood. Great for general use cases. |
| **GPT-Image-2** | Azure OpenAI | 2025 | Enhanced quality. Better detail capture. Faster inference. Next-gen DALL-E. |
| **FLUX.2 Pro** | Microsoft Foundry | 2024 | Photorealistic masterpieces. Cinematic quality. For when "good" isn't enough. |
| **FLUX.2 Flex** | Microsoft Foundry | 2024 | Text-perfect designs. Logos. UI copy. Product packaging. Where typography matters. |
| **MAI-Image-2** | Microsoft Foundry | 2024 | Balanced quality and speed. Excellent for production workflows. No queuing. Synchronous API. |

### Why Four Models?

**Redundancy + Specialization = Resilience.**

If one provider experiences downtime, switch to another. If you need photorealistic art, use FLUX.2. If you're rendering diagrams with text, use FLUX.2 Flex. If you want the fastest generation, use MAI-Image-2. And if you need wide availability at scale, GPT-Image-2 has you covered.

ElBruno.Text2Image treats them all equally. No more vendor lock-in. No more choosing between quality and flexibility. Just pick the right tool for the job.

---

## Model Comparison Table

A quick reference for developers:

| Feature | GPT-Image-1.5 | GPT-Image-2 | FLUX.2 Pro | FLUX.2 Flex | MAI-Image-2 |
|---------|---|---|---|---|---|
| **Provider** | Azure OpenAI | Azure OpenAI | Microsoft Foundry | Microsoft Foundry | Microsoft Foundry |
| **Quality** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Text Rendering** | Good | Good | Excellent | **Perfect** | Good |
| **Photorealism** | Good | **Excellent** | **Excellent** | Good | **Excellent** |
| **Resolution** | 1024×1024, 1792×1024 | 1024×1024, 1792×1024 | 1024×1024 | 1024×1024 | 768–1024px |
| **Speed** | ~5-10s | ~5-10s | ~10-15s | ~10-15s | ~3-5s |
| **Async Pattern** | Synchronous | Synchronous | 202 + poll | 202 + poll | Synchronous |
| **API Type** | OpenAI REST | OpenAI REST | BFL Native | BFL Native | Azure REST |
| **Best For** | General use, high availability | Premium quality | Photorealism, art | Logos, UI, design | Production workflows |
| **Cost Model** | Per call | Per call | Per megapixel | Per megapixel | Per call |
| **Availability** | Broad | Broad | Limited regions | Limited regions | Growing |

---

## Library Usage — C# Code Samples

### Installation

Choose **one** package matching your provider:

```bash
# For Azure OpenAI (GPT-Image-1.5 and GPT-Image-2)
dotnet add package ElBruno.Text2Image.Foundry

# For Microsoft Foundry (FLUX.2 and MAI-Image-2)
dotnet add package ElBruno.Text2Image.Foundry

# For local models (optional—add AFTER cloud packages if needed)
dotnet add package ElBruno.Text2Image.Cpu
```

> **Note:** `ElBruno.Text2Image.Foundry` covers both Azure OpenAI and Microsoft Foundry cloud providers.

### Sample 1: GPT-Image-1.5 (Versatile DALL-E 3)

**Use Case:** Marketing visuals, blog headers, general-purpose content.

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

// Load credentials from appsettings.json, user secrets, or environment variables
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["GptImage1p5:Endpoint"];
var apiKey = config["GptImage1p5:ApiKey"];

// Create the generator
using var generator = new GptImage1p5Generator(
    endpoint: endpoint,
    apiKey: apiKey,
    deploymentName: "gpt-image-1p5");

// Generate a marketing hero image
var result = await generator.GenerateAsync(
    "a modern software developer working at a standing desk with a mountain view, professional photography, warm lighting",
    new ImageGenerationOptions
    {
        Width = 1792,
        Height = 1024  // Landscape format for hero banners
    });

await result.SaveAsync("hero-banner.png");
Console.WriteLine($"Generated in {result.InferenceTimeMs}ms");
```

**Setup (one-time):**
```bash
# Option A: User Secrets (recommended for development)
dotnet user-secrets set GptImage1p5:Endpoint "https://your-resource.openai.azure.com"
dotnet user-secrets set GptImage1p5:ApiKey "your-api-key-here"
dotnet user-secrets set GptImage1p5:Model "gpt-image-1p5"

# Option B: Environment Variables
$env:GptImage1p5__Endpoint = "https://your-resource.openai.azure.com"
$env:GptImage1p5__ApiKey = "your-api-key-here"

# Option C: appsettings.json
# { "GptImage1p5": { "Endpoint": "...", "ApiKey": "...", "Model": "gpt-image-1p5" } }
```

---

### Sample 2: GPT-Image-2 (Premium Quality)

**Use Case:** Product photography, concept art, high-stakes visuals.

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

// Create the generator (credentials from config as above)
using var generator = new GptImage2Generator(
    endpoint: "https://your-resource.openai.azure.com",
    apiKey: "your-api-key",
    deploymentName: "gpt-image-2");

// Generate a product showcase
var result = await generator.GenerateAsync(
    "a sleek stainless steel coffee maker on a marble countertop, morning sunlight streaming through a window, professional product photography",
    new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

await result.SaveAsync("product-showcase.png");
```

---

### Sample 3: FLUX.2 Pro (Photorealistic Excellence)

**Use Case:** Art direction, cinematic visuals, high-fidelity renders.

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

// Create the generator
using var generator = new Flux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Pro",
    modelId: "FLUX.2-pro");

// Generate a concept art scene
var result = await generator.GenerateAsync(
    "a bioluminescent alien forest at night, towering crystal structures, deep blues and purples, volumetric light rays, cinematic composition",
    new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

await result.SaveAsync("concept-art.png");
Console.WriteLine($"Generated by FLUX.2 Pro in {result.InferenceTimeMs}ms");
```

---

### Sample 4: FLUX.2 Flex (Text-Perfect Design)

**Use Case:** Logos, UI mockups, product packaging, text-centric designs.

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

// Create the Flex generator (same interface, different model)
using var generator = new Flux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelName: "FLUX.2 Flex",
    modelId: "FLUX.2-flex");  // Note: different model ID

// Generate a product label with perfect text rendering
var result = await generator.GenerateAsync(
    "a professional product label for 'CloudSync Pro' cloud storage software, white background, modern sans-serif font, clear readable text, tech company branding, mint green accent color",
    new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });

await result.SaveAsync("product-label.png");
```

> 💡 **Pro Tip:** FLUX.2 Flex is ideal for any design where text needs to be perfectly readable. Logos, certificates, UI designs, packaging — anything where typography matters, use Flex.

---

### Sample 5: MAI-Image-2 (Fast & Balanced)

**Use Case:** Batch generation, production workflows, real-time applications.

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

// Create the generator
using var generator = new MaiImage2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelName: "MAI-Image-2",
    modelId: "MAI-Image-2");

// Generate multiple illustrations for a blog series
var prompts = new[]
{
    "a watercolor painting of a cat reading a book, cozy library, warm lighting",
    "a watercolor painting of a dog playing fetch in a meadow, sunny day, vibrant colors",
    "a watercolor painting of a bird perched on a branch, morning fog, serene mood"
};

var outputDir = "blog-illustrations";
Directory.CreateDirectory(outputDir);

foreach (var (prompt, index) in prompts.Select((p, i) => (p, i)))
{
    Console.WriteLine($"Generating {index + 1}/{prompts.Length}: {prompt}");
    
    var result = await generator.GenerateAsync(prompt, 
        new ImageGenerationOptions { Width = 1024, Height = 1024 });
    
    var outputPath = Path.Combine(outputDir, $"illustration-{index + 1}.png");
    await result.SaveAsync(outputPath);
}

Console.WriteLine($"Batch complete. Images saved to: {Path.GetFullPath(outputDir)}");
```

---

### Sample 6: Dependency Injection (The Enterprise Way)

**Use Case:** ASP.NET Core apps, cloud services, DI-first architectures.

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

// Configure your service container
var services = new ServiceCollection();

// Add FLUX.2 Pro as the default IImageGenerator
services.AddFlux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key",
    modelId: "FLUX.2-pro");

var provider = services.BuildServiceProvider();

// Inject IImageGenerator anywhere
var imageGenerator = provider.GetRequiredService<IImageGenerator>();

// Use it without knowing the implementation details
var request = new ImageGenerationRequest("a serene mountain landscape at sunrise");
var options = new ImageGenerationOptions { ImageSize = new System.Drawing.Size(1024, 1024) };

var response = await imageGenerator.GenerateAsync(request, options);
var imageBytes = response.Contents.OfType<DataContent>().First().Data.ToArray();

await File.WriteAllBytesAsync("mountain.png", imageBytes);
```

---

## CLI Usage — Practical Examples

The `t2i` command-line tool wraps the same library for shell access. Perfect for CI/CD, scripts, and rapid prototyping.

### Setup

```bash
# Install the CLI globally
dotnet tool install --global ElBruno.Text2Image.Cli

# Run the setup wizard (interactive)
t2i config
```

The wizard guides you through:
1. Detecting available local providers (CPU, CUDA, DirectML)
2. Configuring cloud credentials (Azure OpenAI, Microsoft Foundry)
3. Setting your default provider
4. Testing connectivity

### Example 1: Generate a Logo (FLUX.2 Flex)

```bash
t2i "a minimalist tech company logo, two overlapping circles forming a sync symbol, blue and teal gradient, white background, clean modern design" \
  --provider foundry-flux2 \
  --width 512 \
  --height 512 \
  --out logo.png
```

**Output:** `logo.png` (512×512, perfect text rendering, professional)

---

### Example 2: Batch Generate Blog Illustrations (MAI-Image-2)

```bash
# Create a script: generate-illustrations.sh

#!/bin/bash
prompts=(
  "a watercolor painting of a developer fixing bugs, laptop screen glowing"
  "a watercolor painting of a cloud server center, neon lights"
  "a watercolor painting of an AI assistant helping a user, warm colors"
)

for i in "${!prompts[@]}"; do
  echo "Generating illustration $((i+1))/${#prompts[@]}"
  t2i "${prompts[$i]}" \
    --provider foundry-mai2 \
    --width 1024 \
    --height 1024 \
    --out "illustration-$((i+1)).png"
done
```

```bash
chmod +x generate-illustrations.sh
./generate-illustrations.sh
```

---

### Example 3: Product Photography (GPT-Image-2)

```bash
t2i "a luxury smartwatch on a marble surface with a modern minimalist design, professional product photography, soft studio lighting" \
  --provider openai-gpt-image-2 \
  --width 1024 \
  --height 1024 \
  --out smartwatch.png
```

---

### Example 4: Diagram Generation (FLUX.2 Flex)

```bash
t2i "a clean technical diagram showing a microservices architecture with API Gateway, Auth Service, Database, and Message Queue boxes connected by labeled arrows, white background, black text and lines, professional" \
  --provider foundry-flux2 \
  --out architecture-diagram.png
```

---

### Example 5: Switch Providers Mid-Workflow

```bash
# If the primary provider is slow, try another
t2i "an abstract geometric wallpaper with gradient blues and purples" \
  --provider foundry-mai2 \
  --out wallpaper.png

# Or try GPT-Image-1.5 if you prefer
t2i "an abstract geometric wallpaper with gradient blues and purples" \
  --provider openai-gpt-image-1p5 \
  --out wallpaper.png
```

---

### Example 6: CI/CD Pipeline Integration

```yaml
# .github/workflows/generate-assets.yml
name: Generate Marketing Assets

on:
  schedule:
    - cron: '0 2 * * MON'  # Every Monday at 2 AM UTC

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Install t2i CLI
        run: dotnet tool install --global ElBruno.Text2Image.Cli
      
      - name: Generate hero banner
        run: |
          t2i "a modern tech office with natural light, developers collaborating" \
            --provider foundry-flux2 \
            --width 1920 \
            --height 1080 \
            --out hero-banner.png
        env:
          FOUNDRY_ENDPOINT: ${{ secrets.FOUNDRY_ENDPOINT }}
          FOUNDRY_API_KEY: ${{ secrets.FOUNDRY_API_KEY }}
      
      - name: Commit and push
        run: |
          git config user.name "Bot"
          git config user.email "bot@example.com"
          git add hero-banner.png
          git commit -m "chore: regenerate marketing assets"
          git push
```

---

## Performance & Quality Tips

### Model Selection Guide

**Choose GPT-Image-1.5 if:**
- You want reliable, consistent results
- You need broad availability across regions
- You're building a high-volume production system
- You want the lowest latency

**Choose GPT-Image-2 if:**
- Quality is your top priority
- You need enhanced detail and consistency
- You're generating premium assets (marketing, product shots)
- Your budget allows for slightly higher costs

**Choose FLUX.2 Pro if:**
- You need photorealistic, cinematic-quality images
- You're creating art, concept designs, or hero visuals
- Text rendering doesn't matter as much
- You want cutting-edge quality

**Choose FLUX.2 Flex if:**
- Your design includes text (logos, labels, UI mocks)
- You need perfect typography
- You're generating product packaging or certificates
- You want readable text in images

**Choose MAI-Image-2 if:**
- Speed is critical (batch workflows, real-time apps)
- You want synchronous responses (no polling)
- You're running production workloads at scale
- You want a good balance of quality and performance

### Prompt Engineering Patterns

**Pattern 1: Style Guide**
```
"[subject], [style], [lighting], [mood], [technical quality descriptor]"

Example: "a futuristic city at sunset, cyberpunk aesthetic, neon lights, moody, 4K concept art"
```

**Pattern 2: Design Requirements**
```
"[design element], [colors], [layout], [typography], [material], [background]"

Example: "a product label, white background, serif font 'ProductName', mint green accent, glossy finish"
```

**Pattern 3: Iterative Refinement**
```
First pass: "a coffee cup"
Refined: "a handcrafted ceramic coffee cup with steam rising, warm sunlight, shallow depth of field, professional photography"
Final: "a handcrafted ceramic coffee cup with steam rising, warm sunlight, shallow depth of field, professional product photography, matte black finish, minimalist white background"
```

### Rate Limits & Quotas

| Model | Rate Limit | Quota Type | Recommendation |
|-------|-----------|-----------|-----------------|
| GPT-Image-1.5 | Per-minute quota | Call-based | Check Azure Portal for your quota |
| GPT-Image-2 | Per-minute quota | Call-based | Check Azure Portal for your quota |
| FLUX.2 | Per-minute calls | Call-based | ~100 calls/minute typical |
| FLUX.2 Flex | Per-megapixel | Usage-based | $0.05/MP; 1024×1024 = ~$0.05 |
| MAI-Image-2 | Per-minute calls | Call-based | ~10-20 calls/minute typical |

**Best Practice:** Implement exponential backoff for rate limit handling:

```csharp
var retryCount = 0;
const int maxRetries = 3;

while (retryCount < maxRetries)
{
    try
    {
        var result = await generator.GenerateAsync(prompt);
        return result;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
        retryCount++;
        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        await Task.Delay(delay);
    }
}
```

### Cost Considerations

**GPT-Image-1.5 & GPT-Image-2:**
- Pay per image generated
- Same cost regardless of resolution (1024×1024 or 1792×1024)
- Estimate: $0.08–$0.12 per image depending on region and quota tier

**FLUX.2 Pro:**
- Pay per megapixel
- 1024×1024 = ~1 MP ≈ $0.08
- Lower costs for smaller images

**FLUX.2 Flex:**
- Pay per megapixel
- $0.05 per MP (cheaper than Pro)
- Best value for text-based designs

**MAI-Image-2:**
- Pay per call (varies by provider pricing)
- No per-megapixel charges
- Often the most economical for high-volume workflows

**Optimization:**
- Use smaller image sizes when full resolution isn't needed (768×768 vs 1024×1024)
- Batch generations to avoid repeated setup costs
- Cache generated images when possible
- Use MAI-Image-2 or GPT-Image-1.5 for high-volume, cost-sensitive workloads

---

## Getting Started

### Prerequisites

- **.NET 8.0+** (we support .NET 8 and .NET 10)
- **Cloud credentials** (Azure OpenAI API key or Microsoft Foundry endpoint + key)
- **Visual Studio, VS Code, or Rider** (or just dotnet CLI)

### Step 1: Install the Package

```bash
dotnet add package ElBruno.Text2Image.Foundry
```

### Step 2: Add Credentials

Choose one method:

**Option A: User Secrets (Development)**
```bash
dotnet user-secrets set FLUX2_ENDPOINT "https://your-resource.services.ai.azure.com"
dotnet user-secrets set FLUX2_API_KEY "your-api-key"
```

**Option B: Environment Variables (CI/CD)**
```bash
export FLUX2_ENDPOINT="https://your-resource.services.ai.azure.com"
export FLUX2_API_KEY="your-api-key"
```

**Option C: appsettings.json (Simple)**
```json
{
  "FLUX2_ENDPOINT": "https://your-resource.services.ai.azure.com",
  "FLUX2_API_KEY": "your-api-key"
}
```

### Step 3: Write Code

```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;

using var generator = new Flux2Generator(
    endpoint: "https://your-resource.services.ai.azure.com",
    apiKey: "your-api-key");

var result = await generator.GenerateAsync("a beautiful sunset");
await result.SaveAsync("output.png");
```

### Step 4: Generate!

```bash
dotnet run
```

### Resources & Documentation

- **📖 Full Documentation:** [docs/](https://github.com/elbruno/ElBruno.Text2Image/tree/main/docs)
- **🚀 Setup Guides:**
  - [FLUX.2 Setup](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/flux2-setup-guide.md)
  - [MAI-Image-2 Setup](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/mai-image-2-setup-guide.md)
  - [GPT-Image Setup](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/gpt-image-1p5-setup-guide.md)
- **💾 Code Samples:** [src/samples/](https://github.com/elbruno/ElBruno.Text2Image/tree/main/src/samples)
  - Scenario 03: FLUX.2 Cloud
  - Scenario 13: MAI-Image-2 Cloud
  - Scenario 15: GPT-Image-1.5 Cloud
  - Scenario 16: GPT-Image-2 Cloud
- **🛠️ API Reference:** [NuGet Package Docs](https://www.nuget.org/packages/ElBruno.Text2Image.Foundry)
- **🔒 Security Guide:** [docs/security.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/security.md)

---

## Call to Action

**Try it now:**

```bash
# Install the CLI
dotnet tool install --global ElBruno.Text2Image.Cli

# Run setup
t2i config

# Generate your first image
t2i "a beautiful landscape at sunset"
```

**Share your creations:**
- Show us what you build on [GitHub Discussions](https://github.com/elbruno/ElBruno.Text2Image/discussions)
- Tag us on Twitter/X: [@elbruno](https://x.com/elbruno)
- Contributing? PRs welcome! See [CONTRIBUTING.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/CONTRIBUTING.md)

**Questions?**
- 📚 Check the [docs](https://github.com/elbruno/ElBruno.Text2Image/tree/main/docs)
- 🐛 Found a bug? [Open an issue](https://github.com/elbruno/ElBruno.Text2Image/issues)
- 💬 Have feedback? Join the [discussions](https://github.com/elbruno/ElBruno.Text2Image/discussions)

**Next up:** We're working on local GPU support (ONNX Runtime) and prompt optimization workflows. Stay tuned.

---

## Closing Thoughts

Multi-model support isn't just about having more choices. It's about resilience, optimization, and giving you control. No vendor lock-in. No "you're stuck with this provider" moments. Just clean .NET code that works with whatever backend you choose.

**Four models. One API. Infinite possibilities.**

Happy generating! 🎨

---

*Made with ❤️ by ElBruno and the community. If this helps you build something awesome, consider [supporting the project](https://github.com/sponsors/elbruno).*
