# t2i — Text-to-Image CLI

Cross-platform command-line tool for text-to-image generation using ElBruno.Text2Image. Supports local CPU/GPU inference and cloud providers.

## Editions

**🌟 Lite (this package):** Cloud providers only. ~30 MB. Ideal for CI/CD, containers, and cloud-first workflows.

**🚀 Full (planned for v0.2.0):** Adds local CPU/CUDA/DirectML inference via ONNX Runtime. ~200 MB.

This documentation describes the **Lite edition**.

## What is t2i?

`t2i` is a .NET global tool that makes AI image generation simple and accessible from the command line. It wraps the ElBruno.Text2Image library and provides:

- **Local inference**: CPU, CUDA, DirectML (no internet required)
- **Cloud providers**: Microsoft Foundry (FLUX.2 Pro, MAI-Image-2)
- **Unified interface**: Same command syntax for all providers
- **Secure secret management**: DPAPI (Windows), environment variables, opt-in plaintext
- **Interactive setup**: First-run wizard guides you through configuration

## Installation

Install as a .NET global tool:

```bash
dotnet tool install --global ElBruno.Text2Image.Cli
```

Verify installation:

```bash
t2i --version
```

## Quick Start

### First Run — Interactive Setup

On first run, `t2i` launches an interactive wizard to configure your preferred provider:

```bash
t2i config
```

The wizard will:
1. Detect available local providers (CPU, CUDA, DirectML)
2. Prompt for cloud provider credentials (optional)
3. Set a default provider
4. Save configuration securely

### Generate Your First Image

```bash
t2i "a cat sitting on a windowsill"
```

This uses your configured default provider. The image is saved as `output.png`.

### Specify a Provider

```bash
# Use local CPU
t2i "a mountain landscape" --provider cpu

# Use cloud provider
t2i "a futuristic city" --provider foundry-flux2 --width 1024 --height 1024
```

### Custom Output Path

```bash
t2i "a sunset over the ocean" --out sunset.png
```

## Command Reference

| Command | Description | Example |
|---------|-------------|---------|
| `t2i "<prompt>"` | Generate image (default command) | `t2i "a cat"` |
| `t2i generate "<prompt>"` | Generate image (explicit) | `t2i generate "a dog" --provider cuda` |
| `t2i config` | Interactive configuration wizard | `t2i config` |
| `t2i config show` | Display current config (masked secrets) | `t2i config show` |
| `t2i config set <key> <value>` | Set config value | `t2i config set default-provider cpu` |
| `t2i config path` | Show config file path | `t2i config path` |
| `t2i config remove <provider>` | Remove provider config | `t2i config remove foundry-flux2` |
| `t2i secrets set <provider>` | Set provider secrets (interactive) | `t2i secrets set foundry-flux2` |
| `t2i secrets list` | List configured providers | `t2i secrets list` |
| `t2i secrets test <provider>` | Test provider connectivity | `t2i secrets test foundry-flux2` |
| `t2i secrets remove <provider>` | Remove provider secrets | `t2i secrets remove foundry-flux2` |
| `t2i doctor` | Run system diagnostics | `t2i doctor` |
| `t2i providers` | List available providers | `t2i providers` |
| `t2i version` | Show version information | `t2i version` |

### Generate Command Options

```bash
t2i "<prompt>" [options]
```

| Option | Description | Default |
|--------|-------------|---------|
| `--provider` | Provider ID (cpu, cuda, directml, foundry-flux2, foundry-mai2) | Config default |
| `--out`, `-o` | Output file path | `output.png` |
| `--width`, `-w` | Image width in pixels | 512 (local), 1024 (cloud) |
| `--height`, `-h` | Image height in pixels | 512 (local), 1024 (cloud) |
| `--steps`, `-s` | Inference steps (local only) | 20 |
| `--endpoint` | Cloud endpoint URL (override config) | From config |
| `--api-key` | Cloud API key (override secrets) | From secrets |

## Providers

**Note:** The Lite edition includes cloud providers only. Local CPU/GPU providers (cpu, cuda, directml) will be available in the **Full edition** (v0.2.0).

| Provider ID | Name | Type | Requirements |
|-------------|------|------|--------------|
| `foundry-flux2` | FLUX.2 Pro (Cloud) | Cloud | Microsoft Foundry endpoint + API key |
| `foundry-mai2` | MAI-Image-2 (Cloud) | Cloud | Microsoft Foundry endpoint + API key |

### Cloud Providers

