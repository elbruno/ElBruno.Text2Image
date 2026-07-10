# t2i CLI

`t2i` is a cross-platform .NET global tool for cloud image generation. The Lite package includes Microsoft Foundry and Azure OpenAI providers; it does not include local ONNX inference.

## Install

```bash
dotnet tool install --global ElBruno.Text2Image.Cli
t2i version
```

Update with either `dotnet tool update --global ElBruno.Text2Image.Cli` or `t2i update`.

## Providers

| Provider ID | Default model | Service |
|---|---|---|
| `foundry-flux2` | `FLUX.2-pro` | Microsoft Foundry |
| `foundry-mai2` | `MAI-Image-2` | Microsoft Foundry |
| `foundry-mai25` | `MAI-Image-2.5` | Microsoft Foundry |
| `foundry-mai25-flash` | `MAI-Image-2.5-Flash` | Microsoft Foundry |
| `foundry-gpt-image-1p5` | `gpt-image-1.5` | Azure OpenAI |
| `foundry-gpt-image-2` | `gpt-image-2` | Azure OpenAI |

Run `t2i providers` to see the providers in the installed tool and their configuration status.

## Configure a provider

Use the interactive setup wizard:

```bash
t2i config
```

Or configure a provider explicitly:

```bash
t2i config set foundry-flux2.endpoint "https://your-resource.services.ai.azure.com"
t2i config set foundry-flux2.model "FLUX.2-pro"
t2i secrets set foundry-flux2
t2i config set default-provider foundry-flux2
```

`endpoint` and `model` are configuration fields. `apiKey` is a secret; `t2i config set <provider>.apiKey <value>` stores it in the configured secret store, but `t2i secrets set <provider>` avoids exposing it in shell history.

Use `t2i config show` to inspect configuration (secrets are masked) and `t2i config path` to print the configuration-file location.

## Generate an image

```bash
# Uses the configured default provider.
t2i "a robot painting a landscape" --out robot.png

# Use a provider explicitly.
t2i "a product landing page with readable headline text" --provider foundry-flux2 --width 1024 --height 1024 --out landing-page.png

# GPT-Image-2 can take several minutes.
t2i "a space station in orbit" --provider foundry-gpt-image-2 --timeout 300 --out station.png
```

The generation command is the default command: there is no `t2i generate` subcommand. `--out` (or `-o`) sets the output path. Without it, `t2i` creates a timestamped PNG name from the prompt.

| Option | Description | Default |
|---|---|---|
| `--provider` | Provider ID | Configured default provider |
| `--out`, `-o` | PNG output path | Prompt-derived timestamped name |
| `--width`, `-w` | Requested image width | 512 |
| `--height` | Requested image height | 512 |
| `--steps`, `-s` | Requested inference steps | 20 |
| `--timeout` | Request timeout in seconds | 300 |
| `--endpoint` | Listed endpoint override; configure the endpoint with `t2i config set` for reliable use | Configured endpoint |
| `--api-key` | One-command API-key override | Resolved secret |

Provider APIs can constrain or adjust image dimensions. In particular, MAI Image models require each dimension to be at least 768 pixels and the total must not exceed 1,048,576 pixels. See [model support](model-support.md).

## Secrets and environment variables

The CLI resolves an API key in this order:

1. `--api-key`
2. `T2I_<PROVIDER>_APIKEY`
3. Local secret storage

For example:

```powershell
$env:T2I_FOUNDRY_FLUX2_APIKEY = "<api-key>"
t2i "a poster for a developer conference" --provider foundry-flux2 --out poster.png
```

Provider names use underscores in environment variables, so the GPT-Image-2 key is `T2I_FOUNDRY_GPT_IMAGE_2_APIKEY`.

On Windows, locally stored secrets use DPAPI; plaintext fallback is intentionally blocked. On Linux and macOS, they are stored in a user-owned plaintext file. Use environment variables or your CI system's secret store for unattended runs.

## Diagnostics

```bash
t2i doctor
t2i secrets list
t2i secrets test foundry-mai2
```

`doctor` is informational and exits with code `0`, including when a provider is not configured. FLUX and MAI provider checks normally validate local configuration; set `T2I_DETAILED_HEALTH_CHECKS=1` to enable their detailed network checks.

## AI-agent skill files

```bash
# Create skill files for GitHub Copilot and Claude Code.
t2i init

# Create only one target.
t2i init --target github

# Refresh skill files that already exist, without creating new ones.
t2i upgrade
```

`init` writes `SKILL.md` to `.github/skills/t2i/` and/or `.claude/skills/t2i/`. It overwrites existing files unless `--keep-existing` is supplied. `upgrade` only updates files that already exist. See [skill integration](skill-integration.md).
