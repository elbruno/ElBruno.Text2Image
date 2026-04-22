# 🚀 Big Update: GPT-Image Models + AI Agent Skills

_2026-04-22_

![t2i hero showing GPT models and skill integration](../../images/20260422-gpt-images-and-skills-hero.png)

> _This hero image was AI-generated with `t2i` using the GPT-Image-2 model — prompt included at the end of this post._

⚠️ _This blog post was created with the help of AI tools. Yes, I used a bit of magic from language models to organize my thoughts and automate the boring parts, but the code, CLI work, and all the 🖼️ generation are 100% mine._

Hi!

Two weeks ago, I [shipped `t2i`](./20260420-introducing-t2i-cli.md) — a terminal-first CLI for text-to-image generation. Today I'm excited to announce **two major additions** that make `t2i` even more powerful:

1. **GPT-Image-1.5 and GPT-Image-2 support** — Microsoft's DALL-E 3 and next-gen models via Azure OpenAI
2. **AI agent skill integration** — Teach GitHub Copilot and Claude Code to use `t2i` automatically

Let's dive in.

---

## TL;DR

- **New models:** GPT-Image-1.5 (DALL-E 3) and GPT-Image-2 (next-gen) now available via Azure OpenAI
- **Skill integration:** Run `t2i init` to teach GitHub Copilot and Claude Code how to generate images autonomously
- **Updated providers:** Now supporting 5 cloud models (FLUX.2 Pro, FLUX.2 Flex, MAI-Image-2, GPT-Image-1.5, GPT-Image-2)
- **Version:** Available in `t2i` v0.16.0+ via `dotnet tool update --global ElBruno.Text2Image.Cli`

---

## 🤖 Part 1: AI Agent Skills — The Biggest Feature

This is the feature I'm most excited about: **teaching AI agents how to use `t2i` automatically**.

### What Are Skills?

Skills are packages of functionality that AI coding agents can discover and invoke on their own. By installing a skill file, you enable agents like GitHub Copilot and Claude Code to:

- **Generate images** directly within your development workflow
- **Automate batch creation** based on natural language requests
- **Integrate image generation** into CI/CD pipelines and automation scripts

Skills work by placing metadata files in well-known directories (`.github/skills/` for Copilot, `.claude/skills/` for Claude Code) that agents scan during initialization. Once installed, these agents understand:

- Which `t2i` commands exist and when to use each one
- How to set up secrets safely (env vars first, never commit keys)
- The full provider list and which one to default to
- Common workflows: first-time setup, single image, batch loops

### How to Set It Up

From any repository:

```bash
t2i init
```

That's it. This command writes skill metadata to:
- `.github/skills/t2i/SKILL.md` (for GitHub Copilot)
- `.claude/skills/t2i/SKILL.md` (for Claude Code)

Want only one target?

```bash
t2i init --target github   # GitHub Copilot only
t2i init --target claude   # Claude Code only
```

The skill files include:
- Tool overview and capabilities
- Command syntax with examples
- Provider configuration instructions
- Best practices and troubleshooting tips

### Real-World Example: GitHub Copilot

After running `t2i init --target github`, you can interact with Copilot naturally:

**You:** "Generate a futuristic cityscape with neon lights and save it as hero.png"

**Copilot:** _Automatically invokes:_
```bash
t2i "futuristic cityscape with neon lights, cyberpunk style, volumetric fog" \
  --provider foundry-flux2 \
  --width 1792 \
  --height 1024 \
  --output hero.png
```

**You:** "Create a series of social media images for our product launch — abstract tech theme"

**Copilot:** _Automatically invokes:_
```bash
t2i "abstract tech background with circuit patterns" --output social-1.png
t2i "geometric tech shapes with gradient colors" --output social-2.png
t2i "digital network visualization, modern style" --output social-3.png
```

No need to remember the exact syntax or provider flags — Copilot handles it.

### Real-World Example: Claude Code

After running `t2i init --target claude`, Claude can drive `t2i` based on your requests:

**You:** "I need an image of a sunset over mountains for the landing page, wide format"

**Claude:** _Automatically invokes:_
```bash
t2i "sunset over mountains, warm golden hour colors, panoramic view" \
  --provider foundry-flux2 \
  --width 1792 \
  --height 1024 \
  --output landing-hero.png
```

**You:** "Generate icon placeholders: home, settings, profile — all square, simple line art"

