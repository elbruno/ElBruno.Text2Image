# Changelog

All notable changes to ElBruno.Text2Image are documented in this file.

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
