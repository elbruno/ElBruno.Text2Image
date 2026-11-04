---
name: t2i
description: >-
  Generate images with the t2i CLI using Microsoft Foundry or Azure OpenAI.
  Use when a user requests image generation, batch image automation, CI image
  generation, or help configuring and troubleshooting t2i.
license: MIT
compatibility: Requires the ElBruno.Text2Image.Cli .NET tool and a configured cloud provider.
metadata:
  author: Bruno Capuano
---

<!-- t2i:managed-skill -->

# t2i CLI skill

Use the `t2i` command-line tool to generate images from text prompts. The tool
is a .NET global tool with Microsoft Foundry and Azure OpenAI providers.

## Before generating

1. Check whether `t2i` is installed. If not, ask before installing software:

   ```sh
   dotnet tool install --global ElBruno.Text2Image.Cli
   ```

2. Check configuration with `t2i doctor` or `t2i providers`. If no provider is
   configured, guide the user through `t2i config` and `t2i secrets set
   <provider>`. Never ask the user to paste a key into chat.
3. Use a configured provider, or pass `--provider <id>` explicitly.
4. For scripts, always pass `--out <path>` so the output filename is stable.

## Generate an image

```sh
t2i "a red fox in an autumn forest" --provider foundry-mai25 --out fox.png
```

The generation command is the default command; there is no `generate`
subcommand. `--out` (or `-o`) selects the PNG path. Without it, the tool
creates a timestamped filename from the prompt.

Use `--width`, `--height`, and `--steps` when the selected provider supports
those options. See [provider selection](references/providers.md) for available
provider IDs and guidance.

## Common tasks

- **Configure or diagnose:** `t2i config`, `t2i providers`, `t2i doctor`.
- **Generate a single image:** `t2i "<prompt>" --out image.png`.
- **Automate generation:** pass explicit output paths and use CI secret storage.
  See [automation and troubleshooting](references/automation-and-troubleshooting.md).
- **Protect credentials:** use `t2i secrets set <provider>` locally or
  environment variables from a secret manager in automation. Never put secrets
  in source control, prompts, logs, or generated skill files. See
  [configuration and security](references/configuration-and-security.md).

## Skill setup

`t2i init` creates or updates the skill in the current repository for GitHub
Copilot and Claude Code. Use `--target github` or `--target claude` for a
single target. `t2i init --keep-existing` preserves files already present.
After installing a newer tool version, `t2i upgrade` refreshes existing
t2i-managed skills without creating missing targets or changing unrelated
skills.

Installing this NuGet tool does not automatically activate this skill in an
agent. To use it from a project, run `t2i init` or use a compatible NuGet
package-skill discovery tool to inspect and install the packaged skill.

## Safety

- Ask before installing the global tool or running image-generation requests
  that incur a cost or use a user's configured cloud account.
- Never display or expose API keys. Do not use the `--api-key` option in
  scripts; use the secret store or environment variables instead.
- Treat prompts and generated files as user-controlled data. Do not overwrite
  user files unless the user requested it.
- Do not configure retired provider IDs. For current choices, see
  [provider selection](references/providers.md).