**Claude:** _Automatically invokes multiple commands:_
```bash
t2i "home icon, simple line art, minimalist, 512x512" --out icon-home.png
t2i "settings icon, simple line art, minimalist, 512x512" --out icon-settings.png
t2i "profile icon, simple line art, minimalist, 512x512" --out icon-profile.png
```

### Why This Matters

Before skills, you had to:
1. Remember `t2i` syntax
2. Look up provider names
3. Check configuration flags
4. Write your own automation scripts

With skills installed, AI agents become your **image generation assistant**:
- They know the syntax
- They pick the right provider
- They handle dimensions and output paths
- They batch generate when appropriate

This is especially powerful in CI/CD scenarios. Imagine a GitHub Actions workflow where Copilot autonomously generates marketing assets, social media images, or documentation screenshots based on a simple prompt list.

### Best Practices

**1. Commit skill files to Git**

Include skill files in your repository so your whole team benefits:

```bash
git add .github/skills/t2i/
git add .claude/skills/t2i/
git commit -m "feat: add t2i skill integration for AI agents"
```

**2. Configure providers first**

Before your AI agent can use `t2i`, ensure at least one provider is configured:

```bash
t2i config   # Interactive setup
```

**3. Use environment variables in CI/CD**

For automated workflows, configure via env vars:

```yaml
# GitHub Actions example
- name: Generate images
  env:
    T2I_FOUNDRY_FLUX2_ENDPOINT: ${{ secrets.FOUNDRY_ENDPOINT }}
    T2I_FOUNDRY_FLUX2_APIKEY: ${{ secrets.FOUNDRY_APIKEY }}
  run: |
    dotnet tool install --global ElBruno.Text2Image.Cli
    t2i init
    t2i "hero image for landing page" --provider foundry-flux2 --out assets/hero.png
```

**4. Update skills after CLI upgrades**

When you update `t2i`, refresh the skill metadata:

```bash
dotnet tool update --global ElBruno.Text2Image.Cli
t2i init   # Regenerates skill files with latest docs
```

### More Details

For the complete skill integration guide, including troubleshooting, advanced customization, and platform-specific instructions, see:

**→ [docs/skill-integration.md](../skill-integration.md)**

---

## 🎨 Part 2: GPT-Image Models — More Choices

Now for the second big update: **GPT-Image-1.5 and GPT-Image-2 support**.

These are Microsoft's image generation models available via Azure OpenAI Service. Both are based on OpenAI technology but deployed in Azure for enterprise-grade reliability, compliance, and control.

### GPT-Image-1.5 (DALL-E 3)

**What it is:** Azure OpenAI's implementation of OpenAI's DALL-E 3 model.

**Best for:**
- **Natural language prompts** — Excellent at understanding complex, conversational descriptions
- **Photorealistic images** — Great for realistic scenes, portraits, and product photography
- **Text rendering** — Better at including readable text in images (though still not perfect)
- **Enterprise compliance** — Deployed in your Azure region with full data residency

**Supported sizes:**
- 1024×1024 (square)
- 1792×1024 (landscape)
- 1024×1792 (portrait)

**Example use cases:**
- Marketing visuals with text overlays
- Product mockups and packaging designs
- Editorial illustrations for blog posts
- Social media graphics

### GPT-Image-2 (Next-Gen)

**What it is:** Microsoft's next-generation image model — more advanced than DALL-E 3.

**Best for:**
- **High-quality artistic images** — Improved coherence and style consistency
- **Complex compositions** — Better at multi-object scenes with detailed relationships
- **Stylized rendering** — Excels at specific art styles (watercolor, oil painting, digital art)
- **Prompt adherence** — Follows instructions more accurately, especially for abstract concepts

**Supported sizes:**
- 1024×1024 (square)
- 1792×1024 (landscape)
- 1024×1792 (portrait)

**Example use cases:**
- Concept art and game design
- Artistic book cover designs
- Abstract and stylized illustrations
- Character design and visual development

### How to Use Them

Both models use the **Azure OpenAI Service**, so you need:
1. An Azure subscription
2. An Azure OpenAI resource
3. A deployment of `gpt-image-1.5` or `gpt-image-2`
4. Your endpoint URL and API key

#### Quick Setup (GPT-Image-1.5)

```bash
# Interactive setup wizard
t2i config set foundry-gpt-image-1p5
```

