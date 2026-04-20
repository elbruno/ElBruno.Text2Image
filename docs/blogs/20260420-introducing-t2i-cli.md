# 🚀 Meet t2i — The ElBruno.Text2Image CLI

_Updated 2026-04-20: clarified secret-storage guidance for security, fixed v0.10.0 model configuration._

![t2i CLI hero — a terminal emitting a ribbon of AI-generated imagery, generated with t2i itself using MAI-Image-2](../../images/20260420-introducing-t2i-cli-hero.png)

> _This hero image was generated with `t2i` itself — see [the prompt companion doc](./20260420-introducing-t2i-cli-image-prompt.md) for the exact prompt, command, and parameters._

⚠️ _This blog post was created with the help of AI tools. Yes, I used a bit of magic from language models to organize my thoughts and automate the boring parts, but the CLI geeking and all the 🖼️ generation are 100% mine._

Hi!

I just shipped **t2i**, a terminal-first CLI tool for ElBruno.Text2Image. Generate images from your shell in two commands — no UI, no browser, no nonsense. Just a simple, powerful interface to image generation from the cloud.

This is the **Lite edition** (cloud-only, ~2.4 MB on NuGet) — perfect for CI/CD pipelines, deployment scripts, batch jobs, and developers who live in the terminal.

---

## 🛠️ Install

### Option 1: .NET Tool (recommended)

If you have .NET 8+ installed:

```bash
dotnet tool install --global ElBruno.Text2Image.Cli
```

Then use `t2i` from anywhere on your machine.

### Option 2: Self-Contained Binaries

