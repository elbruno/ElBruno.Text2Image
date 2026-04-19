# winget Manifests

This directory contains Windows Package Manager (winget) manifests for the `t2i` CLI tool.

## v0.1.0 — Manual Submission

The initial release (v0.1.0) requires **manual submission** to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).

### How to Submit v0.1.0

1. **Wait for the GitHub Release** to complete (tag `cli-v0.1.0`)
2. **Download the asset** `t2i-win-x64.zip` from the release
3. **Compute SHA256 hash**:
   ```powershell
   Get-FileHash t2i-win-x64.zip -Algorithm SHA256
   ```
4. **Update the manifest**:
   - Replace `PLACEHOLDER_FILL_AFTER_RELEASE` in `ElBruno.Text2Image.yaml` with the actual SHA256
5. **Submit PR to microsoft/winget-pkgs**:
   - Clone [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)
   - Copy `winget/manifests/E/ElBruno/Text2Image/0.1.0/` to their `manifests/e/ElBruno/Text2Image/0.1.0/`
   - Open a PR
   - Wait for validation and merge

## v0.2.0+ — Automated

Future releases (v0.2.0+) will use [vedantmgoyal9/winget-releaser@v2](https://github.com/vedantmgoyal9/winget-releaser) triggered by `cli-v*` tags.

The workflow will:
1. Detect the release event
2. Download the `t2i-win-x64.zip` asset
3. Compute SHA256 automatically
4. Submit PR to microsoft/winget-pkgs with the updated manifest

## Manifest Schema

Version: **1.6.0** (singleton manifest)

Reference: [ManifestSpecv1.6.0.md](https://github.com/microsoft/winget-cli/blob/master/doc/ManifestSpecv1.6.0.md)
