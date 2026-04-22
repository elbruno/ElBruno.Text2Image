# Agent Orchestration: Release (Mal)

**Date:** 2025-04-21  
**Time:** 20:00 UTC  
**Agent:** Mal (Release Lead)  
**Task:** Create coordinated release with multi-model support

---

## Objectives

1. Release Foundry library v0.11.0 with GPT-Image-2 support
2. Release CLI tool v0.12.0 with GPT-Image-2 integration
3. Apply semantic versioning (minor version bump)
4. Document release decision and rationale

---

## Deliverables

### Versions Released

- **foundry-v0.11.0** (from 0.10.0)
  - Minor version bump (new feature, no breaking changes)
  - GPT-Image-2 support added
  - All 668 tests passing (net8.0, net10.0)

- **cli-v0.12.0** (from 0.11.0)
  - Minor version bump (new feature, no breaking changes)
  - GPT-Image-2 CLI integration
  - GitHub releases created
  - NuGet packages auto-published via CI/CD

### Decision Document

- **File:** `.squad/decisions/inbox/mal-release-versioning.md`
- **Content:** Semantic versioning analysis, rationale, breaking change assessment
- **Status:** Ready for merge to `.squad/decisions.md`

---

## Models in Release

| Model | Status | Support |
|-------|--------|---------|
| GPT-Image-1.5 | Existing | DALL-E 3 via Azure OpenAI |
| **GPT-Image-2** | **New** | **Microsoft's latest model** |
| FLUX.2 | Existing | Foundry support |
| MAI-Image-2 | Existing | Foundry support |

---

## Test Coverage

- **Total Tests:** 668
- **net8.0 Passing:** 298
- **net10.0 Passing:** 370
- **Status:** 100% passing ✅

---

## Release Timeline

- **Tags Created:** April 21, 2025
- **GitHub Releases:** Post-tag creation
- **NuGet Publication:** Auto-published via CI/CD
- **Availability:** Public npm/NuGet registries

---

## Breaking Changes

**None** — All existing APIs remain unchanged. New features are additive only.

---

## Deprecations

**None** — All existing models remain fully supported.

---

## Status

**✅ Complete** — Versions released, decision documented, ready for blog announcement.

---

*Coordinated by Mal. Merged to decisions by Scribe.*
