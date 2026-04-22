# Path Traversal Prevention

## Overview

Pattern for preventing directory traversal attacks in file write operations. Validates user-provided paths to ensure they cannot escape intended directories via symbolic links, `../` sequences, or other path manipulation techniques.

## When to Use

Apply this pattern to **all** file write operations that:
- Accept user-provided paths (command-line arguments, API inputs, configuration files)
- Write to sensitive directories (config, secrets, application data)
- Could expose internal file structure if exploited

## Pattern

```csharp
// 1. Resolve full path (resolves symlinks, normalizes separators, canonicalizes path)
var fullPath = Path.GetFullPath(userProvidedPath);

// 2. Determine expected base directory
var expectedBaseDir = Path.GetFullPath(Environment.CurrentDirectory); // or ConfigDirectory, etc.

// 3. Validate resolved path starts with expected base
if (!fullPath.StartsWith(expectedBaseDir, StringComparison.OrdinalIgnoreCase))
{
    throw new UnauthorizedAccessException(
        $"Path traversal detected: '{userProvidedPath}' resolves to '{fullPath}', " +
        $"which is outside the expected directory.");
}

// 4. Proceed with file operation using validated fullPath
await File.WriteAllBytesAsync(fullPath, data);
```

## Key Principles

1. **Always validate AFTER Path.GetFullPath()**: Validation before resolution is ineffective because attackers can use symlinks or junction points.

2. **Use Path.GetFullPath() to resolve paths**: This method:
   - Resolves symbolic links and junction points
   - Normalizes path separators (handles both `/` and `\`)
   - Canonicalizes paths (resolves `.` and `..` segments)
   - Converts relative paths to absolute

3. **Choose appropriate base directory**:
   - User-facing paths (images, outputs): Validate against `Environment.CurrentDirectory`
   - Configuration files: Validate against `ConfigPaths.ConfigDirectory`
   - Secret stores: Validate against platform-specific secure directories
   - Temp files: Validate against `Path.GetTempPath()`

4. **Use OrdinalIgnoreCase for Windows**: Windows paths are case-insensitive, so use `StringComparison.OrdinalIgnoreCase` for `StartsWith()` checks.

5. **Allow absolute paths cautiously**: For user output paths, consider allowing absolute paths if user explicitly provides them. For internal config/secrets, always enforce base directory.

## References

- OWASP: [Path Traversal](https://owasp.org/www-community/attacks/Path_Traversal)
- CWE-22: [Improper Limitation of a Pathname to a Restricted Directory](https://cwe.mitre.org/data/definitions/22.html)
- .NET Docs: [Path.GetFullPath](https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath)

## Implementation History

Applied in ElBruno.Text2Image:
- `ImageGenerationResult.SaveAsync` (user output paths)
- `InitCommand.WriteSkillFile` (skill file installation)
- `ConfigStore.SaveAsync` (configuration files)
- `PlainFileSecretStore.SaveStoreAsync` (plaintext secret storage)
- `DpapiSecretStore.SaveStoreAsync` (encrypted secret storage)

Commits: 2610955, 1c31a87 (2026-04-22)
