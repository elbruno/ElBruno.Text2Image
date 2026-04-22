# Orchestration Log: MAI-Image-2 Dimension Default Bump

**Timestamp:** 2026-04-20T14:50:59Z  
**Mode:** Lightweight (Coordinator inline handling)  
**Triggered By:** Bruno Capuano  
**Issue:** CLI error when invoking `t2i "..."` without dimension flags: "MAI-Image-2 requires both dimensions to be at least 768px"

## What Was Done

**Branch:** `fix/mai-image2-default-dimensions` (from `main`)

**Files Touched:**
1. `src/ElBruno.Text2Image.Foundry/Generators/MaiImage2Generator.cs` — Auto-bump dimensions under 768px to 1024 with progress note
2. `src/ElBruno.Text2Image.Cli/ElBruno.Text2Image.Cli.csproj` — Version bump 0.10.0 → 0.10.1
3. `CHANGELOG.md` — Added entry for v0.10.1 describing dimension auto-bump fix

**Build & Test:**
- Clean build: 0 warnings, 0 errors
- Test suite: 404 passing (full suite verified)

## PRs Processed

**Merged (in order):**
1. PR #14 (`docs/blog-secrets-accuracy`) — merged with `--admin --squash`
2. PR #15 (`fix/mai-image2-default-dimensions`) — merged with `--admin --squash`

## Release

**Tag:** `cli-v0.10.1`  
**Workflow Run:** `publish-cli.yml` run 24673091352 — completed successfully

## Context

The generic CLI default of 512×512 is provider-agnostic. MAI-Image-2 enforces a minimum of 768px per dimension. Rather than forcing users to manually specify dimensions or defaulting to 1024 for all providers, the adapter silently bumps under-768 dimensions to 1024 and logs a progress note so the user is informed but not blocked.