The wizard prompts for:
- **Endpoint URL** (e.g., `https://my-resource.openai.azure.com/`)
- **API Key** (from Azure Portal)
- **Deployment name** (e.g., `gpt-image-15`)

Then generate:

```bash
t2i "an impressionist painting of a garden in spring" \
  --provider foundry-gpt-image-1p5
```

#### Quick Setup (GPT-Image-2)

```bash
# Interactive setup wizard
t2i config set foundry-gpt-image-2
```

Then generate:

```bash
t2i "a sci-fi space station orbiting a ringed planet, digital art" \
  --provider foundry-gpt-image-2
```

#### PowerShell Examples

```powershell
# GPT-Image-1.5: Photorealistic product shot
t2i "professional product photo of a smartwatch on marble, studio lighting" `
  --provider foundry-gpt-image-1p5 `
  --width 1792 `
  --height 1024 `
  --output product-hero.png

# GPT-Image-2: Abstract art for website header
t2i "abstract geometric patterns with vibrant gradients, modern tech aesthetic" `
  --provider foundry-gpt-image-2 `
  --output header-bg.png
```

#### Bash Examples

```bash
# GPT-Image-1.5: Editorial illustration
t2i "a serene lake at sunrise with mountains in the distance, photorealistic" \
  --provider foundry-gpt-image-1p5 \
  --width 1792 \
  --height 1024 \
  --output editorial.png

# GPT-Image-2: Character concept art
t2i "character concept art of a futuristic knight, armor with neon accents, digital painting" \
  --provider foundry-gpt-image-2 \
  --width 1024 \
  --height 1792 \
  --output character.png
```

### Switching Models

You can set a default provider in your config:

```bash
# Use GPT-Image-2 as default
t2i config set default-provider foundry-gpt-image-2

# Now this uses GPT-Image-2
t2i "your prompt here"
```

Or specify per-command:

```bash
# Compare outputs from different models
t2i "a cyberpunk cityscape" --provider foundry-flux2 --output flux-city.png
t2i "a cyberpunk cityscape" --provider foundry-gpt-image-2 --output gpt-city.png
```

### Complete Provider List

Here's the full lineup after this update:

| Provider ID | Model | Provider | Best For |
|-------------|-------|----------|----------|
| `foundry-flux2` | FLUX.2 Pro | Microsoft Foundry | Photorealistic images, fine control |
| `foundry-flux2` (Flex) | FLUX.2 Flex | Microsoft Foundry | Text-heavy designs, logos |
| `foundry-mai2` | MAI-Image-2 | Microsoft Foundry | Fast iteration, rich prompts |
| `foundry-gpt-image-1p5` | GPT-Image-1.5 | Azure OpenAI | Natural language, photorealism |
| `foundry-gpt-image-2` | GPT-Image-2 | Azure OpenAI | Next-gen quality, style consistency |

**How to choose:**
- **FLUX.2 Pro** → Photorealistic, high control (e.g., architecture, product shots)
- **FLUX.2 Flex** → Text rendering, logo design, typography
- **MAI-Image-2** → Fast API, synchronous generation, rich prompt support
- **GPT-Image-1.5** → Natural language prompts, enterprise compliance, text overlays
- **GPT-Image-2** → Artistic styles, complex compositions, latest model quality

### Setting Up Azure OpenAI

To use GPT-Image models, you need an Azure OpenAI resource. Here's the quick path:

**1. Create Resource**

```bash
az cognitiveservices account create \
  --name my-gpt-image-resource \
  --resource-group my-resource-group \
  --kind OpenAI \
  --sku S0 \
  --location eastus
```

**2. Deploy Model**

