# Security Findings - ElBruno.Text2Image
**Reviewer:** Kaylee (Core Dev)  
**Date:** 2025-01-26  
**Scope:** Complete security-focused code review

---

## Security Findings

### Critical (Immediate Fix)
**None identified.** The codebase demonstrates strong security practices overall.

### High (Fix Soon)

#### H-1: API Key Exposure in Adapter Health Checks
**File:** `src\ElBruno.Text2Image.Cli\Providers\FoundryFlux2Adapter.cs:61`  
**File:** `src\ElBruno.Text2Image.Cli\Providers\FoundryMaiImage2Adapter.cs:64`  
**Issue:** Health check sends API key in Authorization header to test endpoint connectivity, but doesn't validate certificate or handle potential MITM attacks.

```csharp
// FoundryFlux2Adapter.cs, line 61
using var request = new HttpRequestMessage(HttpMethod.Head, endpoint);
request.Headers.Add("Authorization", $"Bearer {apiKey}");
```

**Risk:** If an attacker can perform a MITM attack, they could intercept the API key during health checks.

**Recommendation:**  
1. Ensure HttpClient is configured with certificate validation (it is by default, but should be explicit)
2. Consider making health checks optional or use a dedicated health endpoint that doesn't require credentials
3. Add timeout and retry limits to prevent DoS via health check abuse

---

#### H-2: Plaintext Secret Storage Warning May Be Insufficient
**File:** `src\ElBruno.Text2Image.Cli\Secrets\PlainFileSecretStore.cs:150-156`

**Issue:** While the code warns users about plaintext storage, the warning only appears once per session and might be missed. Secrets are stored in `~/.config/t2i/secrets.json` (Linux/macOS) or `%APPDATA%\t2i\secrets.json` (Windows) with restricted permissions on Unix, but no encryption.

```csharp
private void WarnOnFirstUse()
{
    if (!_hasWarnedOnce)
    {
        Console.Error.WriteLine("⚠ Plaintext secrets store — consider using DPAPI on Windows");
        _hasWarnedOnce = true;
    }
}
```

**Recommendation:**  
1. Make the warning more prominent (use colors, require acknowledgment)
2. Add a config flag to require encrypted storage and fail if not available
3. Consider implementing libsecret (Linux) or Keychain (macOS) support instead of plaintext fallback
4. Document security implications clearly in README/docs

---

#### H-3: Error Messages May Leak Endpoint Information
**File:** `src\ElBruno.Text2Image.Foundry\MaiImage2Generator.cs:196-206`  
**File:** `src\ElBruno.Text2Image.Foundry\Flux2Generator.cs:217-227`

**Issue:** Error messages include the full resolved endpoint URL, which could leak internal infrastructure details.

```csharp
var hint = response.StatusCode == System.Net.HttpStatusCode.NotFound
    ? "\n\nHint: The endpoint URL may be incorrect. MAI-Image-2 uses the MAI API at /mai/v1/images/generations.\n" +
      $"The resolved endpoint was: {_endpoint}\n" +  // <-- Endpoint exposure
      "Ensure you provide either:\n" + 
      "  - A base URL (e.g., https://your-resource.services.ai.azure.com)\n" +
      // ...
```

**Risk:** In production scenarios, error messages could expose internal Azure resource names or network topology.

**Recommendation:**  
1. Make detailed endpoint hints debug-only (check environment variable or config)
2. In production mode, use generic error messages: "Invalid endpoint configuration"
3. Log full details securely (to a log file) but show minimal info to users

---

### Medium (Consider Fixing)

#### M-1: Input Validation - Prompt Length Limits Vary
**Files:**
- `src\ElBruno.Text2Image.Foundry\MaiImage2Generator.cs:24` - MaxPromptLength = 32,000
- `src\ElBruno.Text2Image.Foundry\GptImage1p5Generator.cs:23` - MaxPromptLength = 4,000
- `src\ElBruno.Text2Image.Foundry\GptImage2Generator.cs` - MaxPromptLength = 4,000
- `src\ElBruno.Text2Image.Foundry\Flux2Generator.cs:181` - 1,000 character limit

**Issue:** Inconsistent prompt length validation across generators. The limits are model-specific but not clearly documented at the API surface level.

