# Session Log: CLI `-h` Short Option Collision Fix

**Session:** 2026-04-20 11:02:47 UTC  
**Work:** Coordinator  
**Trigger:** `t2i -h` error: option collision  

## What Happened

- Identified `-h` short alias on `--height` conflicted with Spectre.Console's `-h, --help`
- Removed `-h` short alias from `GenerateCommand.cs`
- Updated documentation and version (0.10.1 → 0.10.2)
- Build clean, tests passed (404), help verified working
- PR #16 merged, release cli-v0.10.2 published

## Decision

Short option aliases must not collide with Spectre.Console built-ins (`-h` is reserved).

## Outcome

Fixed. No impact to existing functionality.
