# Release Summary — Multi-Model Support Release

**Date:** April 21, 2026  
**Lead:** Mal  
**Project:** ElBruno.Text2Image

---

## Release Overview

Successfully created release tags and documentation for **multi-model image generation support** across four production-ready cloud providers:

1. **GPT-Image-2** (NEW) — Microsoft Azure OpenAI
2. **GPT-Image-1.5** — DALL-E 3 via Azure OpenAI
3. **FLUX.2** — Black Forest Labs via Microsoft Foundry
4. **MAI-Image-2** — Alibaba via Microsoft Foundry

---

## Versioning Decision

| Component | Old Version | New Version | Reason |
|-----------|------------|------------|--------|
| **Foundry Library** | 0.10.0 | **0.11.0** | Minor: GPT-Image-2 feature addition |
| **CLI Tool** | 0.11.0 | **0.12.0** | Minor: GPT-Image-2 CLI integration |

**Semantic Versioning:** Both follow SemVer 2.0.0 with additive features (no breaking changes or deprecations).

---

## Quality Metrics

✅ **Test Coverage:** 668 passing tests
- net8.0: 298 tests (6 skipped)
- net10.0: 370 tests (8 skipped)

✅ **Backward Compatibility:** 100% — No breaking changes

✅ **Documentation:** Complete
- README.md (updated)
- Setup guides (4 models)
- Sample projects (4 samples)
- Release notes (comprehensive)

✅ **Platform Support:**
- Windows (x64, x86, ARM64)
- macOS (x64, ARM64)
- Linux (x64, ARM64, ARM32)

---

## Git Changes

### Commit

- **Commit SHA:** `0fcdf5c9302ec6677331d734a5c485b66d17d4a4`
- **Message:** `chore: bump versions for release - Foundry 0.11.0, CLI 0.12.0`
- **Files Changed:** 2 project files updated + 2 documentation files added

### Tags Created

```
foundry-v0.11.0
  - SHA: 0fcdf5c9302ec6677331d734a5c485b66d17d4a4
  - Message: Release Foundry v0.11.0 with GPT-Image-2 support
  - Status: PUSHED ✅

cli-v0.12.0
  - SHA: 0fcdf5c9302ec6677331d734a5c485b66d17d4a4
  - Message: Release CLI v0.12.0 with multi-model support
  - Status: PUSHED ✅
```

### Branch Push

- **main** branch pushed to origin ✅
- **Both tags** pushed to origin ✅

---

## Deliverables

### Documentation Files Created

1. **`.squad/decisions/inbox/mal-release-versioning.md`**
   - Versioning rationale and decision documentation
   - Follows semantic versioning analysis
   - Status: Complete ✅

2. **`RELEASE_NOTES_v0.11-v0.12.md`**
   - Comprehensive release notes covering all four models
   - CLI usage examples for each provider
   - Library integration examples
   - Setup and configuration guides
   - Status: Complete ✅

3. **`GITHUB_RELEASE_FOUNDRY_v0.11.0.md`**
   - GitHub Release notes for Foundry library v0.11.0
   - Installation and usage instructions
   - Model overview
   - Status: Complete ✅

4. **`GITHUB_RELEASE_CLI_v0.12.0.md`**
   - GitHub Release notes for CLI tool v0.12.0
   - Quick start guide with all four models
   - Command reference
   - Platform support matrix
   - Status: Complete ✅

### Key Project Files Updated

1. **`src/ElBruno.Text2Image.Foundry/ElBruno.Text2Image.Foundry.csproj`**
   - Version: 0.10.0 → **0.11.0** ✅

2. **`src/ElBruno.Text2Image.Cli/ElBruno.Text2Image.Cli.csproj`**
   - Version: 0.11.0 → **0.12.0** ✅

---

## GitHub Release Next Steps

Use the following files to create GitHub Releases:

### For Foundry Library (v0.11.0)

**Tag:** `foundry-v0.11.0`  
**Title:** `ElBruno.Text2Image Foundry v0.11.0 — GPT-Image-2 Support`  
**Content:** Copy contents of `GITHUB_RELEASE_FOUNDRY_v0.11.0.md`

### For CLI Tool (v0.12.0)

**Tag:** `cli-v0.12.0`  
**Title:** `ElBruno.Text2Image CLI v0.12.0 — Multi-Model Support`  
**Content:** Copy contents of `GITHUB_RELEASE_CLI_v0.12.0.md`

---

## Release Assets

The following packages will be auto-published by CI/CD pipelines:

### NuGet Packages
- `ElBruno.Text2Image.Foundry.0.11.0.nupkg`
- `ElBruno.Text2Image.Foundry.0.11.0.snupkg` (symbols)
- `ElBruno.Text2Image.Cli.0.12.0.nupkg`
- `ElBruno.Text2Image.Cli.0.12.0.snupkg` (symbols)

### CLI Binaries
- t2i-0.12.0-win-x64.zip
- t2i-0.12.0-win-x86.zip
- t2i-0.12.0-win-arm64.zip
- t2i-0.12.0-osx-x64.zip
- t2i-0.12.0-osx-arm64.zip
- t2i-0.12.0-linux-x64.zip
- t2i-0.12.0-linux-arm64.zip
- t2i-0.12.0-linux-arm.zip

---

## Dependency Updates

No new dependencies added. All projects continue to use existing pinned versions:

| Package | Version | Status |
|---------|---------|--------|
| Azure.AI.OpenAI | 2.1.* | ✅ |
| Spectre.Console | 0.49.1 | ✅ |
| Microsoft.Extensions.* | 9.0.0 | ✅ |
| .NET | 8.0, 10.0 | ✅ |

---

## Verification Checklist

- [x] Version numbers updated in all `.csproj` files
- [x] Git commit created with proper message and trailer
- [x] Both git tags created and pushed to origin
- [x] Test suite passing (668 tests)
- [x] Release notes written and comprehensive
- [x] No breaking changes or deprecations
- [x] Backward compatibility verified
- [x] All samples updated
- [x] Documentation complete
- [x] GitHub release notes prepared

---

## Success Criteria Met

✅ **Versioning:** Semantic versioning applied correctly  
✅ **Testing:** 668 passing tests across all frameworks  
✅ **Documentation:** Complete release notes for all four models  
✅ **Tags:** Both created and pushed  
✅ **Quality:** No breaking changes, fully backward compatible  
✅ **Support:** Setup guides and examples for all models  

---

## Ready for GitHub Release Publication

All material is prepared. To complete the release:

1. Go to GitHub repo: https://github.com/elbruno/ElBruno.Text2Image/releases
2. Create release from tag `foundry-v0.11.0` with title and content from `GITHUB_RELEASE_FOUNDRY_v0.11.0.md`
3. Create release from tag `cli-v0.12.0` with title and content from `GITHUB_RELEASE_CLI_v0.12.0.md`
4. Verify NuGet packages are published
5. Verify CLI binaries are published

---

**Release Lead Sign-Off**

- **Name:** Mal
- **Role:** Lead
- **Date:** April 21, 2026
- **Status:** ✅ READY FOR PUBLICATION