**Recommendation:**  
1. Add IImageGenerator.GetConstraints() method to expose model-specific limits
2. Validate prompts at the CLI level before calling generators
3. Add clear documentation about per-model limits

---

#### M-2: File Path Operations Lack Validation
**File:** `src\ElBruno.Text2Image\ImageGenerationResult.cs:51-52`  
**File:** `src\ElBruno.Text2Image\ImageGenerationOptions.cs:93`

**Issue:** File operations don't validate paths for directory traversal or other injection attacks.

```csharp
// ImageGenerationResult.cs
var directory = Path.GetDirectoryName(filePath);
if (!string.IsNullOrEmpty(directory))
    Directory.CreateDirectory(directory);
await File.WriteAllBytesAsync(filePath, ImageBytes);
```

**Risk:** If an attacker controls the output path (e.g., via CLI arguments), they could potentially write files outside intended directories.

**Recommendation:**  
1. Validate that output paths are within expected directories
2. Reject paths with `..` segments
3. Use Path.GetFullPath() and compare with allowed base directories
4. Add path sanitization in CLI command parsing

---

#### M-3: HTTP Response Size Not Limited
**File:** `src\ElBruno.Text2Image.Foundry\MaiImage2Generator.cs:228-229`

**Issue:** When fetching images from URLs, response size is not limited, which could lead to memory exhaustion.

```csharp
var imageResponse = await _httpClient.SendAsync(imageRequest, cancellationToken);
imageResponse.EnsureSuccessStatusCode();
imageBytes = await imageResponse.Content.ReadAsByteArrayAsync(cancellationToken);
```

**Recommendation:**  
1. Add max response size check (e.g., 50 MB limit)
2. Stream large responses instead of loading into memory
3. Validate Content-Length header before reading

---

#### M-4: Exception Handling Suppresses Errors in Secret Deletion
**File:** `src\ElBruno.Text2Image.Cli\Secrets\SecretResolver.cs:93-103`

**Issue:** Exceptions during secret deletion are silently suppressed, which could mask security issues.

```csharp
foreach (var store in _stores.Where(s => s.IsAvailable))
{
    try
    {
        await store.DeleteAsync(provider, field, ct);
    }
    catch
    {
        // Ignore failures - field might not exist in this store
    }
}
```

**Recommendation:**  
1. Log suppressed exceptions for debugging
2. Only catch specific exceptions (e.g., FileNotFoundException, KeyNotFoundException)
3. Rethrow unexpected exceptions

---

#### M-5: Temporary File Naming May Be Predictable
**File:** `src\ElBruno.Text2Image.Cli\Secrets\PlainFileSecretStore.cs:126`  
**File:** `src\ElBruno.Text2Image.Cli\Secrets\DpapiSecretStore.cs:148`

**Issue:** Temporary files use predictable naming pattern (`secrets.json.tmp`, `secrets.dpapi.tmp`).

```csharp
var tempPath = _filePath + ".tmp";
```

**Risk:** In a multi-user system, this could lead to race conditions or symlink attacks (though mitigated by file permissions on Unix).

**Recommendation:**  
1. Use Path.GetRandomFileName() for temp files
2. Use File.Replace() atomically (already done, which is good)
3. Ensure temp files have restrictive permissions

---

## Credential & Secrets Management

### ✅ Strong Points:
1. **Multi-Layer Secret Resolution:** Well-designed 5-layer chain (CLI flags → env vars → DPAPI → plaintext → wizard)
2. **DPAPI Encryption on Windows:** Uses Windows Data Protection API for encrypted storage at rest
3. **Unix File Permissions:** Plaintext files restricted to user-only (0600) on Linux/macOS
4. **No API Keys in DefaultRequestHeaders:** API keys sent per-request via headers, not stored in HttpClient
5. **Environment Variable Naming:** Clear convention (`T2I_<PROVIDER>_<FIELD>`)
6. **Atomic File Updates:** Uses File.Replace() to prevent corruption during writes

### ⚠️ Areas for Improvement:
1. **No Credential Rotation:** No built-in mechanism to rotate API keys
2. **Plaintext Fallback:** Should be opt-in, not automatic
3. **No Audit Logging:** Secret access/modification not logged
4. **Missing Key Derivation:** DPAPI used directly without additional KDF layers

