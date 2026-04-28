# v0.16.0 — Skill Integration for AI Coding Agents

## Overview

**v0.16.0** brings **GitHub Copilot** and **Claude Code** skill integration to the `t2i` CLI, making the tool discoverable and usable by AI coding agents. This enables seamless automation of image generation within development workflows—agents can now invoke `t2i` autonomously to generate images based on natural language requests.

**Headline:** AI agents can now generate images directly in your development workspace. No manual configuration needed—just run `t2i init`.

---

## ✨ What's New

### 1. **Skill Integration for AI Coding Agents**

#### GitHub Copilot Support
- Run `t2i init --target github` to install skill metadata in `.github/skills/t2i/`
- GitHub Copilot automatically discovers the skill and can invoke `t2i` commands
- Works with GitHub Copilot in VS Code, GitHub.com, and Copilot CLI

#### Claude Code Support  
- Run `t2i init --target claude` to install skill metadata in `.claude/skills/t2i/`
- Claude Code (VS Code extension, web editor) discovers and invokes `t2i` autonomously
- Perfect for image generation tasks embedded in coding workflows

#### Default (All Platforms)
- Run `t2i init` (no flags) to install for both Copilot and Claude Code

**Example Workflow:**
```bash
# User: "Generate a logo for my project and save it as logo.png"
# GitHub Copilot → invokes: t2i "project logo" --output logo.png
# Result: Logo image created and saved automatically
```

### 2. **Enhanced Documentation**

#### New Guide: `docs/skill-integration.md`
Comprehensive guide covering:
- What skills are and why `t2i` ships one
- Step-by-step setup with `t2i init`
- GitHub Copilot workflow examples
- Claude Code workflow examples
- Skill updates and lifecycle management
- Troubleshooting and best practices
- CI/CD integration patterns

#### Updated: `README.md`
- Added "🤖 AI Agent Integration" section highlighting the skill capability
- Links to skill setup guide
- Quick reference on `t2i init` command

#### Enhanced: `docs/cli-tool.md`
- Detailed `t2i init` command reference
- Flag options: `--target`, `--force`
- Examples showing multi-platform setup

### 3. **Improved Embedded Skill File (`SKILL.md`)**

The `SKILL.md` resource (embedded in the CLI and deployed by `t2i init`) now includes:
- Comprehensive command reference with all flags
- Complete provider documentation
- Practical workflows (single image, batch generation, CI/CD)
- Security best practices for credentials
- Troubleshooting section with common errors
- AI agent guidance with rules and best practices

---

## 📦 Packages & Versions

All packages updated to **v0.16.0**:

