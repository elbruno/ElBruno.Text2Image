# Version Management

> **Key Rule:** All packages in the ElBruno.Text2Image monorepo MUST have identical version numbers. When you update one, you update all.

## Why Unified Versioning?

- **Clarity:** Users see "v1.2.1" and know all packages are compatible
- **Simplicity:** No confusion about which CLI version goes with which library version
- **Automation:** Reduces manual errors during releases
- **Consistency:** Matches user expectations

## How to Bump the Version

### Quick Start

```powershell
# Update all packages to v1.2.2 and commit with tag
.\scripts\Update-AllVersions.ps1 -Version "1.2.2" -Commit -Tag

# Then push
git push origin main && git push origin v1.2.2
```

### Step-by-Step

**Step 1: Update all versions**
```powershell
.\scripts\Update-AllVersions.ps1 -Version "1.2.2"
```

**Step 2: Build and test**
```bash
dotnet build ElBruno.Text2Image.slnx --no-restore
dotnet test src\ElBruno.Text2Image.Tests\ --no-restore
```

**Step 3: Commit**
```bash
git add -A
git commit -m "chore: bump all packages to v1.2.2"
```

**Step 4: Tag**
```bash
git tag v1.2.2
```

**Step 5: Push**
```bash
git push origin main && git push origin v1.2.2
```

**Step 6: Create GitHub release**
```bash
gh release create v1.2.2 \
  --title "v1.2.2 — Release Title" \
  --notes "Release notes here..."
```

The publish-to-NuGet workflow will run automatically.

## Packages Tracked

The unified versioning applies to these packages:

| Package | Path | NuGet |
|---------|------|-------|
| ElBruno.Text2Image | `src/ElBruno.Text2Image/` | [nuget.org/packages/ElBruno.Text2Image](https://www.nuget.org/packages/ElBruno.Text2Image) |
| ElBruno.Text2Image.Cli | `src/ElBruno.Text2Image.Cli/` | [nuget.org/packages/ElBruno.Text2Image.Cli](https://www.nuget.org/packages/ElBruno.Text2Image.Cli) |
| ElBruno.Text2Image.Foundry | `src/ElBruno.Text2Image.Foundry/` | [nuget.org/packages/ElBruno.Text2Image.Foundry](https://www.nuget.org/packages/ElBruno.Text2Image.Foundry) |
| ElBruno.Text2Image.Cuda | `src/ElBruno.Text2Image.Cuda/` | [nuget.org/packages/ElBruno.Text2Image.Cuda](https://www.nuget.org/packages/ElBruno.Text2Image.Cuda) |
| ElBruno.Text2Image.Cpu | `src/ElBruno.Text2Image.Cpu/` | [nuget.org/packages/ElBruno.Text2Image.Cpu](https://www.nuget.org/packages/ElBruno.Text2Image.Cpu) |
| ElBruno.Text2Image.DirectML | `src/ElBruno.Text2Image.DirectML/` | [nuget.org/packages/ElBruno.Text2Image.DirectML](https://www.nuget.org/packages/ElBruno.Text2Image.DirectML) |

## Automation & Safety

### Pre-Commit Hook

A pre-commit hook (`.githooks/pre-commit-version-check`) automatically verifies that all `.csproj` files have matching versions before any commit. If you try to commit with mismatched versions, you'll see:

```
❌ VERSION MISMATCH DETECTED:
  src\ElBruno.Text2Image\ElBruno.Text2Image.csproj: 1.2.1
  src\ElBruno.Text2Image.Cli\ElBruno.Text2Image.Cli.csproj: 1.2.2

All packages MUST have the same version number.
```

**Fix with:** `.\scripts\Update-AllVersions.ps1 -Version "1.2.1"`

### CI/CD Validation

The NuGet publish workflow (`.github/workflows/publish.yml`) also validates version consistency. If versions don't match, the workflow will fail.

## Examples

### Releasing a Patch Fix

```powershell
# Update from v1.2.0 to v1.2.1
.\scripts\Update-AllVersions.ps1 -Version "1.2.1" -Commit -Tag
git push origin main && git push origin v1.2.1
gh release create v1.2.1 --title "v1.2.1 — Timeout Fix" --notes "Fixed configurable timeout for slow image providers"
```

### Releasing a Minor Feature

```powershell
# Update from v1.2.1 to v1.3.0
.\scripts\Update-AllVersions.ps1 -Version "1.3.0" -Commit -Tag
git push origin main && git push origin v1.3.0
gh release create v1.3.0 --title "v1.3.0 — New Features" --notes "Added support for X, improved Y, fixed Z"
```

## See Also

- [`docs/publishing.md`](./publishing.md) — Full publishing workflow for NuGet
- [`.squad/decisions.md`](../.squad/decisions.md) — Team decision on unified versioning
- [`scripts/Update-AllVersions.ps1`](../scripts/Update-AllVersions.ps1) — The automation script (full help available)
