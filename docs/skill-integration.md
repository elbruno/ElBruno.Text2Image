# t2i skill integration

`t2i init` makes the CLI discoverable to GitHub Copilot and Claude Code by writing the same `SKILL.md` guidance to their workspace skill directories.

## Install

From the repository or workspace root:

```bash
# Both supported targets
t2i init

# One target
t2i init --target github
t2i init --target claude
```

| Target | File created |
|---|---|
| GitHub Copilot | `.github/skills/t2i/SKILL.md` |
| Claude Code | `.claude/skills/t2i/SKILL.md` |

There is no `skill.json` manifest. Commit the generated `SKILL.md` files when the workspace should share image-generation guidance with other contributors and agents.

## Refresh safely

`init` creates or replaces the selected files. To preserve an existing skill file, use:

```bash
t2i init --keep-existing
```

To refresh only skill files that already exist, use:

```bash
t2i upgrade
t2i upgrade --target github
```

Use `upgrade` after updating the global tool when you want the latest embedded guidance without creating a skill for another agent platform.

## Configure before generation

Skill files describe commands; they do not include credentials. Configure a provider separately:

```bash
t2i config
t2i doctor
```

For automation, inject `T2I_<PROVIDER>_APIKEY` through the CI platform's secret mechanism. Never put keys in a generated skill file, source file, or command history.

```yaml
- name: Generate documentation image
  env:
    T2I_FOUNDRY_FLUX2_APIKEY: ${{ secrets.FOUNDRY_API_KEY }}
  run: t2i "developer workflow illustration" --provider foundry-flux2 --out docs/workflow.png
```

Use `--out`, not `--output`, for predictable file names. For current providers and commands, see the [CLI guide](cli-tool.md).
