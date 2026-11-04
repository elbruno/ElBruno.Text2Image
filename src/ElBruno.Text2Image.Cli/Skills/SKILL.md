---
name: t2i
description: >-
  Generate images with the t2i CLI using Microsoft Foundry or Azure OpenAI.
  Use for image generation, reference-image editing, batch automation, CI
  image generation, and t2i configuration or troubleshooting.
license: MIT
compatibility: Requires ElBruno.Text2Image.Cli >=1.7.0 and a configured cloud provider.
metadata:
  author: Bruno Capuano
---

<!-- t2i:managed-skill -->

# t2i CLI skill

Use the `t2i` .NET global tool to generate images from text prompts with
Microsoft Foundry and Azure OpenAI providers.

## Before generating

1. Check whether `t2i` is installed. If not, ask before installing software:

   ```sh
   dotnet tool install --global ElBruno.Text2Image.Cli
   ```

2. Check setup with `t2i doctor` or `t2i providers`. If no provider is
   configured, guide the user through `t2i config` and `t2i secrets set
   <provider>`. Never ask for a key in chat.
3. Use a configured provider or pass `--provider <id>` explicitly.
4. For scripts, pass `--out <path>` so the output filename is stable.

## Generate an image

```sh
t2i "a red fox in an autumn forest" --provider foundry-mai25 --out fox.png
```

Generation is the default command, not a `generate` subcommand. `--out` (or
`-o`) selects the PNG path. Without it, the tool creates a timestamped
filename from the prompt.

Use `--width`, `--height`, and `--steps` when supported by the selected
provider. See [provider selection](references/providers.md) for available
provider IDs and model-specific guidance.

## Edit with reference images

Use the repeatable `--image` option for supported providers:

```sh
t2i "turn this sketch into a watercolor painting" \
  --provider foundry-flux2 --image sketch.png --out watercolor.png
```

Use `--mask` or `--input-fidelity` only with compatible GPT-Image providers.
MAI-Image-2.6 providers currently support text-to-image only. See
[provider selection](references/providers.md) for capabilities and limits.

## Common tasks

- **Configure or diagnose:** `t2i config`, `t2i providers`, `t2i doctor`.
- **Generate a single image:** `t2i "<prompt>" --out image.png`.
- **Automate generation:** use explicit output paths and CI secret storage.
  See [automation and troubleshooting](references/automation-and-troubleshooting.md).
- **Protect credentials:** use `t2i secrets set <provider>` locally or
  environment variables from a secret manager in automation. Never put secrets
  in source control, prompts, logs, or generated skill files. See
  [configuration and security](references/configuration-and-security.md).

## Skill setup

Run `t2i init` to install this skill and its references for GitHub Copilot and
Claude Code. Use `--target github` or `--target claude` for one target.
`t2i init --keep-existing` preserves files already present. After installing a
newer tool version, `t2i upgrade` refreshes managed skills without creating
missing targets or changing unrelated files.

The CLI NuGet package includes this skill under `skills/t2i/` for compatible
package inspection tools. Installing the tool does not automatically activate
the skill in an agent.

## Safety

- Ask before installing the global tool or running image-generation requests
  that incur a cost or use the user's configured cloud account.
- Never display or expose API keys. Do not use `--api-key` in scripts; use the
  secret store or environment variables instead.
- Treat prompts and generated files as user-controlled data. Do not overwrite
  user files unless the user requested it.
- Do not configure retired provider IDs. For current choices, see
  [provider selection](references/providers.md).
