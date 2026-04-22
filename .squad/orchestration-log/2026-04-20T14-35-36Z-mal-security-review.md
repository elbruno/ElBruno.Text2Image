# Mal — Security Review: Environment Variable Secret Storage

**Date:** 2026-04-20T14:35:36Z  
**Agent:** Mal (Lead, Security Review)  
**Session:** Security review of CLI Secrets/ backends and blog post guidance

## Work Completed

### 1. Backend Analysis
Analyzed three secret storage backends in `SecretResolver.cs`:
- **DPAPI** (Windows): `ProtectedData.Protect(DataProtectionScope.CurrentUser)` — OS-native encryption
- **PlainFile** (Unix): `~/.t2i/secrets.json` with `0600` permissions — filesystem-protected
- **EnvVar**: `T2I_{PROVIDER}_{FIELD}` — insecure for local dev, CI/CD-only

### 2. Blog Post Review
Reviewed `docs/20260420-introducing-t2i-cli.md` "Where Do My Secrets Live?" section.

**Current order (problematic):**
1. Environment Variables (presented as "best")
2. Windows DPAPI
3. Plaintext file
4. CLI override

This ordering misrepresents env vars as a recommended default for local development.

### 3. Security Assessment
Environment variables expose secrets via:
- Process tree visibility (`/proc/<pid>/environ` on Linux)
- Shell history (`.bash_history`, `ConsoleHost_history.txt`)
- Accidental commits (added to `.bashrc`, `.zshrc`, dotfiles)
- Debug output and CI/CD logging
- Child process inheritance

**Conclusion:** Env vars are **not secure for local development**. Appropriate only for ephemeral CI/CD environments.

## Recommendation

**Reorder blog post to prioritize OS-native encrypted storage:**

1. **Local Development** — DPAPI (Windows) / File with 0600 (Unix) — FIRST
2. **CI/CD Pipelines** — Environment variables — SECOND (with explicit warnings)
3. **One-Off Tests** — CLI override — THIRD
4. **Resolution Order** — Explain priority chain

**Additions:**
- Add security best practices box (do/don't list)
- Explicit warning: "DO NOT use env vars for local development"
- Clarify that CLI already implements this correctly (no code changes needed)

## Implementation Checklist

- [ ] Edit `docs/20260420-introducing-t2i-cli.md` (reorder, add warnings)
- [ ] Edit `.github/skills/t2i/SKILL.md` (align security section)
- [ ] Edit `.claude/skills/t2i/SKILL.md` (align security section)
- [ ] Edit `docs/cli-tool.md` (align "Secret Resolution Chain")
- [ ] Consider: Add `t2i doctor` check for env-var-only secrets
- [ ] Update README.md if applicable

## Status

**Recommendation: Pending Bruno's approval**

Decision document written to `.squad/decisions/inbox/mal-secret-storage-recommendation.md`.