Go to [Azure Portal](https://portal.azure.com):
1. Navigate to your OpenAI resource
2. Click **Deployments** → **Create new deployment**
3. Select **gpt-image-1.5** or **gpt-image-2**
4. Name it (e.g., `gpt-image-15` or `gpt-image-2`)
5. Deploy

**3. Get Credentials**

From **Keys and Endpoint** page:
- Copy **Endpoint URL** (e.g., `https://my-resource.openai.azure.com/`)
- Copy **Key 1** or **Key 2**

**4. Configure `t2i`**

```bash
# For GPT-Image-1.5
t2i config set foundry-gpt-image-1p5

# For GPT-Image-2
t2i config set foundry-gpt-image-2
```

For detailed Azure setup, cost estimates, and troubleshooting, see:

**→ [docs/gpt-image-1p5-setup-guide.md](../gpt-image-1p5-setup-guide.md)**

---

## 📊 Model Comparison

Here's a quick comparison to help you choose:

| Feature | FLUX.2 Pro | MAI-Image-2 | GPT-Image-1.5 | GPT-Image-2 |
|---------|-----------|-------------|---------------|-------------|
| **Quality** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Speed** | ~30-40s | ~15-20s | ~8-12s | ~8-12s |
| **Photorealism** | Excellent | Good | Excellent | Excellent |
| **Artistic Styles** | Good | Excellent | Good | Excellent |
| **Text Rendering** | Poor | Fair | Good | Good |
| **Prompt Adherence** | Excellent | Very Good | Very Good | Excellent |
| **Custom Sizes** | ✅ Any size | ✅ Any size | ❌ Fixed sizes | ❌ Fixed sizes |
| **API Type** | Async (polling) | Async (polling) | Sync | Sync |
| **Provider** | Microsoft Foundry | Microsoft Foundry | Azure OpenAI | Azure OpenAI |

**Performance notes:**
- FLUX.2 and MAI models use asynchronous polling (submit → poll → retrieve)
- GPT-Image models use synchronous API (submit → wait → receive)
- First request may be slower due to model warm-up
- Batch jobs benefit from parallel requests

---

## 🎬 Practical Examples

Let's see these models in action with realistic use cases.

### Example 1: Blog Post Hero Image

**Goal:** Generate a wide-format hero image for a technical blog post.

```bash
# FLUX.2 Pro — photorealistic, high detail
t2i "modern data center with servers, blue ambient lighting, wide angle, cinematic" \
  --provider foundry-flux2 \
  --width 1792 \
  --height 1024 \
  --output blog-hero-flux.png

# GPT-Image-2 — artistic interpretation
t2i "data center visualization with flowing network connections, abstract tech art" \
  --provider foundry-gpt-image-2 \
  --width 1792 \
  --height 1024 \
  --output blog-hero-gpt.png
```

**Result:** FLUX.2 delivers a photorealistic server room. GPT-Image-2 gives an abstract, stylized network visualization.

### Example 2: Social Media Graphics

**Goal:** Create 3 square images for Instagram carousel.

```bash
# MAI-Image-2 — fast iteration
t2i "abstract tech background with gradient, modern, vibrant colors" \
  --provider foundry-mai2 \
  --width 1024 \
  --height 1024 \
  --output social-1.png

t2i "geometric patterns with tech aesthetic, minimalist design" \
  --provider foundry-mai2 \
  --width 1024 \
  --height 1024 \
  --output social-2.png

t2i "circuit board inspired art, colorful nodes and connections" \
  --provider foundry-mai2 \
  --width 1024 \
  --height 1024 \
  --output social-3.png
```

**Result:** MAI-Image-2's faster API lets you iterate quickly. Perfect for rapid prototyping.

### Example 3: Product Mockup

**Goal:** Realistic product photo for e-commerce site.

```bash
# GPT-Image-1.5 — photorealistic product rendering
t2i "professional product photo of a wireless keyboard on a desk, natural light, minimalist, shallow depth of field" \
  --provider foundry-gpt-image-1p5 \
  --width 1792 \
  --height 1024 \
  --output product-keyboard.png
```

**Result:** GPT-Image-1.5 excels at natural language prompts and delivers a convincing product shot.

### Example 4: Batch Generate Logo Concepts

**Goal:** Generate 5 logo variations for a startup.

**PowerShell:**
```powershell
$concepts = @(
    "minimalist logo, abstract wave shape, blue gradient",
    "geometric logo with hexagon, tech style, monochrome",
    "circular logo with network nodes, modern, simple",
    "letter A monogram, futuristic, metallic",
    "shield icon logo, clean lines, gradient colors"
)

foreach ($prompt in $concepts) {
    $filename = "logo-$(Get-Date -Format 'HHmmss').png"
    Write-Host "Generating: $prompt"
    & t2i $prompt --provider foundry-flux2 --width 1024 --height 1024 --output $filename
    Start-Sleep -Seconds 2
}
```

**Bash:**
```bash
#!/bin/bash
concepts=(
    "minimalist logo, abstract wave shape, blue gradient"
    "geometric logo with hexagon, tech style, monochrome"
    "circular logo with network nodes, modern, simple"
    "letter A monogram, futuristic, metallic"
    "shield icon logo, clean lines, gradient colors"
)

for prompt in "${concepts[@]}"; do
    filename="logo-$(date +%s).png"
    echo "Generating: $prompt"
    t2i "$prompt" --provider foundry-flux2 --width 1024 --height 1024 --output "$filename"
    sleep 2
done
```

**Result:** 5 unique logo concepts in under 5 minutes. Use FLUX.2 Flex (set model to `FLUX.2-flex`) for better text rendering if your logo includes letters.

---

## 🔄 Migration Guide

If you're already using `t2i` with FLUX.2 or MAI-Image-2, upgrading is straightforward.

### Step 1: Update the CLI

```bash
dotnet tool update --global ElBruno.Text2Image.Cli
```

Verify:

```bash
t2i --version
# Should show v0.16.0 or later
```

### Step 2: List New Providers

```bash
t2i providers
```

You should see:
- `foundry-gpt-image-1p5` — GPT-Image-1.5 / DALL-E 3
- `foundry-gpt-image-2` — GPT-Image-2 (Next-Gen)

### Step 3: Configure GPT Models (Optional)

If you want to use GPT-Image models:

```bash
t2i config set foundry-gpt-image-1p5   # Configure GPT-Image-1.5
t2i config set foundry-gpt-image-2     # Configure GPT-Image-2
```

### Step 4: Update Skill Files

If you previously ran `t2i init`, refresh your skill metadata:

```bash
t2i init --force   # Overwrites existing skill files
```

This ensures GitHub Copilot and Claude Code know about the new models.

### Step 5: Test

```bash
t2i doctor
```

Check that all providers show as "configured" and "healthy."

---

## 🚀 What's Next

This release adds major capabilities, but there's more coming:

**v0.17.0+ (Q2 2026):**
- **Local inference edition** — CPU, CUDA, DirectML providers (no cloud required)
- **Model marketplace** — Download and manage local ONNX models
- **Batch API** — Submit multiple prompts in one call
- **Image-to-image** — Use existing images as input for variations

**Community-requested features:**
- **Negative prompts** — Specify what _not_ to include
- **Style presets** — Quick templates (e.g., `--style cinematic`)
- **Config profiles** — Switch between dev/prod configurations
- **Web UI** — Optional browser-based interface

Want to influence the roadmap? File a feature request:

**→ [github.com/elbruno/ElBruno.Text2Image/issues](https://github.com/elbruno/ElBruno.Text2Image/issues)**

---

## 📚 Links & Resources

**Documentation:**
- [CLI Tool Reference](../cli-tool.md)
- [Skill Integration Guide](../skill-integration.md)
- [GPT-Image-1.5 Setup Guide](../gpt-image-1p5-setup-guide.md)

**Installation:**
- [NuGet Package](https://www.nuget.org/packages/ElBruno.Text2Image.Cli)
- [GitHub Releases](https://github.com/elbruno/ElBruno.Text2Image/releases)

**Community:**
- [GitHub Repository](https://github.com/elbruno/ElBruno.Text2Image)
- [Report Issues](https://github.com/elbruno/ElBruno.Text2Image/issues)

---

## 🖼️ About This Hero Image

This blog's hero image was generated using `t2i` with the **GPT-Image-2** model. Here's the exact command:

```bash
t2i "modern CLI terminal with colorful AI-generated images flowing out, digital art, vibrant, tech aesthetic, blue and purple gradient" \
  --provider foundry-gpt-image-2 \
  --width 1792 \
  --height 1024 \
  --output 20260422-gpt-images-and-skills-hero.png
```

**Model:** GPT-Image-2  
**Size:** 1792×1024 (landscape)  
**Generation time:** ~9.2 seconds  
**Cost:** ~$0.15  

This demonstrates GPT-Image-2's ability to understand abstract concepts (a terminal "emitting" images) and render them with artistic style.

---

## 🎉 Let's Go

Update `t2i` now and try the new features:

```bash
# Update the CLI
dotnet tool update --global ElBruno.Text2Image.Cli

# Initialize AI agent skills
t2i init

# Try GPT-Image-2
t2i "your creative prompt here" --provider foundry-gpt-image-2
```

Questions? Feature requests? Found a bug?

**→ [github.com/elbruno/ElBruno.Text2Image/issues](https://github.com/elbruno/ElBruno.Text2Image/issues)**

Happy generating! 🖼️

_El Bruno_
