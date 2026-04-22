# Release Index — Multi-Model Support (v0.11.0 & v0.12.0)

**Project:** ElBruno.Text2Image  
**Lead:** Mal  
**Release Date:** April 21, 2026  
**Status:** ✅ COMPLETE & READY FOR PUBLICATION

---

## Quick Reference

### Versions Released

| Component | Version | Tag | Commit |
|-----------|---------|-----|--------|
| **Foundry Library** | 0.11.0 | `foundry-v0.11.0` | `0fcdf5c` |
| **CLI Tool** | 0.12.0 | `cli-v0.12.0` | `0fcdf5c` |

### Models Supported

1. ✅ **GPT-Image-2** (NEW) — Microsoft Azure OpenAI
2. ✅ **GPT-Image-1.5** — DALL-E 3 via Azure OpenAI
3. ✅ **FLUX.2** — Black Forest Labs via Microsoft Foundry
4. ✅ **MAI-Image-2** — Alibaba via Microsoft Foundry

### Quality Metrics

- ✅ **Tests:** 668/668 passing (net8.0 + net10.0)
- ✅ **Breaking Changes:** 0 (fully backward compatible)
- ✅ **Deprecations:** 0
- ✅ **Documentation:** Complete for all four models

---

## Documentation Map

### 1. Versioning Decision
📄 **File:** `.squad/decisions/inbox/mal-release-versioning.md`

**Contains:**
- Versioning rationale (SemVer 2.0.0 analysis)
- Why 0.11.0 for Foundry (minor bump for new feature)
- Why 0.12.0 for CLI (minor bump for new feature)
- No breaking changes or deprecations
- Timeline and dependencies

**For:** Architectural decision documentation

---

### 2. Full Release Notes
📄 **File:** `RELEASE_NOTES_v0.11-v0.12.md`

**Contains:**
- Comprehensive overview of all four models
- CLI usage examples for each provider
- Library integration examples
- Setup and configuration guides
- Testing information (668 tests)
- Breaking changes (none) and deprecations (none)
- Dependencies and platform support (net8.0, net10.0)
- Migration guide from v0.10.0

**For:** Complete release documentation reference

---

### 3. GitHub Release — Foundry v0.11.0
📄 **File:** `GITHUB_RELEASE_FOUNDRY_v0.11.0.md`

**Contains:**
- What's New section (GPT-Image-2 support)
- Installation instructions (NuGet)
- Model overview
- Library usage examples
- Setup prerequisites
- Test coverage metrics
- Breaking changes (none)

**For:** GitHub release page (foundry-v0.11.0)

**Copy & Paste to GitHub Release:**
1. Go to: https://github.com/elbruno/ElBruno.Text2Image/releases
2. Create new release from tag: `foundry-v0.11.0`
3. Title: `ElBruno.Text2Image Foundry v0.11.0 — GPT-Image-2 Support`
4. Copy full content of this file into description
5. Publish

---

### 4. GitHub Release — CLI v0.12.0
📄 **File:** `GITHUB_RELEASE_CLI_v0.12.0.md`

**Contains:**
- What's New section (GPT-Image-2 CLI provider)
- Installation as .NET tool
- Quick start guide with all four models
- Command reference
- Configuration instructions
- Setup with environment variables
- Platform support matrix
- Migration notes

**For:** GitHub release page (cli-v0.12.0)

**Copy & Paste to GitHub Release:**
1. Go to: https://github.com/elbruno/ElBruno.Text2Image/releases
2. Create new release from tag: `cli-v0.12.0`
3. Title: `ElBruno.Text2Image CLI v0.12.0 — Multi-Model Support`
4. Copy full content of this file into description
5. Publish

---

### 5. Release Summary
📄 **File:** `RELEASE_SUMMARY.md`

**Contains:**
- Execution checklist
- Versioning decision summary
- Git changes (commit + tags)
- All deliverables listed
- Quality metrics
- Model support overview
- Verification checklist
- Success criteria confirmation

**For:** Release execution documentation

---

### 6. Execution Complete Report
📄 **File:** `EXECUTION_COMPLETE.md`

**Contains:**
- Executive summary
- What was accomplished (5 major sections)
- Quality assurance results
- Documentation artifacts list
- Models supported (4 total)
- Next steps for Bruno
- Files ready for copy-paste
- Quality checkpoints (all passed)
- Sign-off and approval

**For:** Completion confirmation and next steps

---

## Git Artifacts

### Branch & Commits
```
Branch: main
Commit: 0fcdf5c (HEAD)
Message: chore: bump versions for release - Foundry 0.11.0, CLI 0.12.0

Changes:
- src/ElBruno.Text2Image.Foundry/ElBruno.Text2Image.Foundry.csproj (0.10.0 → 0.11.0)
- src/ElBruno.Text2Image.Cli/ElBruno.Text2Image.Cli.csproj (0.11.0 → 0.12.0)
- RELEASE_NOTES_v0.11-v0.12.md (added)
```

