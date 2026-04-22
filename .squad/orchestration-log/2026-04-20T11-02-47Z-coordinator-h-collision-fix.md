# Orchestration Log: CLI `-h` Short Option Collision Fix

**Date:** 2026-04-20 11:02:47 UTC  
**Trigger:** User "Bruno Capuano" reported `t2i -h` failed: `Option 'height' is defined but no value has been provided`  
**Root Cause:** `--height` option had `-h` short alias, which collided with Spectre.Console's built-in `-h, --help`  
**Mode:** Lightweight (Coordinator only, no domain agents)

## Actions Taken

1. **Branch:** Created `fix/cli-height-shortopt-collision` from `main`
2. **Code Fix:** Removed `-h` short alias from `--height` in `GenerateCommand.cs`
3. **Documentation:** Updated `docs/cli-tool.md` options table (removed `-h` from height row)
4. **Version Bump:** CLI csproj `0.10.1` → `0.10.2`
5. **Changelog:** Added entry to CHANGELOG
6. **Build:** Clean build, 0 warnings
7. **Tests:** 404 passing
8. **Verification:** `dotnet run -- -h` correctly shows help (not height error)
9. **PR:** Opened and merged PR #16 with `--admin --squash`
10. **Release:** Created release `cli-v0.10.2`
11. **CI:** Publish workflow run 24673709800 completed successfully

## Key Decision

CLI short option aliases must not collide with Spectre.Console built-ins. The `-h` flag is reserved for `--help`. Future CLI options must check Spectre.Console's reserved short-flag set before assigning aliases.

## Outcome

- Fix deployed and released
- No user-facing breaking changes
- Help system works as expected
