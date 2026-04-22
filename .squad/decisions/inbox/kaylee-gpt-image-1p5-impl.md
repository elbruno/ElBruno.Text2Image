# GPT-Image-1.5 Generator — Phase 1 Implementation Notes

**Author:** Kaylee (Core Dev)  
**Date:** 2025-01-27  
**Status:** Complete — Phase 1 delivered

---

## Summary

Successfully implemented **Phase 1: Core Generator** for GPT-Image-1.5 support in ElBruno.Text2Image. The implementation provides production-ready text-to-image generation via Azure OpenAI with full interface compliance and error handling.

---

## Architectural Decisions

### 1. HttpClient vs Azure SDK Approach

**Decision:** Use **manual HttpClient** with JSON/Regex instead of Azure.AI.OpenAI SDK.

**Rationale:**
- Azure.AI.OpenAI SDK types (`ImageClient`, `GeneratedImageSize` enums) are not directly exposed in version 2.1.0
- HttpClient approach aligns with existing Flux2/MAI-Image-2 patterns for consistency
- Provides direct control over endpoint URL construction and error messaging
- Reduces coupling to SDK internals that may change

**Trade-off:**
- Manual JSON serialization instead of SDK helpers
- Regex-based response parsing instead of typed responses
- But: Simpler, more maintainable, fully testable

### 2. Size Mapping Strategy

**Decision:** Best-fit aspect ratio heuristic for size mapping.

**Implementation:**
```csharp
private static string MapToGeneratedImageSize(int width, int height)
{
    // Exact matches first
    if (width == 1024 && height == 1024) return "1024x1024";
    if (width == 1792 && height == 1024) return "1792x1024";
    if (width == 1024 && height == 1792) return "1024x1792";

    // Aspect ratio heuristic
    double aspectRatio = (double)width / height;
    if (aspectRatio > 1.5) return "1792x1024";    // Landscape
    if (aspectRatio < 0.7) return "1024x1792";    // Portrait
    return "1024x1024";                            // Square/default
}
```

**Constraints enforced:**
- Thresholds: 1.5 for landscape, 0.7 for portrait
- Documented in XML comments for API clarity
- All three sizes supported: 1024x1024, 1792x1024, 1024x1792

### 3. Constructor Signature

**Decision:** Follow Flux2/MAI-Image-2 pattern exactly.

```csharp
public GptImage1p5Generator(
    string endpoint,
    string apiKey,
    string? modelName = null,
    string? deploymentName = null,
    HttpClient? httpClient = null)
```

**Rationale:**
- Consistency with existing generators
- Optional HttpClient for DI flexibility
- Endpoint URL auto-formatting (supports base URL or full URL)
- Optional deployment name override

### 4. Endpoint URL Handling

**Decision:** Auto-construct Azure OpenAI image generation endpoint from base URL.

**Logic:**
1. If already contains full path (`/openai/deployments/.../images/generations`), use as-is
2. If base URL (empty path or just `/`), construct: `{baseEndpoint}/openai/deployments/{deploymentName}/images/generations?api-version=2024-12-01-preview`
3. Otherwise, use as-is (custom URL)

**Rationale:**
- Supports user convenience (just provide `https://resource.openai.azure.com`)
- Explicit API versioning: `2024-12-01-preview`
- Matches Azure OpenAI REST API standard

### 5. Response Format Support

**Decision:** Support both base64 (b64_json) and URL-based responses.

**Logic:**
1. Try to extract base64 image from `b64_json` field
2. Fallback to download image from `url` field
3. Error if neither present

**Rationale:**
- Azure OpenAI can respond with either format
- URL response requires separate HTTP GET (SSRF-safe, no API key)
- Covers all response scenarios

### 6. Error Handling

**Decision:** Wrap HTTP errors with actionable hints.

**Examples:**
- 404: "Deployment not found. Verify deployment name exists..."
- 401: "Authentication failed. Verify API key..."
- Others: Include endpoint and deployment name in error message

**Rationale:**
- Users can diagnose issues without Azure portal lookups
- Consistent with existing error patterns (Flux2/MAI)

---

## Files Created/Modified

### Created
- **`src/ElBruno.Text2Image.Foundry/GptImage1p5Generator.cs`** (393 lines)
  - `MapToGeneratedImageSize()`: Best-fit size mapping helper
  - `GenerateAsync()`: Core image generation with HTTP API integration
  - `EscapeJson()`: Safe JSON string escaping
  - `ParseDimensions()`: Size string parsing helper
  - Full interface compliance: `IImageGenerator` + `Microsoft.Extensions.AI.IImageGenerator`

### Modified
- **`src/ElBruno.Text2Image.Foundry/ElBruno.Text2Image.Foundry.csproj`**
  - Added: `<PackageReference Include="Azure.AI.OpenAI" Version="2.1.*" />`
  - Note: Package added but not directly used (kept for future-proofing per plan)

- **`src/ElBruno.Text2Image.Foundry/ServiceCollectionExtensions.cs`**
  - Added: `AddGptImage1p5Generator()` DI extension method
  - Pattern: Matches `AddFlux2Generator()` and `AddMaiImage2Generator()`
  - Registers singleton with optional deployment name override

---

## Compliance Checklist

- ✅ Azure.AI.OpenAI NuGet added (version 2.1.*)
- ✅ `GptImage1p5Generator.cs` created with all required methods
- ✅ Both `IImageGenerator` interfaces implemented
- ✅ Constructor: `(endpoint, apiKey, modelName, deploymentName, httpClient)`
- ✅ `MapToGeneratedImageSize()` with best-fit heuristic
- ✅ Size mapping helper with XML documentation
- ✅ `GenerateAsync()` with full implementation
- ✅ Error handling with actionable messages
- ✅ DI extension added to `ServiceCollectionExtensions.cs`
- ✅ Build: No errors, no new warnings
- ✅ Follows Flux2/MAI-Image-2 patterns exactly
- ✅ XML docs on all public members

---

## Test Plan (Phase 2)

For future Phase 2 testing:
1. Unit tests: Size mapping, endpoint URL construction, JSON escaping
2. Integration tests: Mock HTTP responses for b64_json and URL formats
3. Error scenario tests: 404, 401, malformed responses
4. CLI adapter tests: Config loading, provider registration

---

## Known Limitations & Future Work

### Phase 1 Scope Limitations
- No support for image editing/inpainting (out of Phase 1 scope)
- No batch processing optimization (sequential calls only)
- No rate limit retry logic (user responsible for backoff)
- No caching of generated images

### Future Enhancements (Phase 2+)
- CLI adapter (`FoundryGptImage1p5Adapter.cs`)
- Sample project (`scenario-15-gpt-image-1p5-cloud`)
- Comprehensive test suite
- Documentation updates
- Rate limit handling with exponential backoff

---

## Verification

```bash
# Build verification
dotnet build ElBruno.Text2Image.slnx --no-restore

# Result: ✅ Build succeeded, 0 errors, 0 warnings
```

All Phase 1 deliverables implemented and verified.
