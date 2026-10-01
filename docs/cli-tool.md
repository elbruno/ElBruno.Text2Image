# t2i CLI

`t2i` is a cross-platform .NET global tool for cloud image generation. The Lite package includes Microsoft Foundry and Azure OpenAI providers; it does not include local ONNX inference.

## Install

```bash
dotnet tool install --global ElBruno.Text2Image.Cli --source https://api.nuget.org/v3/index.json
t2i version
```

If the tool is already installed, use `update` rather than `install`:

```bash
dotnet tool update --global ElBruno.Text2Image.Cli --source https://api.nuget.org/v3/index.json
```

The explicit NuGet.org source is useful on machines whose NuGet configuration enables only an organizational or offline feed.
You can omit `--source` after enabling `https://api.nuget.org/v3/index.json` in your NuGet configuration.

## Providers

| Provider ID | Default model | Service |
|---|---|---|
| `foundry-flux2` | `FLUX.2-pro` | Microsoft Foundry |
| `foundry-mai2` | `MAI-Image-2` (retired) | Microsoft Foundry |
| `foundry-mai25` | `MAI-Image-2.5` | Microsoft Foundry |
| `foundry-mai25-flash` | `MAI-Image-2.5-Flash` | Microsoft Foundry |
| `foundry-gpt-image-25-sunburst` | `GPT-Image-2.5-Sunburst` | Azure OpenAI |
| `foundry-gpt-image-25-flare` | `GPT-Image-2.5-Flare` | Azure OpenAI |
| `foundry-gpt-image-1p5` | `gpt-image-1.5` | Azure OpenAI |

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

# GPT-Image-2.5-Sunburst can take several minutes.
t2i "a space station in orbit" --provider foundry-gpt-image-25-sunburst --timeout 300 --out station.png

# Image-to-image: guide generation with one or more reference images.
t2i "turn this sketch into a watercolor painting" --provider foundry-flux2 --image sketch.png --out watercolor.png
t2i "combine these products into one lifestyle shot" --provider foundry-gpt-image-2 -i bottle.png -i box.jpg --input-fidelity high --out combo.png
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
| `--image`, `-i` | Reference image (local PNG/JPEG/WebP file, HTTPS URL, or data URI). Repeat for multiple images | None |
| `--mask` | PNG mask (≤ 4 MB) for inpainting; transparent pixels mark the area to edit. Requires `--image` | None |
| `--input-fidelity` | `low` or `high` fidelity to the reference images. Requires `--image` | Provider default |

Provider APIs can constrain or adjust image dimensions. In particular, MAI Image models require each dimension to be at least 768 pixels and the total must not exceed 1,048,576 pixels. See [model support](model-support.md).

### Reference images (image-to-image)

Pass `--image` (or `-i`) one or more times to edit or guide generation from existing images. Local files are read and validated (PNG, JPEG, WebP; up to 50 MB each); HTTPS URLs and `data:` URIs are passed through. Plain `http://` URLs are rejected. Validation happens before any secret lookup or network call, so unsupported combinations fail fast with exit code 2.

| Provider | Reference images | Mask | Input fidelity | How it is sent |
|---|---|---|---|---|
| `foundry-flux2` | Up to 8 (PNG/JPEG/WebP) | — | — | `referenceImages` in the FLUX.2 request |
| `foundry-gpt-image-1p5`, `foundry-gpt-image-2`, `foundry-gpt-image-25-sunburst`, `foundry-gpt-image-25-flare` | Up to 16 (PNG/JPEG/WebP) | ✅ PNG | ✅ | Azure OpenAI `images/edits` (multipart) |
| `foundry-mai25`, `foundry-mai25-flash` | Up to 5 (PNG/JPEG) | — | — | `/mai/v1/images/edits` (multipart) |

`t2i providers` and `t2i doctor` show each provider's reference-image support. The retired `foundry-mai2` provider does not support reference images.

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

Provider names use underscores in environment variables, so the GPT-Image-2.5-Sunburst key is `T2I_FOUNDRY_GPT_IMAGE_25_SUNBURST_APIKEY`.
Configure its endpoint with `T2I_FOUNDRY_GPT_IMAGE_25_SUNBURST_ENDPOINT`; use the corresponding
`T2I_FOUNDRY_GPT_IMAGE_25_FLARE_ENDPOINT` and `T2I_FOUNDRY_GPT_IMAGE_25_FLARE_APIKEY` for Flare.

On Windows, locally stored secrets use DPAPI; plaintext fallback is intentionally blocked. On Linux and macOS, they are stored in a user-owned plaintext file. Use environment variables or your CI system's secret store for unattended runs.

## Diagnostics

```bash
t2i doctor
t2i secrets list
t2i secrets test foundry-mai25
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