No .NET? Download pre-built binaries from the [release page](https://github.com/elbruno/ElBruno.Text2Image/releases/tag/cli-v0.10.0). Currently available for:

- **Windows x64**
- **Linux x64**
- **macOS arm64** (Intel coming back soon)

Just extract and run. One file, no dependencies.

---

## 🚀 First Image in 30 Seconds

Ready to generate? Two commands:

```bash
t2i config
```

This launches an interactive setup wizard (powered by [Spectre.Console](https://spectreconsole.net/), so it's pretty). Pick your provider, enter your API key, and you're done. The CLI stores everything securely.

Then generate:

```bash
t2i "a robot painting a landscape"
```

That's it. The image appears in your current directory.

---

## 🔐 Where Do My Secrets Live?

Short version: `t2i config` stores credentials securely on your machine — DPAPI-encrypted on Windows, `0600`-permissioned file on macOS/Linux. Env vars are for CI only. CLI `--api-key` is for one-off tests.

For the full resolution chain, CI/CD guidance, and the do/don't table, see **[docs/cli-secrets.md](../cli-secrets.md)**.

---

## ☁️ Providers in Lite

This release ships with two cloud providers on Azure AI Foundry:

| Provider | Model | Best For |
|---|---|---|
| `foundry-flux2` | FLUX.2 Pro | High-quality, fine control, batch jobs |
| `foundry-mai2` | MAI-Image-2 | Synchronous API, rich prompts, fast iteration |

More providers (Anthropic, OpenAI) coming in future releases.

---

## 📋 Useful Commands

Here's the CLI cheat sheet:

```bash
# List available providers
t2i providers

# Run health checks (config + API connectivity)
t2i doctor

# Show stored secrets (redacted)
t2i secrets list

# Display version + commit SHA
t2i version

# Interactive config setup
t2i config

# Generate with default provider
t2i "a cyberpunk cityscape at night"

# Generate with specific provider, dimensions, and output file
t2i "my prompt" \
  --provider foundry-mai2 \
  --width 1024 \
  --height 1024 \
  --output ./my-image.png

# Show help
t2i --help
t2i generate --help

# Teach your AI agent
t2i init
```

## 🔄 Switching Models (v0.10.0+)

Both providers support multiple model variants. By default, `foundry-mai2` uses `MAI-Image-2` and `foundry-flux2` uses `FLUX.2-pro`. To switch models:

```bash
# Use MAI-Image-2e
t2i config set foundry-mai2.model MAI-Image-2e

# Use FLUX.2 Flex for text-heavy design and logos
t2i config set foundry-flux2.model FLUX.2-flex

# View your configuration
t2i config show
```

The `config show` command displays your endpoint and model in plain text, with only the API key masked. This makes it easy to verify your setup without revealing sensitive credentials.

## 🤖 Teach Your AI Agent — `t2i init`

Inspired by [Aspire's agent init pattern](https://aspire.dev/get-started/ai-coding-agents/), t2i now ships with a skill file your AI coding agent can read. Run this in any repo:

```bash
t2i init
```

That writes a `SKILL.md` to both `.github/skills/t2i/` and `.claude/skills/t2i/`. From that point on, GitHub Copilot, Claude Code, and any MCP-aware agent know:

- Which `t2i` commands exist and when to use each one
- How to set up secrets safely (env vars first, never commit keys)
- The full provider list and which one to default to
- Common workflows: first-time setup, single image, batch loops

Want only one target?

```bash
t2i init --target github   # only .github/skills/t2i/
t2i init --target claude   # only .claude/skills/t2i/
t2i init --force           # overwrite existing skill files
```

The canonical version of this skill also lives in this repo at `.github/skills/t2i/SKILL.md` — that means if you open the ElBruno.Text2Image source itself in Copilot or Claude Code, your agent already knows how to drive the CLI.

---

## 📦 Coming Soon — Other Platforms

### winget

The manifest stub is already in the repo (`winget/manifests/E/ElBruno/Text2Image/0.10.0/`). First submission to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) is queued. Automation will come in v0.2.0.

### Homebrew

Tap `elbruno/elbruno` is planned for v0.2.0.

### macOS Intel (osx-x64)

GitHub's macOS-13 runner queue has been a bit slow lately. Intel Mac users should use `dotnet tool install` for now. The self-contained binary will return once the runners stabilize.

### Full Edition (Local GPU)

The **Cli.Full** edition — supporting local inference with ONNX Runtime (CPU, CUDA, DirectML, NPU) — is coming in a future release. ~200 MB, separate package.

---

## 🎨 Sample Usages

Here are some fun one-liners to try:

```bash
t2i "a cyberpunk taco truck at sunset, cinematic lighting, volumetric fog"

t2i "a low-poly 3D render of a friendly robot waving" --provider foundry-mai2

t2i "minimalist line art of a cat reading a book" --width 1024 --height 1024 --output cat.png

t2i "watercolor painting of the Madrid skyline at dusk, golden hour" --output madrid.png
```

**Tip:** The better your prompt, the better the image. Be specific about style (cinematic, watercolor, line art), mood (peaceful, energetic), and composition (wide-angle, close-up). Models reward detail.

---

## 🤝 Let's Go

[Download it now](https://www.nuget.org/packages/ElBruno.Text2Image.Cli/0.1.0), run `t2i config`, and start generating. Found a bug? [File an issue](https://github.com/elbruno/ElBruno.Text2Image/issues).

**Links:**

- 📦 **NuGet:** [ElBruno.Text2Image.Cli/0.10.0](https://www.nuget.org/packages/ElBruno.Text2Image.Cli/0.10.0)
- 📂 **Release:** [github.com/elbruno/ElBruno.Text2Image/releases/tag/cli-v0.10.0](https://github.com/elbruno/ElBruno.Text2Image/releases/tag/cli-v0.10.0)
- 📖 **Full Docs:** [docs/cli-tool.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)
- 🖼️ **Hero image prompt:** [docs/blogs/20260420-introducing-t2i-cli-image-prompt.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/blogs/20260420-introducing-t2i-cli-image-prompt.md)
- 🐛 **Issues:** [github.com/elbruno/ElBruno.Text2Image/issues](https://github.com/elbruno/ElBruno.Text2Image/issues)

Happy generating! 🖼️

_El Bruno_