| Package | NuGet Link | Purpose |
|---------|-----------|---------|
| **ElBruno.Text2Image** | [NuGet](https://www.nuget.org/packages/ElBruno.Text2Image/0.16.0) | Core library with IImageGenerator interface |
| **ElBruno.Text2Image.Foundry** | [NuGet](https://www.nuget.org/packages/ElBruno.Text2Image.Foundry/0.16.0) | Microsoft Foundry cloud support (FLUX.2, MAI-Image-2) |
| **ElBruno.Text2Image.Cli** | [NuGet](https://www.nuget.org/packages/ElBruno.Text2Image.Cli/0.16.0) | Command-line tool with skill integration |
| **ElBruno.Text2Image.Acceleration.Cpu** | [NuGet](https://www.nuget.org/packages/ElBruno.Text2Image.Acceleration.Cpu/0.16.0) | CPU acceleration (ONNX Runtime) |
| **ElBruno.Text2Image.Acceleration.Cuda** | [NuGet](https://www.nuget.org/packages/ElBruno.Text2Image.Acceleration.Cuda/0.16.0) | NVIDIA CUDA acceleration |
| **ElBruno.Text2Image.Acceleration.DirectML** | [NuGet](https://www.nuget.org/packages/ElBruno.Text2Image.Acceleration.DirectML/0.16.0) | Windows DirectML acceleration |

---

## 🧪 Testing & Quality

✅ **All tests passing:**
- **298 tests** on .NET 8.0
- **385 tests** on .NET 10.0
- **683 total passing tests** (0 failures)
- **14 integration tests** skipped (require external credentials — expected)

✅ **Zero build warnings** maintained

✅ **Test Coverage:**
- InitCommand: Complete coverage for skill file creation, all target platforms, force overwrites, edge cases
- CLI command surface: All commands and flags validated
- Image generators: Comprehensive unit tests for all models
- Credential management: Multiple storage backends tested

---

## 🚀 How to Use v0.16.0

### Installation

```bash
# Install or upgrade the CLI
dotnet tool install --global ElBruno.Text2Image.Cli
# or
dotnet tool update --global ElBruno.Text2Image.Cli
```

### Enable Skill Discovery

```bash
# In your repository root, run:
t2i init

# This creates:
# ✓ .github/skills/t2i/SKILL.md (GitHub Copilot)
# ✓ .claude/skills/t2i/SKILL.md (Claude Code)
```

### Commit to Your Repository

```bash
git add .github/skills/ .claude/skills/
git commit -m "feat: add t2i AI agent skills"
git push
```

### Use with AI Agents

#### GitHub Copilot:
```bash
copilot "Generate a logo for my project and save it as logo.png"
```

#### Claude Code (in chat):
```
Generate 3 test images for my gallery app with different styles
```

### For Library Usage (Developers)

```bash
dotnet add package ElBruno.Text2Image.Foundry
```

```csharp
// Cloud-based image generation
var generator = new Flux2Generator(endpoint, apiKey, "FLUX.2-pro", "flux-2-pro");
var image = await generator.GenerateImageAsync("a robot painting", cancellationToken);
```

---

## 🔄 Migration from v0.15.0

✅ **Fully backward compatible.** No breaking changes.

**To upgrade:**

1. Update the CLI:
   ```bash
   dotnet tool update --global ElBruno.Text2Image.Cli
   ```

2. Enable skills in your repository (optional but recommended):
   ```bash
   t2i init
   ```

3. Update NuGet packages in your projects:
   ```bash
   dotnet add package ElBruno.Text2Image.Foundry --version 0.16.0
   ```

---

## 📋 What's Included

### CLI Commands
- `t2i <prompt>` — Generate image from text prompt
- `t2i init` — **NEW**: Install AI agent skills (GitHub Copilot, Claude Code)
- `t2i config` — Interactive setup and configuration
- `t2i doctor` — Diagnostic tool (system info, connectivity, credentials)
- `t2i providers` — List available image generation providers
- `t2i secrets` — Manage credentials securely
- `t2i version` — Show version and git SHA

### Supported Models

**Local (ONNX Runtime):**
- Stable Diffusion 1.5, 2.1
- SDXL Turbo
- LCM Dreamshaper

**Cloud (Microsoft Foundry):**
- FLUX.2 Pro (photorealistic)
- FLUX.2 Flex (text-heavy design)
- MAI-Image-2 (high quality, fast)

**Cloud (Azure OpenAI):**
- GPT-Image-1.5 (DALL-E 3)
- GPT-Image-2 (latest generation)

---

## 📚 Documentation

- **README.md** — Project overview and quick-start
- **docs/cli-tool.md** — Command reference and examples
- **docs/skill-integration.md** — Complete skill setup guide (GitHub Copilot & Claude Code)
- **docs/gpu-acceleration.md** — GPU setup and optimization
- **docs/security.md** — Credential management and security best practices
- **docs/model-support.md** — Feature matrix for all models
- **docs/architecture.md** — System design and patterns

---

## 🐛 Known Issues

- **14 integration tests skipped** — Require Azure deployment credentials (not a blocker; unit tests fully cover functionality)
- **Framework-specific tests on .NET 10 preview** — May show warnings but do not affect functionality

---

## 🙏 Contributors

Built with ❤️ by Bruno Capuano and the open-source community.

---

## 🔗 Links

- **GitHub Repository:** https://github.com/elbruno/ElBruno.Text2Image
- **NuGet Packages:** https://www.nuget.org/packages?q=ElBruno.Text2Image
- **Report Issues:** https://github.com/elbruno/ElBruno.Text2Image/issues
- **Discussions:** https://github.com/elbruno/ElBruno.Text2Image/discussions

---

## 🎯 What's Next

Planned for future releases:
- Extended AI agent integration (additional platforms)
- Performance optimizations for batch image generation
- Enhanced model fine-tuning capabilities
- Expanded provider ecosystem

---

**Thank you for using t2i! 🚀**