### API Key Handling Analysis:
- ✅ **Not hardcoded** - All secrets loaded from external stores
- ✅ **Not logged** - Error messages truncate sensitive data
- ✅ **HTTPS enforced** - All generators validate `https://` protocol
- ✅ **Per-request headers** - API keys not stored in long-lived HttpClient instances
- ⚠️ **Printed in metadata** - Endpoint URLs included in generation results (line 133, 158 in adapters)

---

## HTTP Security

### ✅ Strong Points:
1. **HTTPS Enforcement:** All cloud generators reject non-HTTPS endpoints
   - `MaiImage2Generator.cs:69-70`
   - `GptImage2Generator.cs:53-54`
   - `Flux2Generator.cs:71-72`
2. **Proper Content-Type Headers:** All requests set `application/json` with UTF-8
3. **Content-Length Explicit:** Requests use ByteArrayContent to set Content-Length
4. **Certificate Validation:** Default HttpClient configuration validates certificates
5. **Timeout Configuration:** HttpClients have 5-minute timeouts to prevent hanging

### ⚠️ Potential Issues:
1. **No Certificate Pinning:** For high-security scenarios, consider pinning Azure certs
2. **URL Downloading Images:** MAI-Image-2 may return image URLs instead of base64
   - Mitigation: Separate request without API key (line 226-229, MaiImage2Generator.cs) ✅
3. **No Request Size Limits:** Prompt validation exists, but image download size unchecked
4. **User-Agent Not Set:** Requests don't include a User-Agent header (consider adding for tracking)

### Injection Risk Assessment:
- ✅ **No XXE:** Using System.Text.Json (not XML parsers)
- ✅ **No SQL Injection:** No database queries
- ✅ **No Command Injection:** No shell execution with user input
- ✅ **JSON Serialization Safe:** Using source-generated JsonSerializer contexts

---

## Input Validation

### Prompts:
- ✅ Length validation implemented per model
- ✅ Null/whitespace checks on all inputs
- ⚠️ No content filtering (e.g., injection attempts like prompt injection)
- ⚠️ No sanitization of special characters

### API Responses:
- ✅ JSON deserialization with null checks
- ✅ Error body truncation (MaxErrorBodyLength = 1024 bytes)
- ✅ Base64 validation via Convert.FromBase64String (throws on invalid)
- ⚠️ Response size not limited (could cause OOM)

### File Operations:
- ⚠️ No path traversal checks (`ImageGenerationResult.SaveAsync`)
- ⚠️ No validation that output path is within safe directory
- ⚠️ CLI allows arbitrary output paths via `--out` flag
- ✅ Directory creation safe (uses Path.GetDirectoryName)

### Reference Images (FLUX.2):
- ✅ File extension validation (`.png`, `.jpg`, `.jpeg`, `.webp`)
- ✅ Magic number validation for file formats
- ⚠️ No file size limit check before reading
- ✅ Proper exception handling for missing files

---

## Dependency Security

### Vulnerability Scan Results:
✅ **No known vulnerabilities** in direct or transitive dependencies (as of scan date).

### Key Dependencies:
| Package | Version | Latest | Notes |
|---------|---------|--------|-------|
| **Azure.AI.OpenAI** | 2.1.* | 2.1.x | Up to date, wildcard for patches ✅ |
| **Spectre.Console** | 0.49.1 | 0.49.1 | Up to date ✅ |
| **Spectre.Console.Cli** | 0.49.1 | 0.49.1 | Up to date ✅ |
| **Microsoft.Extensions.Hosting** | 9.0.0 | 9.0.0 | Up to date ✅ |
| **System.Security.Cryptography.ProtectedData** | 9.0.0 | 9.0.0 | Up to date ✅ |
| **Microsoft.ML.OnnxRuntime.Managed** | 1.24.1 | 1.24.1 | Up to date ✅ |
| **ElBruno.HuggingFace.Downloader** | 0.5.0 | ? | External package - review separately |
| **SixLabors.ImageSharp** | 3.* | 3.x | Wildcard for major version ✅ |

### Dependency Management Practices:
- ✅ Version constraints prevent breaking changes
- ✅ Wildcard patches allow security updates (`2.1.*`, `3.*`)
- ⚠️ No automated dependency scanning in CI/CD (consider Dependabot)
- ✅ NuGet package signing enforced by ecosystem

