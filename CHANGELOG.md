# Changelog

## [0.10.2] - 2026-04-20

### Fixed
- **`-h` short option collision** — `t2i -h` now correctly shows help instead of
  failing with `Option 'height' is defined but no value has been provided`. The
  `--height` option no longer registers `-h` as a short alias (it conflicted with
  the built-in `-h, --help`). `--width` still accepts `-w`; height must be passed
  via the long form `--height <pixels>`.

## [0.10.1] - 2026-04-20

### Fixed
- **MAI-Image-2 default dimensions** — `t2i "..."` with no `--width`/`--height` flags
  no longer fails with `MAI-Image-2 requires both dimensions to be at least 768px`.
  The CLI's generic 512px default is now silently bumped to MAI's preferred 1024px
  when the selected provider is `foundry-mai2`, with a note shown in the progress
  output. Users can still pass explicit dimensions ≥ 768px.

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
- Added "Switching models" section to CLI blog post (docs/blogs/20260420-introducing-t2i-cli.md)
- Added "Using a different model" section to MAI-Image-2 setup guide
- Added "Choosing a FLUX.2 model" section to FLUX.2 setup guide

### Details
- Default behavior unchanged: MAI-Image-2 defaults to `MAI-Image-2`, FLUX.2 defaults to `FLUX.2-pro`
- Backward compatible — existing configurations continue to work without modification
- Config schema already supported model field; no breaking changes to storage format