Cloud providers require a Microsoft Foundry deployment. See [Microsoft Foundry documentation](https://learn.microsoft.com/azure/ai-services/foundry/) to create a deployment.

You'll need:
- **Endpoint URL**: Your Foundry resource URL (e.g., `https://myresource.services.ai.azure.com`)
- **API Key**: From your Foundry resource's "Keys and Endpoint" page

## Secret Resolution Chain

When a cloud provider needs credentials, `t2i` checks these sources **in order**:

1. **CLI flags**: `--endpoint`, `--api-key` (highest priority)
2. **Environment variables**: `T2I_<PROVIDER>_<FIELD>` (e.g., `T2I_FOUNDRY_FLUX2_APIKEY`)
3. **OS native store**: DPAPI on Windows (encrypted), plaintext file elsewhere (opt-in)
4. **Config file**: `config.json` in your config directory (plaintext, opt-in)
5. **Interactive wizard**: If running in a TTY, prompts interactively

**Security Note**: On Windows, secrets are encrypted using DPAPI. On Linux/macOS, secrets are stored in plaintext by default (future versions will support Keychain/libsecret).

### Setting Secrets

```bash
# Interactive (recommended)
t2i secrets set foundry-flux2

# Environment variable (session-scoped)
export T2I_FOUNDRY_FLUX2_ENDPOINT="https://myresource.services.ai.azure.com"
export T2I_FOUNDRY_FLUX2_APIKEY="your-api-key"

# CLI flag (command-scoped, not persisted)
t2i "a cat" --provider foundry-flux2 \
  --endpoint "https://myresource.services.ai.azure.com" \
  --api-key "your-api-key"
```

## Configuration Files

Config files are stored at platform-specific locations:

- **Windows**: `%APPDATA%\t2i\config.json`
- **Linux/macOS**: `$XDG_CONFIG_HOME/t2i/config.json` or `~/.config/t2i/config.json`

To view your config path:

```bash
t2i config path
```

### Example config.json

```json
{
  "defaultProvider": "cpu",
  "providers": {
    "foundry-flux2": {
      "endpoint": "https://myresource.services.ai.azure.com",
      "model": "FLUX.2-pro",
      "extras": {}
    }
  }
}
```

## First-Run Wizard

When you run `t2i config` for the first time, an interactive wizard guides you through setup:

1. **Provider Selection**: Choose your preferred cloud provider (foundry-flux2 or foundry-mai2)
2. **Cloud Setup**: Enter endpoint and API key for your selected provider
3. **Secret Storage**: Choose where to store secrets (DPAPI on Windows, file elsewhere)
4. **Test**: Validates configuration with a quick health check

The wizard is non-destructive — you can re-run it to update settings.

## Diagnostics

Run diagnostics to check system health:

```bash
t2i doctor
```

Checks:
- Config file validity
- Secret store accessibility
- Provider connectivity (cloud)

## Examples

### Basic Usage

```bash
# Generate with default provider
t2i "a cat"

# Specify provider
t2i "a dog" --provider foundry-flux2

# Custom dimensions
t2i "a landscape" --width 768 --height 512

# Custom output path
t2i "abstract art" --out artwork.png
```

### Cloud Provider

```bash
# High-resolution with FLUX.2 Pro
t2i "a photorealistic portrait" \
  --provider foundry-flux2 \
  --width 1024 \
  --height 1024

# MAI-Image-2
t2i "a technical diagram" \
  --provider foundry-mai2 \
  --width 1024 \
  --height 1024
```

### Configuration Management

```bash
# Set default provider
t2i config set default-provider foundry-flux2

# View config path
t2i config path

# Show current config (secrets masked)
t2i config show

# Remove a provider
t2i config remove foundry-flux2
```

### Secret Management

```bash
# Set secrets interactively (recommended)
t2i secrets set foundry-flux2

# List configured providers
t2i secrets list

# Test provider connectivity
t2i secrets test foundry-flux2

# Remove secrets
t2i secrets remove foundry-flux2
```

## Roadmap

### Phase 1: .NET Global Tool (Current)
✅ `dotnet tool install -g ElBruno.Text2Image.Cli`  
✅ Cross-platform (Windows, Linux, macOS)  
✅ Requires .NET 10 SDK installed

### Phase 2: Self-Contained Binaries
- Per-platform executables (no SDK required)
- `win-x64`, `linux-x64`, `osx-arm64`
- Single-file publish with ReadyToRun

### Phase 3: winget (Windows Package Manager)
- One-command install: `winget install ElBruno.Text2Image`
- Auto-updates via winget

### Phase 4: Homebrew (macOS)
- One-command install: `brew install elbruno/tap/t2i`
- Auto-updates via Homebrew

### Phase 5: Enhanced Security
- macOS Keychain integration
- Linux libsecret integration
- Azure Key Vault backend (optional)

## Troubleshooting

### "Command not found: t2i"

After installing, restart your terminal or run:

```bash
# Refresh PATH (or restart terminal)
export PATH="$PATH:$HOME/.dotnet/tools"  # Linux/macOS
# OR add %USERPROFILE%\.dotnet\tools to PATH (Windows)
```

### "No default provider configured"

Run the first-time setup wizard:

```bash
t2i config
```

### "Provider not available"

Check provider status:

```bash
t2i providers
```

Run diagnostics:

```bash
t2i doctor
```

### Cloud Provider Issues

Test connectivity:

```bash
t2i secrets test foundry-flux2
```

Verify secrets are set:

```bash
t2i secrets list
```

## Design Decisions

**Why Spectre.Console.Cli?**  
We chose `Spectre.Console.Cli` over `System.CommandLine` for its stability, rich TUI rendering, and seamless integration with Spectre.Console's tables, prompts, and progress bars.

**Why net10.0 only?**  
CLI tools benefit from a single target framework — simplifies packaging, reduces publish size, and ensures consistent runtime behavior. Libraries remain multi-targeted (net8.0;net10.0).

**Why DPAPI on Windows?**  
DPAPI (Data Protection API) encrypts secrets per-user using Windows credentials — no master password needed. Linux/macOS support for Keychain/libsecret is planned for Phase 5.

## Contributing

This tool is part of the [ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image) project. Contributions welcome!

## License

MIT — see [LICENSE](../LICENSE) for details.
