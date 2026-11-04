# t2i skill integration

`t2i init` installs the CLI skill for GitHub Copilot and Claude Code by writing
the skill and its reference files to their workspace skill directories.
The repository's portable skill strategy uses `.github/skills/t2i/` and
`.claude/skills/t2i/`; `.agents/skills/` is not generated because this project
does not have an established `.agents` consumer. The embedded CLI skill is the
authoritative source for both managed copies and the CLI NuGet package.

The CLI NuGet package also contains the skill under `skills/t2i/`. Package
restore makes those files available in the NuGet cache, but does not
automatically activate the skill in an agent. To inspect the packaged skill
directly, use a compatible package inspection tool, for example:

```bash
dotnet-inspect package ElBruno.Text2Image.Cli -S Skills
```

The project dependency-graph workflow is intended for package references. The
CLI is normally installed as a global tool, so inspect its package directly
rather than assuming it appears in an application's restored dependency graph.
Review package skill instructions before using them, and install packages only
from sources and maintainers you trust.

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
| GitHub Copilot | `.github/skills/t2i/SKILL.md` and `references/*.md` |
| Claude Code | `.claude/skills/t2i/SKILL.md` and `references/*.md` |

There is no `skill.json` manifest. Commit the generated skill directory when
the workspace should share image-generation guidance with other contributors
and agents.

## Refresh safely

`init` creates or replaces the selected files. To preserve an existing skill file, use:

```bash
t2i init --keep-existing
```

To refresh an installed, managed t2i skill, use:

```bash
t2i upgrade
t2i upgrade --target github
```

Use `upgrade` after updating the global tool when you want the latest skill and
reference files. It does not create a skill for a target that is not already
installed. Missing packaged references are restored only within an existing
managed t2i skill directory; unrelated or user-owned files are preserved.

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