### Tags
```
foundry-v0.11.0
  └─ Commit: 0fcdf5c
  └─ Message: Release Foundry v0.11.0 - GPT-Image-2 Support
  └─ Status: Pushed to origin ✅

cli-v0.12.0
  └─ Commit: 0fcdf5c
  └─ Message: Release CLI v0.12.0 - Multi-Model Support
  └─ Status: Pushed to origin ✅
```

---

## Test Results

```
.NET 8.0 (net8.0):
  ✅ Passed:   298
  ⊘ Skipped:  6
  Total:      304

.NET 10.0 (net10.0):
  ✅ Passed:   370
  ⊘ Skipped:  8
  Total:      378

OVERALL:
  ✅ Total Passed: 668
  ⊘ Total Skipped: 14
  ❌ Failed: 0
```

---

## Models Documentation

### 1. GPT-Image-2 (NEW)
- **Class:** `GptImage2Generator`
- **Provider ID:** `foundry-gpt-image-2`
- **CLI:** `t2i --provider foundry-gpt-image-2 "prompt"`
- **Supported Sizes:** 1024×1024, 1792×1024, 1024×1792
- **Setup:** Azure OpenAI Service

### 2. GPT-Image-1.5 (DALL-E 3)
- **Class:** `GptImage1p5Generator`
- **Provider ID:** `foundry-gpt-image-1p5`
- **CLI:** `t2i --provider foundry-gpt-image-1p5 "prompt"`
- **Supported Sizes:** 1024×1024, 1792×1024, 1024×1792
- **Setup:** Azure OpenAI Service

### 3. FLUX.2
- **Class:** `Flux2Generator`
- **Provider ID:** `foundry-flux2`
- **CLI:** `t2i --provider foundry-flux2 "prompt"`
- **Variants:** FLUX.2-pro (default), FLUX.2-flex
- **Setup:** Microsoft Foundry API

### 4. MAI-Image-2
- **Class:** `MaiImage2Generator`
- **Provider ID:** `foundry-mai2`
- **CLI:** `t2i --provider foundry-mai2 "prompt"`
- **Variants:** MAI-Image-2 (default), MAI-Image-2e
- **Setup:** Microsoft Foundry API

---

## How to Use This Release Index

### For Bruno (Project Lead)

1. **Review Versioning Decision**
   → Open `.squad/decisions/inbox/mal-release-versioning.md`
   → Confirm SemVer logic is correct

2. **Review Full Release Notes**
   → Open `RELEASE_NOTES_v0.11-v0.12.md`
   → Reference for any questions about content

3. **Create Foundry Release on GitHub**
   → Copy content from `GITHUB_RELEASE_FOUNDRY_v0.11.0.md`
   → Create new release from tag `foundry-v0.11.0`

4. **Create CLI Release on GitHub**
   → Copy content from `GITHUB_RELEASE_CLI_v0.12.0.md`
   → Create new release from tag `cli-v0.12.0`

5. **Verify CI/CD**
   → Check NuGet package publication
   → Check CLI binary publication

### For Developers

1. **Upgrade Foundry Library**
   ```bash
   dotnet add package ElBruno.Text2Image.Foundry --version 0.11.0
   ```

2. **Use GPT-Image-2**
   ```csharp
   var generator = new GptImage2Generator(endpoint, apiKey);
   var result = await generator.GenerateAsync("Prompt", 1024, 1024);
   ```

3. **Upgrade CLI Tool**
   ```bash
   dotnet tool update --global ElBruno.Text2Image.Cli --version 0.12.0
   ```

4. **Use GPT-Image-2 CLI**
   ```bash
   t2i --provider foundry-gpt-image-2 "Your prompt"
   ```

### For Documentation

- Link to `RELEASE_NOTES_v0.11-v0.12.md` from all GitHub releases
- Reference individual GitHub release pages for quick access

---

## Verification Checklist

- [x] Versioning decision documented
- [x] Git commit created with version bumps
- [x] Both git tags created and pushed
- [x] All 668 tests passing
- [x] Release notes written for all four models
- [x] GitHub release notes prepared (Foundry)
- [x] GitHub release notes prepared (CLI)
- [x] No breaking changes identified
- [x] No deprecations identified
- [x] Full backward compatibility verified

---

## Ready for Publication

✅ All documentation complete  
✅ Git tags pushed  
✅ Tests passing  
✅ No blockers  

**Status: READY FOR GITHUB RELEASE PUBLICATION**

---

## Questions?

Refer to:
- **Versioning questions** → `mal-release-versioning.md`
- **Feature details** → `RELEASE_NOTES_v0.11-v0.12.md`
- **Foundry release** → `GITHUB_RELEASE_FOUNDRY_v0.11.0.md`
- **CLI release** → `GITHUB_RELEASE_CLI_v0.12.0.md`
- **Execution details** → `EXECUTION_COMPLETE.md`

---

**Release prepared by:** Mal (Lead)  
**Date:** April 21, 2026  
**Sign-off:** ✅ APPROVED
