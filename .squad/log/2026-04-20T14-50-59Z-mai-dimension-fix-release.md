# Session Log: MAI Dimension Fix & Release

**Timestamp:** 2026-04-20T14:50:59Z  
**Scope:** MAI-Image-2 default dimension bump + CLI v0.10.1 release

**What Happened:**
- Coordinator (inline mode) fixed MAI-Image-2 adapter to auto-bump dimensions <768px to 1024
- CLI version bumped 0.10.0 → 0.10.1
- CHANGELOG updated
- PRs #14 and #15 merged, release tagged and published

**Outcome:** Users invoking `t2i "..."` with generic defaults now work with MAI-Image-2 without errors.

**Files Changed:** 3 (generator, csproj, changelog)  
**Build Status:** Clean (0 warnings)  
**Tests:** 404 passing
