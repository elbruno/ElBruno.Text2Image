# Skill Integration Guide

This guide explains how to integrate the `t2i` CLI as a discoverable skill for AI coding agents like GitHub Copilot and Claude Code.

## What are Skills?

Skills are packages of functionality that AI agents can discover and invoke autonomously. By exposing `t2i` as a skill, you enable AI coding assistants to:
- Generate images directly within your development workflow
- Automate image creation based on natural language prompts
- Integrate image generation into CI/CD pipelines and automation scripts

Skills work by installing metadata files in well-known directories that AI agents scan during initialization. These files describe the tool's capabilities, usage patterns, and command syntax.

## Why t2i Ships a Skill

The `t2i` CLI is designed for both human developers and AI agents. By shipping skill metadata, we make it easy for agents to:
1. **Discover** the tool's existence without manual configuration
2. **Understand** its capabilities through structured documentation
3. **Invoke** it correctly with appropriate parameters

This enables seamless integration into AI-assisted workflows, from interactive coding sessions to automated build pipelines.

## Getting Started with `t2i init`

The `t2i init` command installs skill metadata files into your current workspace. It supports two AI platforms:

| Platform | Target Flag | Installation Directory |
|----------|-------------|------------------------|
| **GitHub Copilot** | `--target github` | `.github/skills/t2i/` |
| **Claude Code** | `--target claude` | `.claude/skills/t2i/` |

### Basic Usage

```bash
# Install for all supported agents (default)
t2i init

# Install for GitHub Copilot only
t2i init --target github

# Install for Claude Code only
t2i init --target claude
```

The command creates the following files:

**GitHub Copilot:**
- `.github/skills/t2i/SKILL.md` — Human-readable documentation
- `.github/skills/t2i/skill.json` — Machine-readable manifest

**Claude Code:**
- `.claude/skills/t2i/SKILL.md` — Skill documentation

These files are automatically generated from the CLI's embedded resources and contain:
- Tool overview and capabilities
- Command syntax and examples
- Provider configuration instructions
- Best practices and troubleshooting tips

### When to Run `t2i init`

Run `t2i init` in the following scenarios:

1. **First-time setup**: After installing `t2i` for the first time
2. **New workspace**: When starting a new project that needs image generation
3. **After updates**: When you update the `t2i` CLI to refresh skill metadata
4. **Multi-agent environments**: To ensure all AI agents can discover the tool

## For GitHub Copilot Users

GitHub Copilot scans `.github/skills/` directories at workspace startup. Once `t2i` is initialized, Copilot can autonomously invoke the tool when you request image generation.

### Installation

```bash
# From your workspace root
t2i init --target github
```

This creates:
```
.github/
└── skills/
    └── t2i/
        ├── SKILL.md
        └── skill.json
```

### Example Interactions

After initialization, you can interact with Copilot naturally:

**User:** "Generate a futuristic cityscape image with neon lights"

**Copilot:** *Automatically invokes:*
```bash
t2i "a futuristic cityscape with neon lights, cyberpunk style" --provider foundry-flux2 --width 1024 --height 1024
```

**User:** "Create a minimalist logo design and save it as logo.png"

**Copilot:** *Automatically invokes:*
```bash
t2i "minimalist logo design, geometric shapes, modern" --out logo.png
```

### Configuration

Copilot respects your `t2i` configuration. Before using the skill, ensure you've configured at least one provider:

```bash
# Configure interactively
t2i config

# Or set up manually
t2i secrets set foundry-flux2
```

### Troubleshooting

**Copilot doesn't recognize the skill:**
1. Verify files exist: `ls .github/skills/t2i/`
2. Restart your IDE or GitHub Copilot extension
3. Check that you're in the correct workspace directory
4. Re-run `t2i init --target github`

**Skill invocations fail:**
1. Verify `t2i` is installed: `t2i --version`
2. Test manually: `t2i "test prompt"`
3. Check provider configuration: `t2i config show`
4. Run diagnostics: `t2i doctor`