### Supply Chain Risks:
- ⚠️ `ElBruno.HuggingFace.Downloader` is a first-party dependency - ensure it's audited
- ✅ All other packages are from trusted sources (Microsoft, well-known OSS)

---

## Error Handling

### ✅ Good Practices:
1. **Structured Exception Handling:** Specific exception types caught (ArgumentException, HttpRequestException, etc.)
2. **Error Message Truncation:** API errors truncated to 1024 bytes to prevent log injection
3. **Helpful Hints:** Contextual error messages guide users (404 → endpoint hints)
4. **No Stack Trace Leakage:** Production errors don't expose internal details

### ⚠️ Issues:
1. **Endpoint URLs in Errors:** Leak infrastructure details (see H-3)
2. **Suppressed Exceptions:** Secret deletion swallows all exceptions (see M-4)
3. **Generic Catch Blocks:** Some `catch (Exception ex)` blocks too broad
   - Example: `GenerateCommand.cs:168-173`

### Logging Security:
- ✅ No obvious PII logging in reviewed code
- ✅ Error messages don't include API keys
- ⚠️ Prompts are logged/stored in metadata - could contain sensitive content
- ⚠️ No audit logging for secret access/modification

### Example Error Handling Patterns:
```csharp
// GOOD: Specific exception with context
if (prompt.Length > MaxPromptLength)
    throw new ArgumentOutOfRangeException(nameof(prompt), 
        $"Prompt must be {MaxPromptLength} characters or fewer");

// NEEDS IMPROVEMENT: Broad catch
catch (Exception ex)
{
    ConsoleHelpers.PrintError($"Generation failed: {ex.Message}");
    return 1;
}
```

---

## Recommendations

### Priority 1 (Immediate):
1. **Remove or sanitize endpoint URLs from error messages** (H-3)
   - Add debug mode flag for verbose errors
   - Use generic messages in production

2. **Improve plaintext secret storage warning** (H-2)
   - Make it more prominent
   - Consider requiring opt-in via config flag

3. **Add path traversal validation** (M-2)
   - Validate output paths before writing
   - Reject `..` segments

### Priority 2 (Short-term):
4. **Implement response size limits** (M-3)
   - Cap HTTP response bodies at 50 MB
   - Add Content-Length validation

5. **Add audit logging for secrets** (Credential Management)
   - Log secret access (not values)
   - Implement rotation detection

6. **Improve exception handling in secret deletion** (M-4)
   - Catch specific exceptions
   - Log unexpected errors

### Priority 3 (Long-term):
7. **Add Dependabot or similar for automated dependency scanning**
   - Configure GitHub Actions for weekly scans
   - Auto-create PRs for security patches

8. **Implement certificate pinning for critical endpoints** (HTTP Security)
   - Pin Azure service certificates
   - Add validation in HttpClient setup

9. **Add content filtering for prompts** (Input Validation)
   - Detect potential prompt injection attempts
   - Warn users about potentially unsafe prompts

10. **Consider implementing libsecret/Keychain support** (Credential Management)
    - Provide proper OS keychain integration for Linux/macOS
    - Eliminate plaintext fallback

---

## Summary

**Overall Security Posture:** ⭐⭐⭐⭐☆ (4/5 - Good)

The ElBruno.Text2Image project demonstrates **strong security fundamentals** with proper HTTPS enforcement, encrypted credential storage on Windows, and good input validation practices. The multi-layer secret resolution chain is well-designed and follows security best practices.

**Key Strengths:**
- HTTPS-only enforcement for all cloud APIs
- DPAPI encryption for secrets on Windows
- No hardcoded credentials
- Safe JSON serialization (no XXE/injection risks)
- Up-to-date dependencies with no known vulnerabilities

**Main Concerns:**
- Endpoint URLs leak in error messages (information disclosure)
- Plaintext secret fallback needs stronger warnings
- Path traversal validation missing for file operations
- No response size limits for HTTP downloads

**Recommendation:** Address Priority 1 items before next release, particularly the endpoint exposure issue. The project is production-ready from a security standpoint but would benefit from the recommended improvements.

---

**Next Steps:**
1. Fix H-3 (endpoint exposure) in error messages
2. Enhance plaintext secret warning (H-2)
3. Add path validation (M-2)
4. Schedule security audit cadence (quarterly recommended)
5. Enable Dependabot for automated dependency monitoring

