# Changelog

All notable changes to ElBruno.Text2Image are documented in this file.

## [1.5.2] — Candidate (2026-09-14)

### Candidate release
- Consolidates GPT-Image-2.5-Sunburst and GPT-Image-2.5-Flare support, retired-provider
  migration handling, GPT image sizing fixes, managed skill updates, and the
  `ElBruno.Text2Image.BlazorComponents` Razor Class Library for the 1.5.2 candidate.

## [1.5.1] — 2026-09-14

### Added
- GPT-Image-2.5-Sunburst and GPT-Image-2.5-Flare support through a shared
  parameterized Azure OpenAI generator and CLI providers.
- `ElBruno.Text2Image.BlazorComponents`, a .NET 8 Razor Class Library with five
  reusable components for image-generation interfaces, plus the
  `BlazorText2ImageDemo` sample.

### Changed
- Retired `foundry-mai2` as a migration-only provider that reports a clear
  migration error instead of silently falling back.
- GPT image requests now map requested dimensions to the supported square,
  landscape, or portrait output sizes instead of sending unsupported sizes.

### Documentation
- Added the Blazor component API guide, including native `IImageGenerator`
  integration and optional caption/progress behavior.
- Updated the managed GitHub Copilot and Claude Code `t2i` skills to version
  1.5.1 with current providers, lifecycle guidance, command reference, and
  `t2i upgrade` workflows.
- Added a release-documentation rule requiring validation of package/API
  guidance and an updated, five-entry maximum What's New table for every
  NuGet release.

## [1.4.0] — 2026-07-10

### Added
- `t2i upgrade` refreshes only managed GitHub Copilot and Claude Code skill files, preserving
  user-owned skills and never creating missing targets.
- A Foundry batch-generation sample (`scenario-17-foundry-batch`).

### Changed
- MAI-Image-2.5 and MAI-Image-2.5-Flash now use the Microsoft Foundry
  `/mai/v1/images/generations` endpoint and its 1024×1024 maximum output size.
- Cloud provider commands accept an endpoint override, which takes precedence over configured
  and legacy secret-store endpoints.
- CLI skill and provider documentation now reflects current models, commands, and setup guidance.

### Fixed
- Config and secret-store behavior is isolated per process, preventing cross-test configuration
  leakage.

## [1.3.0] — 2026-06-02

### Added
- **MAI-Image-2.5 and MAI-Image-2.5-Flash** support via Microsoft Foundry.
  - New `MaiImage25Generator` class (in `ElBruno.Text2Image.Foundry`) targeting the
    OpenAI-compatible `/openai/v1/images/generations` endpoint. Both variants are served
    by the same class, selected via `modelId`.
  - New DI helpers: `AddMaiImage25Generator(...)` and `AddMaiImage25FlashGenerator(...)`.
  - New CLI providers: `foundry-mai25` and `foundry-mai25-flash`.
  - New sample: `scenario-18-mai-image25-cloud`.
  - New setup guide: `docs/mai-image-2.5-setup-guide.md`.

## [1.2.6] — 2026-04-29

### Security
- **Fixed:** Windows credentials now encrypted with DPAPI by default instead of plaintext JSON.
  When `t2i secrets set` is called on Windows without `--store` flag, credentials are stored encrypted.
  Plaintext fallback is no longer available on Windows. Use environment variables for CI/CD.

### Fixed
- EndToEnd test assertions: Marked as tech debt for next sprint (exit codes from `t2i doctor`)

## [1.2.5] — 2026-04-28

### Highlights
- All packages and CLI now released at same version (v1.2.4) — enforced via CI/CD gate

### Added
- Version alignment rule validation added to prevent future inconsistencies

### Note
- This is a test release validating the version alignment enforcement.

## [1.2.3] — 2026-04-24

### Added
- Version alignment rule: all packages now at 1.2.3
- CLI version display in `t2i doctor` command
- CI/CD version synchronization validation

### Changed
- Documentation updates for version management

## [1.2.2]

### Added
- Initial release with version management infrastructure

---

For detailed version history, see [docs/version-management.md](./docs/version-management.md).