## For Claude Code Users

Claude Code scans `.claude/skills/` directories to discover available tools. The skill integration enables Claude to understand `t2i`'s capabilities and invoke it correctly.

### Installation

```bash
# From your workspace root
t2i init --target claude
```

This creates:
```
.claude/
└── skills/
    └── t2i/
        └── SKILL.md
```

### Example Interactions

After initialization, Claude can invoke `t2i` based on your requests:

**User:** "I need an image of a sunset over mountains for the landing page"

**Claude:** *Automatically invokes:*
```bash
t2i "sunset over mountains, warm colors, panoramic view" --provider foundry-flux2 --width 1792 --height 1024 --out landing-hero.png
```

**User:** "Generate a series of icons: home, settings, and profile"

**Claude:** *Automatically invokes multiple commands:*
```bash
t2i "home icon, simple line art, 512x512" --out icon-home.png
t2i "settings icon, simple line art, 512x512" --out icon-settings.png
t2i "profile icon, simple line art, 512x512" --out icon-profile.png
```

### Configuration

Ensure `t2i` is configured before use:

```bash
# Interactive setup
t2i config

# Or configure manually
t2i config set default-provider foundry-flux2
t2i secrets set foundry-flux2
```

### Troubleshooting

**Claude doesn't recognize the skill:**
1. Verify the file exists: `ls .claude/skills/t2i/`
2. Restart Claude or your IDE
3. Check you're in the correct workspace
4. Re-run `t2i init --target claude`

**Skill invocations fail:**
1. Verify `t2i` is installed: `t2i --version`
2. Test manually: `t2i "test prompt"`
3. Check provider configuration: `t2i config show`
4. Run diagnostics: `t2i doctor`

## Updating the Skill

When you update the `t2i` CLI tool, the skill metadata may also change (new features, updated documentation, etc.). To refresh the skill files:

```bash
# Update the CLI tool
dotnet tool update --global ElBruno.Text2Image.Cli

# Refresh skill metadata
t2i init
```

This overwrites existing skill files with the latest version. Your `t2i` configuration (providers, secrets) is **not** affected — only the skill metadata files are regenerated.

### Manual Refresh Workflow

If you want to update skills for a specific platform:

```bash
# Update GitHub Copilot skill only
dotnet tool update --global ElBruno.Text2Image.Cli
t2i init --target github

# Update Claude Code skill only
dotnet tool update --global ElBruno.Text2Image.Cli
t2i init --target claude
```

### Version Tracking

The skill files include version metadata to help you track which `t2i` version they were generated from. To check:

```bash
# View GitHub Copilot skill metadata
cat .github/skills/t2i/skill.json

# View Claude Code skill metadata
cat .claude/skills/t2i/SKILL.md
```

## Best Practices

### 1. Commit Skill Files to Git

Include skill files in your repository so team members and CI/CD systems can benefit from agent-assisted workflows:

```bash
git add .github/skills/t2i/
git add .claude/skills/t2i/
git commit -m "feat: add t2i skill integration for AI agents"
```

### 2. Initialize Skills Early

Run `t2i init` as part of your project setup:

```bash
# After cloning a repository
git clone https://github.com/yourorg/project.git
cd project
dotnet tool install --global ElBruno.Text2Image.Cli
t2i init
t2i config
```

### 3. Document Provider Configuration

If your team uses a specific cloud provider, document it in your README:

```markdown
## Image Generation Setup

This project uses `t2i` for AI image generation. To set up:

1. Install: `dotnet tool install --global ElBruno.Text2Image.Cli`
2. Initialize: `t2i init`
3. Configure: `t2i config`
4. Use Microsoft Foundry FLUX.2 for cloud generation
```

### 4. Use Environment Variables in CI/CD

For automated workflows, configure `t2i` using environment variables:

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

### 5. Version Pin in Production

Pin the `t2i` CLI version for reproducible builds:

```bash
# Install specific version
dotnet tool install --global ElBruno.Text2Image.Cli --version 0.16.0

# Or use a global.json manifest
dotnet new tool-manifest
dotnet tool install ElBruno.Text2Image.Cli --version 0.16.0
```

## Advanced: Custom Skills

If you're building custom AI agents or extending existing platforms, you can adapt the skill metadata format:

### GitHub Copilot Skill Manifest

```json
{
  "name": "t2i",
  "version": "0.16.0",
  "description": "Text-to-image generation CLI",
  "command": "t2i",
  "capabilities": [
    "text-to-image",
    "stable-diffusion",
    "flux2",
    "cloud-generation"
  ],
  "examples": [
    {
      "prompt": "a sunset over mountains",
      "command": "t2i \"a sunset over mountains\" --provider foundry-flux2"
    }
  ]
}
```

### Claude Code Skill Documentation

The `SKILL.md` format follows a standard structure:
1. **Overview**: What the tool does
2. **Capabilities**: List of supported features
3. **Usage**: Command syntax and examples
4. **Configuration**: How to set up providers
5. **Troubleshooting**: Common issues and solutions

You can adapt this format for other AI platforms by following the same pattern.

## Troubleshooting

### "Permission denied" during `t2i init`

**Cause:** Insufficient write permissions in the workspace directory.

**Solution:**
```bash
# Check permissions
ls -la .github/skills/ .claude/skills/

# Fix permissions (Linux/macOS)
chmod -R u+w .github/ .claude/

# Run init again
t2i init
```

### Skill files not generated

**Cause:** `t2i init` may have failed silently or been interrupted.

**Solution:**
```bash
# Remove existing partial files
rm -rf .github/skills/t2i/
rm -rf .claude/skills/t2i/

# Re-run init
t2i init
```

### AI agent doesn't use the skill

**Possible causes:**
1. Skill files not in the expected directory
2. AI agent hasn't reloaded the workspace
3. Agent isn't configured to use skills

**Solutions:**
- Verify file locations match platform expectations
- Restart your IDE or AI agent extension
- Check agent settings for skill discovery options
- Consult your AI platform's documentation for skill configuration

### Version mismatch warnings

**Cause:** Skill metadata was generated by an older `t2i` version.

**Solution:**
```bash
# Update CLI
dotnet tool update --global ElBruno.Text2Image.Cli

# Regenerate skills
t2i init
```

## FAQ

**Q: Do I need to run `t2i init` on every machine?**  
A: No. If you commit skill files to Git, they're shared with your team. Each developer only needs to install the `t2i` CLI and configure their providers.

**Q: Can I customize the skill files?**  
A: Yes, but changes will be overwritten when you run `t2i init` again. For permanent customization, fork the `t2i` source and modify the embedded skill templates.

**Q: Do skills work offline?**  
A: Skills work offline if you use local providers (cpu, cuda, directml). Cloud providers (foundry-flux2, foundry-mai2) require internet connectivity.

**Q: Are skill files platform-specific?**  
A: Yes. GitHub Copilot and Claude Code use different directory structures and metadata formats. Use `t2i init` without flags to install for both platforms.

**Q: How do I disable skill integration?**  
A: Remove the skill directories:
```bash
rm -rf .github/skills/t2i/
rm -rf .claude/skills/t2i/
```

**Q: Can I use `t2i` without initializing skills?**  
A: Absolutely. Skills are optional. You can use `t2i` manually or in scripts without AI agent integration.

## Next Steps

- **Configure providers**: [docs/cli-tool.md](cli-tool.md)
- **FLUX.2 setup**: [docs/flux2-setup-guide.md](flux2-setup-guide.md)
- **MAI-Image-2 setup**: [docs/mai-image-2-setup-guide.md](mai-image-2-setup-guide.md)
- **CLI reference**: [docs/cli-tool.md](cli-tool.md)

## Contributing

Found an issue or have suggestions for improving skill integration? Open an issue or PR on [GitHub](https://github.com/elbruno/ElBruno.Text2Image).

## License

MIT — see [LICENSE](../LICENSE) for details.
