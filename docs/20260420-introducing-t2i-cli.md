# 🚀 Meet t2i — The ElBruno.Text2Image CLI

_Updated 2026-04-20: clarified secret-storage guidance for security, fixed v0.10.0 model configuration._

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

The CLI stores credentials across multiple secure backends. Here's the resolution chain (highest priority first), plus guidance on which to use:

### 1. **Local Development** — OS-Native Encrypted Storage (DPAPI/File)

**Windows: DPAPI Encryption** ✅ Recommended

Secrets stored at `%LOCALAPPDATA%\t2i\secrets.dpapi` are encrypted using Windows Data Protection API and keyed to your Windows login. Only your user can decrypt them — no shell history leakage, no accidental dotfile commits.

```bash
t2i config    # Interactive setup — credentials stored encrypted
t2i "your prompt"
```

**macOS/Linux: File with Restricted Permissions** ✅ Recommended

Secrets live in `~/.config/t2i/secrets.json` with Unix `0600` permissions (readable only by you). No encryption, but OS-level access control prevents shell history and sibling process leakage.

```bash
t2i config    # Interactive setup — credentials stored with 0600 permissions
t2i "your prompt"
```

### 2. **CI/CD Pipelines** — Environment Variables (With Caution)

Environment variables are appropriate **only** for ephemeral, isolated environments:

```bash
export T2I_FOUNDRY_FLUX2_APIKEY="your-key-here"
t2i "your prompt"
```

⚠️ **Environment Variable Security Risks (Local Development):**
- Process visibility: Shell history captures env var assignments (`~/.bash_history`, PowerShell logs)
- Process tree leakage: Child processes inherit all env vars; developers inherit vars from build tools, CI runners, etc.
- Accidental exposure: Debug output (`env`, `printenv`, `set`) reveals all environment variables
- Dotfile commits: Developers often add env vars to `.bashrc`, `.zshrc`, `.profile` — then accidentally commit to personal dotfiles repos
- **DO NOT use env vars for local development.** Use the OS-native storage above instead.

✅ **When env vars ARE appropriate:**
- GitHub Actions / Azure Pipelines: Secrets injected from vault, process lifetime is ephemeral (minutes), environment is isolated (container, fresh VM)
- Docker: Use orchestrator secret injection (Docker secrets, Kubernetes), NOT `ENV` directives in Dockerfile
- Server/container deployments: 12-factor app pattern, dedicated service account, no interactive shell

### 3. **CLI Override** — For One-Off Tests Only

```bash
t2i "your prompt" --api-key "key-here"
```

⚠️ Secrets passed via CLI flags appear in your shell history. Only use for quick testing; prefer stored config for regular use.

### 4. **How Resolution Works**

The CLI checks in this order:
1. CLI flag (`--api-key`) — highest priority, ephemeral
2. Environment variable (`T2I_FOUNDRY_FLUX2_APIKEY`)
3. DPAPI store (Windows: `%LOCALAPPDATA%\t2i\secrets.dpapi`)
4. Plaintext file (Unix: `~/.config/t2i/secrets.json`)
5. Not found

### 5. **Security Best Practices**

| Do ✅ | Don't ❌ |
|------|---------|
| Use `t2i config` for local setup (stores encrypted/restricted) | Add `export T2I_*=...` to `.bashrc` or `.zshrc` — commits to dotfiles repo |
| Use env vars in CI/CD from a vault (GitHub Secrets, Azure Key Vault) | Use `ENV` directives in Dockerfiles — secrets baked into image layers |
| Use `--api-key` for one-time testing | Leave API keys in shell history or error logs |
| Rotate API keys regularly | Share credentials via chat, email, or code comments |

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
- 🐛 **Issues:** [github.com/elbruno/ElBruno.Text2Image/issues](https://github.com/elbruno/ElBruno.Text2Image/issues)

Happy generating! 🖼️

_El Bruno_
