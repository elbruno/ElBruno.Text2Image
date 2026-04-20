# Changelog

## [0.10.0] - 2025-04-20

### Added
- **Configurable model names** — CLI now supports switching between model variants
  - MAI-Image-2: Use `MAI-Image-2e` or other variants via `t2i config set foundry-mai2.model <name>`
  - FLUX.2: Switch between `FLUX.2-pro` and `FLUX.2-flex` via `t2i config set foundry-flux2.model <name>`
  - Library support: Pass custom `modelId` to `MaiImage2Generator` and `Flux2Generator` constructors
- **Enhanced config display** — `t2i config show` now displays endpoint and model names in plain text, with only API key masked
- **Setup wizard improvements** — Interactive setup now proposes default models during configuration
- **Cli update note** — Added `dotnet tool update --global ElBruno.Text2Image.Cli` to documentation

### Documentation
- Added "Choosing a model" section in README with CLI examples
- Updated library samples to show custom model parameter usage
- Added "Switching models" section to CLI blog post (docs/20260420-introducing-t2i-cli.md)
- Added "Using a different model" section to MAI-Image-2 setup guide
- Added "Choosing a FLUX.2 model" section to FLUX.2 setup guide

### Details
- Default behavior unchanged: MAI-Image-2 defaults to `MAI-Image-2`, FLUX.2 defaults to `FLUX.2-pro`
- Backward compatible — existing configurations continue to work without modification
- Config schema already supported model field; no breaking changes to storage format
