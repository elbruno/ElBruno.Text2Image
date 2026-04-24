<#
.SYNOPSIS
  Updates the version for ALL packages in the ElBruno.Text2Image monorepo.
  
.DESCRIPTION
  This script ensures unified versioning across all packages:
  - ElBruno.Text2Image (library)
  - ElBruno.Text2Image.Cli
  - ElBruno.Text2Image.Foundry
  - ElBruno.Text2Image.Cuda
  - ElBruno.Text2Image.Cpu
  - ElBruno.Text2Image.DirectML
  
  All packages MUST have identical version numbers. When one is updated, all are updated.

.PARAMETER Version
  The new version number (e.g., "1.2.1", "1.3.0")

.PARAMETER Commit
  If $true, automatically commits the changes with a standard message.
  Default: $false

.PARAMETER Tag
  If $true, creates a git tag after commit.
  Default: $false

.EXAMPLE
  .\scripts\Update-AllVersions.ps1 -Version "1.2.2"
  
  .\scripts\Update-AllVersions.ps1 -Version "1.3.0" -Commit -Tag

.NOTES
  - All .csproj files in src/ are updated
  - No other files are modified
  - Build and tests are NOT run automatically (do this manually after)
  - Use -Commit to auto-stage and commit the changes
  - Use -Tag to create a vX.Y.Z git tag
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [switch]$Commit,
    
    [Parameter(Mandatory = $false)]
    [switch]$Tag
)

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format X.Y.Z (e.g., 1.2.1). Got: $Version"
    exit 1
}

# Find all .csproj files to update
$projectFiles = @(
    "src\ElBruno.Text2Image\ElBruno.Text2Image.csproj",
    "src\ElBruno.Text2Image.Cli\ElBruno.Text2Image.Cli.csproj",
    "src\ElBruno.Text2Image.Foundry\ElBruno.Text2Image.Foundry.csproj",
    "src\ElBruno.Text2Image.Cuda\ElBruno.Text2Image.Cuda.csproj",
    "src\ElBruno.Text2Image.Cpu\ElBruno.Text2Image.Cpu.csproj",
    "src\ElBruno.Text2Image.DirectML\ElBruno.Text2Image.DirectML.csproj"
)

Write-Host "🔄 Updating all packages to version $Version..." -ForegroundColor Cyan

$updatedCount = 0
foreach ($projectFile in $projectFiles) {
    if (-not (Test-Path $projectFile)) {
        Write-Warning "⚠️  File not found: $projectFile (skipping)"
        continue
    }
    
    $content = Get-Content $projectFile -Raw
    $oldVersion = [regex]::Match($content, '<Version>([\d.]+)</Version>').Groups[1].Value
    
    # Replace the version tag
    $newContent = $content -replace '<Version>[\d.]+</Version>', "<Version>$Version</Version>"
    
    # Write back to file
    Set-Content $projectFile -Value $newContent -NoNewline
    
    Write-Host "  ✅ $projectFile"
    Write-Host "     $oldVersion → $Version" -ForegroundColor Green
    $updatedCount++
}

if ($updatedCount -eq 0) {
    Write-Error "❌ No project files were found or updated."
    exit 1
}

Write-Host ""
Write-Host "✅ Updated $updatedCount package(s) to version $Version" -ForegroundColor Green

# Optional: commit changes
if ($Commit) {
    Write-Host ""
    Write-Host "📝 Committing version bump..." -ForegroundColor Cyan
    git add -A
    $commitMessage = "chore: bump all packages to v$Version`n`nCo-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"
    git commit -m $commitMessage
    Write-Host "  ✅ Committed" -ForegroundColor Green
}

# Optional: create git tag
if ($Tag) {
    Write-Host ""
    Write-Host "🏷️  Creating git tag..." -ForegroundColor Cyan
    git tag "v$Version"
    Write-Host "  ✅ Tag created: v$Version" -ForegroundColor Green
    Write-Host "  📤 Push with: git push origin main && git push origin v$Version" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✨ Done!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run: dotnet build ElBruno.Text2Image.slnx --no-restore"
Write-Host "  2. Run: dotnet test src\ElBruno.Text2Image.Tests\ --no-restore"
if (-not $Commit) {
    Write-Host "  3. Review changes and commit: git add -A && git commit -m 'chore: bump all packages to v$Version'"
}
if (-not $Tag) {
    Write-Host "  4. Create tag: git tag v$Version"
    Write-Host "  5. Push: git push origin main && git push origin v$Version"
}
else {
    Write-Host "  3. Push: git push origin main && git push origin v$Version"
}
