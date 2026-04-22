# GPT-Image-1.5 Integration Technical Assessment

**Author:** River (AI/ML Specialist)  
**Date:** 2026-04-25  
**Status:** Assessment  
**Requested by:** Bruno Capuano

---

## Executive Summary

GPT-Image-1.5 (Azure OpenAI DALL-E 3 generation) is a state-of-the-art enterprise image generation model accessible via Azure OpenAI Service. Integration requires the **Azure.AI.OpenAI** SDK (.NET) which provides a distinct approach from our existing Foundry generators (Flux2, MAI-Image-2). This assessment covers model capabilities, SDK patterns, configuration requirements, and integration considerations.

---

## 1. Model Capabilities

### 1.1 Image Generation Features

**Core Capabilities:**
- **Text-to-Image Generation:** High-quality images from natural language prompts with strong prompt adherence and visual fidelity
- **Image-to-Image Editing:** Modify, enhance, and iteratively refine existing images using textual instructions
- **Inpainting & Region-Specific Edits:** Target specific image regions for edits (background swaps, object removal, color changes)
- **High Visual Fidelity:** Superior rendering of complex scenes, facial likeness, lighting, and branded elements
- **Fast Generation:** Up to 4x faster than previous DALL-E generations

**Quality Characteristics:**
- **Prompt Alignment:** Exceptional adherence to prompt details and user intent
- **Face Preservation:** Maintains facial likeness and identity across edits
- **Visual Consistency:** Reliable color tone, lighting, and style application
- **Content Safety:** Built-in Azure content safety filters and customizable policies
- **Provenance Tracking:** C2PA metadata for transparency

### 1.2 Supported Image Dimensions

**Available Sizes:**
- **1024 × 1024 pixels** (square) — primary format
- **1024 × 1792 pixels** (portrait)
- **1792 × 1024 pixels** (landscape)

**Constraints:**
- Fixed size options only — **no custom dimensions**
- Must select from the three presets above
- Default: 1024×1024

**Note:** This differs significantly from our existing generators:
- **Flux2:** Flexible dimensions (512×512 default, can specify arbitrary sizes)
- **MAI-Image-2:** Flexible with constraints (≥768px per dimension, ≤1M total pixels, 1024×1024 default)

### 1.3 Prompt Formats & Best Practices

**Prompt Handling:**
- **Natural language:** Supports conversational, detailed prompts
- **No explicit token limit documented** in search results, but standard practice suggests <1000 words for optimal results
- **Prompt revision:** GPT-Image may internally revise prompts for safety/quality (revised prompt returned in response metadata)
- **Style specifications:** Supports art style, medium, lighting, perspective, mood descriptors

**Best Practices:**
- Be specific about composition, colors, lighting, and subject details
- Use natural language over comma-separated keywords
- Reference specific art styles/mediums for better results (e.g., "oil painting," "photorealistic," "watercolor")
- For product/commercial use, specify branded elements explicitly

### 1.4 Batch Processing & Rate Limits

**Batch Processing:**
- **No native batch API** — images generated one at a time (unlike OpenAI's Batch API for text models)
- Batch workflows require sequential or parallel client-side orchestration

**Rate Limits (Azure OpenAI):**
- **Requests per minute (RPM):** Tier-dependent (default ~20 for image models, varies by subscription)
- **Images per minute:** Default ~20 images/minute (check Azure Portal quotas for exact limits)
- **Concurrent requests:** Limited by RPM quota
- **Quota increases:** Available via Azure Portal support requests

**Rate Limit Handling:**
- HTTP 429 (Too Many Requests) responses when limit exceeded
- Recommended: Implement exponential backoff with jitter
- Monitor Azure Portal "Quotas" pane for usage and limits

### 1.5 Response Formats

**API Response:**
- **Image data format:** Base64-encoded PNG (via `b64_json` field) or URL (temporary download link)
- **Metadata:** Includes timestamp (`created`), revised prompt (if modified), and generation parameters
- **No raw binary stream** — always wrapped in JSON response

**Response Pattern (JSON):**
```json
{
  "created": 1234567890,
  "data": [
    {
      "b64_json": "<base64-encoded-png>",
      "url": "https://...", 
      "revised_prompt": "Optional: revised prompt for safety/quality"
    }
  ]
}
```

**Comparison with Existing Generators:**
- **Flux2:** Same pattern (base64/URL, JSON response, synchronous/async modes)
- **MAI-Image-2:** Same pattern (base64/URL, JSON response, synchronous only)
- **GPT-Image-1.5:** Synchronous only (no 202 polling), base64 or URL

---

## 2. Azure OpenAI SDK Integration (.NET)

### 2.1 SDK Architecture

**Package:** `Azure.AI.OpenAI` (NuGet)  
**Latest Version:** 2.8.0-beta.1 (as of search results)  
**Target Frameworks:** .NET Standard 2.0, .NET 8.0+  
**Compatibility:** Full support for .NET 8, .NET 10 (preview tested)

**Key Classes:**
```csharp
using Azure.AI.OpenAI;
using Azure;
using OpenAI.Images;

// 1. Create Azure OpenAI client
var client = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey)
);

// 2. Get ImageClient for specific deployment
ImageClient imageClient = client.GetImageClient(deploymentName);

// 3. Generate image
GeneratedImage result = await imageClient.GenerateImageAsync(
    prompt,
    new ImageGenerationOptions
    {
        Size = GeneratedImageSize.Size1024x1024
    }
);
```

### 2.2 ImageClient Class

**Methods:**
- `GenerateImageAsync(string prompt, ImageGenerationOptions options)` — primary generation method
- `GenerateImageEditAsync(...)` — edit existing images
- `GenerateImageVariationsAsync(...)` — create variations of input images

**Return Types:**
- Returns `GeneratedImage` (single image result)
- Contains: `ImageUri` (temp URL) or `ImageBytes` (BinaryData), `RevisedPrompt`

### 2.3 GeneratedImageSize Enum

**Available Values:**
```csharp
public enum GeneratedImageSize
{
    Size256x256,    // Legacy (DALL-E 2)
    Size512x512,    // Legacy (DALL-E 2)
    Size1024x1024,  // DALL-E 3 / GPT-Image-1.5 (square)
    Size1024x1792,  // DALL-E 3 / GPT-Image-1.5 (portrait) — check SDK version support
    Size1792x1024   // DALL-E 3 / GPT-Image-1.5 (landscape) — check SDK version support
}
```

**Note:** Older SDK versions (pre-2.8.0) may only expose `Size256x256`, `Size512x512`, `Size1024x1024`. Verify enum values in actual SDK version used.

### 2.4 Async Patterns

**API Behavior:**
- `GenerateImageAsync()` is **synchronous-waiting** — no 202 polling, request blocks until image ready
- Typical latency: 10-30 seconds for 1024×1024 images
- **No async polling required** (unlike Flux2's 202 mode)

**HttpClient Management:**
- `AzureOpenAIClient` internally manages HttpClient
- **Do not** wrap in `using` if reusing client across requests
- For DI scenarios: Register as singleton or scoped service with `IHttpClientFactory` backing

**Cancellation:**
- Full `CancellationToken` support on all async methods
- Recommended timeout: 60-120 seconds for generation calls

### 2.5 Error Handling Patterns

**Exception Types:**
- `RequestFailedException` (Azure SDK standard) — wraps HTTP errors
- `Azure.RequestFailedException` properties: `Status` (HTTP code), `Message`, `ErrorCode`

**Common Error Codes:**
- `401` — Authentication failure (invalid API key or Entra ID token)
- `404` — Deployment not found (check deployment name)
- `429` — Rate limit exceeded (implement retry with backoff)
- `400` — Invalid request (prompt too long, unsupported size, content policy violation)

**Retry Logic:**
- Azure SDK has **built-in retry** for transient failures (503, 429 with Retry-After)
- Customize via `AzureOpenAIClientOptions.Retry` policy
- Recommended: Use SDK defaults, add application-level retry for 429s with exponential backoff

**Error Handling Pattern:**
```csharp
try
{
    var result = await imageClient.GenerateImageAsync(prompt, options);
}
catch (RequestFailedException ex) when (ex.Status == 429)
{
    // Rate limit — wait and retry
    await Task.Delay(TimeSpan.FromSeconds(5));
    // Retry logic
}
catch (RequestFailedException ex) when (ex.Status == 400)
{
    // Invalid request — check prompt/params
    throw new InvalidOperationException($"Image generation failed: {ex.Message}", ex);
}
catch (RequestFailedException ex)
{
    // General Azure API error
    throw new HttpRequestException($"Azure OpenAI error ({ex.Status}): {ex.Message}", ex);
}
```

### 2.6 Version Compatibility

**Azure.AI.OpenAI Package:**
- Requires: .NET Standard 2.0+ or .NET 8.0+
- **No conflicts** with existing packages (System.ClientModel, OpenAI are separate namespaces)
- **Current project targets:** net8.0;net10.0 (multi-target) ✅ Compatible

**Dependencies:**
- `Azure.Core` (common Azure SDK dependency)
- `System.ClientModel` (shared Azure primitives)
- No direct OpenAI package dependency (Azure wrapper is standalone)

**Breaking Changes Risk:**
- Azure SDK uses semantic versioning
- Beta versions (2.8.0-beta.1) may have API changes before GA
- Recommend: Pin to stable version once available, or lock beta version in .csproj

---

## 3. Configuration & Authentication

### 3.1 ApiKeyCredential Pattern

**Authentication Method:**
```csharp
var credential = new AzureKeyCredential(apiKey);
var client = new AzureOpenAIClient(new Uri(endpoint), credential);
```

**Secure Storage Best Practices:**
- **Development:** User Secrets (`dotnet user-secrets set`)
- **CI/CD:** Environment variables
- **Production:** Azure Key Vault with Managed Identity
- **Never:** Hardcode in source, commit to Git, or log API keys

**Key Rotation:**
- Azure OpenAI supports dual keys (primary/secondary)
- Rotate keys via Azure Portal → OpenAI resource → Keys and Endpoint
- Use secondary key during rotation to avoid downtime

### 3.2 Endpoint Format & Validation

**Endpoint Structure:**
```
https://<resource-name>.openai.azure.com/
```

**Differences from Foundry Generators:**
- **GPT-Image-1.5:** Uses `.openai.azure.com` (Azure OpenAI Service)
- **Flux2/MAI-Image-2:** Use `.services.ai.azure.com` (Microsoft Foundry / AI Services)

**Validation:**
- Must be HTTPS
- Must match region of Azure OpenAI deployment
- SDK appends API path automatically — provide base URL only

### 3.3 Deployment Name Significance

**What is a Deployment Name?**
- User-defined identifier for a specific model instance in Azure OpenAI resource
- Maps to a model version (e.g., "dall-e-3", "gpt-image-1.5")
- Required parameter for `GetImageClient(deploymentName)`

**Deployment Name Patterns:**
- **No standard naming:** User chooses any alphanumeric name
- Common patterns: `"dalle3"`, `"image-gen"`, `"gpt-image-1.5"`
- **Does NOT vary by region** — user-defined per resource

**Configuration Strategy:**
- Store deployment name in configuration (user secrets, env vars, appsettings.json)
- Convention: `OPENAI_IMAGE_DEPLOYMENT_NAME` or `GPT_IMAGE_DEPLOYMENT`

### 3.4 Alternative Authentication Methods

**Managed Identity (Entra ID/Azure AD):**
```csharp
using Azure.Identity;

var credential = new DefaultAzureCredential();
var client = new AzureOpenAIClient(new Uri(endpoint), credential);
```

**When to Use:**
- Azure-hosted applications (App Service, Functions, Container Apps)
- Eliminates secret management (no API keys to rotate)
- Recommended for production

**Setup:**
1. Enable Managed Identity on Azure resource (System or User Assigned)
2. Grant identity "Cognitive Services OpenAI User" role on OpenAI resource
3. Use `DefaultAzureCredential()` in code (auto-discovers managed identity)

**Token Refresh:**
- SDK handles token refresh automatically
- Default token lifetime: 1 hour
- No application-level refresh logic needed

---

## 4. Comparison with Existing Generators

### 4.1 Comparison Matrix

| **Aspect**              | **Flux2 (BFL)**                  | **MAI-Image-2**                   | **GPT-Image-1.5 (Azure OpenAI)** |
|-------------------------|----------------------------------|-----------------------------------|----------------------------------|
| **Provider**            | Microsoft Foundry (BFL Native API) | Microsoft Foundry (MAI API)     | Azure OpenAI Service             |
| **Endpoint Domain**     | `.services.ai.azure.com`         | `.services.ai.azure.com`          | `.openai.azure.com`              |
| **Default Size**        | 512×512                          | 1024×1024                         | 1024×1024                        |
| **Size Flexibility**    | ✅ Arbitrary dimensions          | ⚠️ ≥768px, ≤1M pixels             | ❌ 3 fixed presets only          |
| **Supported Sizes**     | Any (512×512 default)            | 768+ dimensions (1024×1024 default) | 1024×1024, 1024×1792, 1792×1024 |
| **Generation Speed**    | ~10-30s (async mode)             | ~5-20s (sync)                     | ~10-30s (sync)                   |
| **API Pattern**         | Sync (200) or Async (202+poll)   | Sync (200) only                   | Sync (200) only                  |
| **SDK**                 | Direct HTTP (`HttpClient`)       | Direct HTTP (`HttpClient`)        | Azure.AI.OpenAI (.NET SDK)       |
| **Auth Method**         | API Key (custom header)          | API Key (custom header)           | Azure Key Credential or Entra ID |
| **Quality Focus**       | Photorealistic, text-in-image    | High resolution, stability        | Prompt adherence, artistic range |
| **Cost**                | Variable (Foundry pricing)       | Variable (Foundry pricing)        | Azure OpenAI consumption-based   |
| **Content Safety**      | Provider-level                   | Provider-level                    | Azure Content Safety filters     |
| **Inpainting**          | ❌ Not supported                 | ❌ Not supported                  | ✅ Supported (editing API)       |
| **Provenance**          | ❌ No metadata                   | ❌ No metadata                    | ✅ C2PA metadata                 |

### 4.2 Performance Comparison

**Expected Latency:**
- **Flux2:** 10-30s (512×512), up to 60s for larger images
- **MAI-Image-2:** 5-20s (1024×1024), fast synchronous response
- **GPT-Image-1.5:** 10-30s (1024×1024), comparable to Flux2

**Quality vs. Speed Trade-offs:**
- **Flux2:** Best for photorealism and text rendering (slower)
- **MAI-Image-2:** Best for high-res, stable outputs (fastest)
- **GPT-Image-1.5:** Best for creative prompt interpretation, artistic range (moderate speed)

### 4.3 Error Handling & Resilience

**Flux2:**
- Custom HTTP error parsing (JSON error bodies)
- Supports both sync and async (202 polling) modes
- Retry logic: Manual (no SDK-level retry)

**MAI-Image-2:**
- Custom HTTP error parsing (JSON error bodies)
- Synchronous only (no polling complexity)
- Retry logic: Manual (no SDK-level retry)

**GPT-Image-1.5:**
- Azure SDK exceptions (`RequestFailedException`)
- Built-in retry for transient failures (503, 429 with Retry-After)
- Structured error codes and messages (easier to handle)

**Resilience Ranking:** GPT-Image-1.5 > MAI-Image-2 > Flux2 (due to SDK retry support)

### 4.4 Code Pattern Comparison

**Flux2 Sample (Current):**
```csharp
using var generator = new Flux2Generator(endpoint, apiKey, modelId: "FLUX.2-pro");
var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
{
    Width = 512,
    Height = 512,
    NumInferenceSteps = 20
});
await result.SaveAsync(outputPath);
```

**MAI-Image-2 Sample (Current):**
```csharp
using var generator = new MaiImage2Generator(endpoint, apiKey, modelId: "mai-image-2");
var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
{
    Width = 1024,
    Height = 1024
});
await result.SaveAsync(outputPath);
```

**GPT-Image-1.5 Sample (Proposed):**
```csharp
// Option 1: Direct Azure SDK usage (no IImageGenerator wrapper)
var client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
var imageClient = client.GetImageClient(deploymentName);
var result = await imageClient.GenerateImageAsync(prompt, new ImageGenerationOptions
{
    Size = GeneratedImageSize.Size1024x1024
});
// Convert result.ImageUri or result.ImageBytes to ImageGenerationResult

// Option 2: Wrapper class implementing IImageGenerator (for consistency)
using var generator = new GptImage15Generator(endpoint, apiKey, deploymentName);
var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
{
    Width = 1024,
    Height = 1024
});
await result.SaveAsync(outputPath);
```

**Shared Patterns:**
- All use `HttpClient` (directly or via SDK)
- All return `ImageGenerationResult` (via `IImageGenerator` interface)
- All support cancellation tokens
- All serialize to JSON (Flux2/MAI via source-gen, GPT via Azure SDK)

**Key Differences:**
- **GPT-Image-1.5 uses Azure SDK** (not direct HttpClient)
- **Size specification:** Enum vs. Width/Height integers
- **Deployment name required** for GPT-Image-1.5 (vs. modelId for Flux2/MAI)

---

## 5. Integration Considerations

### 5.1 NuGet Package Requirements

**New Package:**
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.8.0-beta.1" />
```

**Dependency Tree:**
- `Azure.Core` (required by all Azure SDKs)
- `System.ClientModel` (Azure SDK primitives)
- `System.Text.Json` (already used in project)

**Conflicts:**
- ✅ **No conflicts** with existing packages
- `ElBruno.Text2Image.Foundry` uses `System.Text.Json` source generation (compatible)
- Azure SDK uses separate namespace (`Azure.AI.OpenAI` vs. `ElBruno.Text2Image.Foundry`)

### 5.2 Target Framework Compatibility

**Current Project Targets:** `net8.0;net10.0`  
**Azure.AI.OpenAI SDK:** Supports .NET Standard 2.0, .NET 8.0+  
**Verdict:** ✅ **Fully compatible** (no TFM changes needed)

**Recommendation:** Multi-target library package (`ElBruno.Text2Image.OpenAI`) should continue `net8.0;net10.0` pattern.

### 5.3 Serialization Strategy

**Existing Pattern (Flux2/MAI):**
- Source-generated JSON contexts (`[JsonSerializable]`)
- Custom request/response DTOs
- `ByteArrayContent` for explicit Content-Length headers

**Azure SDK Approach:**
- Built-in serialization (System.Text.Json under the hood)
- No custom DTOs needed (SDK provides classes)
- Serialization abstracted by SDK

**Integration Strategy:**
- **For GPT-Image-1.5:** Use SDK serialization (no custom JSON context needed)
- **For consistency:** Map SDK `GeneratedImage` → `ImageGenerationResult` in wrapper class

### 5.4 Async/Await Pattern Alignment

**Existing Generators:**
- `Task<ImageGenerationResult> GenerateAsync(..., CancellationToken)`
- Flux2 supports async polling (202 → poll → 200)
- MAI-Image-2 synchronous only (200)

**GPT-Image-1.5:**
- `Task<GeneratedImage> GenerateImageAsync(..., CancellationToken)`
- Synchronous-waiting (no 202 polling)
- Aligns with MAI-Image-2 pattern

**Alignment:** ✅ No conflicts. GPT-Image-1.5 fits existing `IImageGenerator` interface.

### 5.5 HttpClient Lifecycle Management

**Existing Pattern:**
- Generators accept optional `HttpClient` via constructor
- Generators track ownership (`_ownsHttpClient` flag)
- If not provided, create internal `HttpClient` with `Dispose()` responsibility

**Azure SDK Pattern:**
- `AzureOpenAIClient` manages internal `HttpClient`
- Does **not** accept external `HttpClient` in constructor
- `HttpClientTransport` can be configured via `AzureOpenAIClientOptions` for advanced scenarios

**Recommendation for Wrapper:**
```csharp
public sealed class GptImage15Generator : IImageGenerator
{
    private readonly AzureOpenAIClient _client;
    private readonly ImageClient _imageClient;
    private readonly bool _ownsClient;

    public GptImage15Generator(string endpoint, string apiKey, string deploymentName)
    {
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _imageClient = _client.GetImageClient(deploymentName);
        _ownsClient = true;
    }

    public void Dispose()
    {
        // Azure SDK clients are not IDisposable — no disposal needed
        // HttpClient is managed internally by SDK
    }
}
```

### 5.6 Configuration Structure

**Proposed Config Pattern (align with Flux2/MAI):**
```json
{
  "Providers": {
    "openai-image": {
      "Endpoint": "https://your-resource.openai.azure.com",
      "Deployment": "dalle3"
    }
  }
}
```

**Secret Storage:**
```bash
# User Secrets
dotnet user-secrets set openai-image:apiKey "your-api-key"

# Or environment variable
OPENAI_IMAGE_API_KEY=your-api-key
```

**CLI Adapter Registration:**
```csharp
public string Id => "openai-image";
public string DisplayName => "GPT-Image-1.5 (Azure OpenAI)";
public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "deployment" };
```

---

## 6. Testing & Validation

### 6.1 Mocking Strategy

**Challenge:** Azure SDK classes (`AzureOpenAIClient`, `ImageClient`) are **not interfaces**.

**Options:**

**Option 1: Wrapper Interface (Recommended)**
```csharp
public interface IGptImage15Generator : IImageGenerator { }

public sealed class GptImage15Generator : IGptImage15Generator
{
    // Wraps AzureOpenAIClient
}

// Test with mock:
var mockGenerator = new Mock<IGptImage15Generator>();
```

**Option 2: HTTP-Level Mocking (Existing Pattern)**
```csharp
// Not applicable — Azure SDK abstracts HTTP layer
// Cannot intercept HttpClient like Flux2/MAI tests
```

**Option 3: Integration Tests Only**
- Test against live Azure OpenAI endpoint (dev/test deployment)
- Use budget-limited test deployments
- Mark tests as `[Trait("Category", "Integration")]`

**Recommendation:**
- **Unit tests:** Wrapper interface + mocks (test request validation, size mapping)
- **Integration tests:** Live Azure OpenAI (test actual image generation, error handling)
- No HTTP-level mocking (Azure SDK internals are sealed)

### 6.2 Integration Test Considerations

**Rate Limits:**
- Default ~20 requests/minute (Azure OpenAI)
- Integration tests should be rate-limited or run sequentially
- Use `[Fact(Skip = "...")]` or `[Trait("Category", "Integration")]` to separate from CI

**Cost:**
- ~$0.04-$0.08 per 1024×1024 image (pricing varies)
- Budget impact: 100 test runs = $4-$8
- Recommend: Limit integration tests to <10 images per PR

**Test Deployment Strategy:**
- Create dedicated "test" deployment in Azure OpenAI
- Use separate resource for CI/CD (with cost alerts)
- Configure low quota (e.g., 10 RPM) to prevent runaway costs

### 6.3 Output Validation

**Dimensions:**
```csharp
[Fact]
public async Task GenerateAsync_Returns1024x1024Image()
{
    var result = await generator.GenerateAsync(prompt, new ImageGenerationOptions
    {
        Width = 1024,
        Height = 1024
    });
    
    Assert.Equal(1024, result.Width);
    Assert.Equal(1024, result.Height);
}
```

**Metadata:**
```csharp
[Fact]
public async Task GenerateAsync_ReturnsValidMetadata()
{
    var result = await generator.GenerateAsync(prompt);
    
    Assert.NotNull(result.ImageBytes);
    Assert.True(result.ImageBytes.Length > 0);
    Assert.NotNull(result.ModelName);
    Assert.Equal("GPT-Image-1.5", result.ModelName);
    Assert.True(result.InferenceTimeMs > 0);
}
```

**PNG Validation:**
```csharp
[Fact]
public async Task GenerateAsync_ReturnsValidPNG()
{
    var result = await generator.GenerateAsync(prompt);
    
    // PNG magic bytes: 0x89 0x50 0x4E 0x47
    Assert.Equal(0x89, result.ImageBytes[0]);
    Assert.Equal(0x50, result.ImageBytes[1]);
    Assert.Equal(0x4E, result.ImageBytes[2]);
    Assert.Equal(0x47, result.ImageBytes[3]);
}
```

### 6.4 Error Scenario Testing

**Required Tests:**
```csharp
[Fact]
public async Task GenerateAsync_InvalidAPIKey_ThrowsRequestFailedException()
{
    var generator = new GptImage15Generator(endpoint, "invalid-key", deployment);
    await Assert.ThrowsAsync<RequestFailedException>(() => 
        generator.GenerateAsync("test"));
}

[Fact]
public async Task GenerateAsync_InvalidDeployment_ThrowsRequestFailedException()
{
    var generator = new GptImage15Generator(endpoint, apiKey, "nonexistent");
    var ex = await Assert.ThrowsAsync<RequestFailedException>(() => 
        generator.GenerateAsync("test"));
    Assert.Equal(404, ex.Status);
}

[Fact]
public async Task GenerateAsync_ContentPolicyViolation_ThrowsRequestFailedException()
{
    var ex = await Assert.ThrowsAsync<RequestFailedException>(() => 
        generator.GenerateAsync("prohibited content example"));
    Assert.Equal(400, ex.Status);
}
```

---

## 7. Concerns & Gotchas

### 7.1 Size Constraint Rigidity

**Issue:** GPT-Image-1.5 only supports 3 fixed sizes (1024×1024, 1024×1792, 1792×1024).

**Impact:**
- Breaks consistency with Flux2/MAI (which accept arbitrary dimensions)
- CLI users expecting 512×512 default will get 1024×1024 (4x more pixels, likely higher cost)
- Adapter must map user requests to nearest supported size

**Mitigation:**
- Document size constraints clearly
- Adapter auto-selects closest size (e.g., 512×512 → 1024×1024) with user notification
- Reject unsupported aspect ratios (e.g., 800×600) with actionable error message

### 7.2 Deployment Name Dependency

**Issue:** GPT-Image-1.5 requires a user-defined deployment name (not standardized like `"FLUX.2-pro"`).

**Impact:**
- Configuration is per-user (Bruno's deployment name ≠ other users' names)
- Cannot provide universal sample code with deployment name
- Setup friction higher than Flux2/MAI (must create deployment in Azure Portal)

**Mitigation:**
- CLI adapter requires `deployment` field in config (like `model` for Flux2/MAI)
- Documentation includes step-by-step Azure Portal deployment creation
- Error messages hint at deployment name mismatch on 404s

### 7.3 Azure SDK Beta Version Risk

**Issue:** Latest Azure.AI.OpenAI version is `2.8.0-beta.1` (not stable GA).

**Impact:**
- API surface may change before GA release
- Breaking changes possible in future beta versions
- NuGet package feed may have pre-release visibility issues

**Mitigation:**
- Pin exact version in .csproj: `<PackageReference Include="Azure.AI.OpenAI" Version="2.8.0-beta.1" />`
- Monitor Azure SDK release notes for GA timeline
- Add CI test job to detect breaking changes on SDK updates
- Consider stable 2.7.x version if 2.8.0-beta.1 unstable (check NuGet for latest stable)

### 7.4 No Async Polling Support

**Issue:** GPT-Image-1.5 API is synchronous-waiting only (no 202 + polling like Flux2).

**Impact:**
- Request holds open for 10-30 seconds during generation
- No progress updates during generation (unlike Flux2 polling mode)
- Timeout configuration critical (must be >30s)

**Mitigation:**
- Set generous HttpClient timeout (60-120s recommended)
- Document expected wait times in user guides
- Not a blocker (MAI-Image-2 also synchronous-only)

### 7.5 Cost Per Image

**Issue:** Azure OpenAI consumption-based pricing (per-image cost).

**Impact:**
- 1024×1024 image: ~$0.04-$0.08 (varies by region)
- Higher than local models (free after download)
- Comparable to Flux2/MAI (cloud pricing similar)

**Mitigation:**
- Document pricing in user guides
- CLI should warn on batch operations (cost × count)
- Azure Cost Management alerts recommended for production

### 7.6 Endpoint Domain Mismatch

**Issue:** GPT-Image-1.5 uses `.openai.azure.com` while Flux2/MAI use `.services.ai.azure.com`.

**Impact:**
- Cannot auto-convert endpoints between providers (like Flux2/MAI do)
- Users with Foundry deployments cannot reuse endpoint for GPT-Image
- Requires separate Azure OpenAI resource (not just different deployment)

**Mitigation:**
- Document clearly: "Azure OpenAI" ≠ "Microsoft Foundry"
- CLI error messages distinguish endpoint types
- Config structure separates providers clearly

---

## 8. Recommendations

### 8.1 Implementation Path

**Phase 1: Core Generator (Week 1)**
- [ ] Create `ElBruno.Text2Image.OpenAI` project (new package)
- [ ] Implement `GptImage15Generator : IImageGenerator`
- [ ] Map Azure SDK `GeneratedImage` → `ImageGenerationResult`
- [ ] Size enum → Width/Height mapping (1024×1024, 1024×1792, 1792×1024)
- [ ] Error handling: `RequestFailedException` → `HttpRequestException`
- [ ] Unit tests (mocked via wrapper interface)

**Phase 2: CLI Integration (Week 1)**
- [ ] CLI adapter: `OpenAIImageAdapter : IProviderAdapter`
- [ ] Config schema: `endpoint`, `deployment` fields
- [ ] Secret resolution: `apiKey` via SecretResolver
- [ ] Health check: Test deployment connectivity
- [ ] Documentation: Setup guide (Azure Portal → deployment creation)

**Phase 3: Testing & Validation (Week 2)**
- [ ] Integration tests against dev Azure OpenAI deployment
- [ ] CLI smoke test: `t2i gen --provider openai-image "test"`
- [ ] Cost tracking: Monitor test deployment usage
- [ ] Sample code: `scenario-15-gpt-image-openai`

**Phase 4: Documentation (Week 2)**
- [ ] Setup guide: `docs/gpt-image-setup-guide.md`
- [ ] Comparison table: Update README with GPT-Image row
- [ ] Blog post: "Adding GPT-Image-1.5 to t2i CLI"

### 8.2 Acceptance Criteria

**Core Generator:**
- ✅ Implements `IImageGenerator` interface
- ✅ Supports all 3 size presets (1024×1024, 1024×1792, 1792×1024)
- ✅ Handles 429 rate limits with retry
- ✅ Returns valid `ImageGenerationResult` with PNG bytes
- ✅ Disposes resources properly (even though Azure SDK is not IDisposable)

**CLI Adapter:**
- ✅ Registered in `ServiceCollectionExtensions`
- ✅ Health check validates endpoint + deployment
- ✅ Secrets stored securely (user secrets, env vars, Key Vault)
- ✅ Error messages are actionable (404 → check deployment name)

**Testing:**
- ✅ Unit tests cover size mapping, error handling
- ✅ Integration tests run against live Azure (manually triggered)
- ✅ No test failures in CI (integration tests skipped or rate-limited)

**Documentation:**
- ✅ Setup guide includes Azure Portal screenshots
- ✅ README comparison table updated
- ✅ Sample code compiles and runs

### 8.3 Open Questions

1. **Which Azure.AI.OpenAI version to target?**
   - **2.8.0-beta.1** (latest, supports 1792×1024) vs. **2.7.x stable** (may lack new sizes)?
   - **Recommendation:** Check NuGet for latest stable; use beta only if 1792 sizes required.

2. **Should we support image editing (inpainting)?**
   - GPT-Image-1.5 supports `GenerateImageEditAsync()`
   - Current `IImageGenerator` is text-to-image only
   - **Recommendation:** Phase 2 feature (extend interface or separate `IImageEditor`)

3. **Managed Identity or API Key for production?**
   - Managed Identity eliminates secrets but requires Azure-hosted apps
   - API Key works everywhere (Azure, on-prem, local dev)
   - **Recommendation:** Support both; default to API Key, document Managed Identity option

4. **Cost alerts in CLI?**
   - Should CLI warn before batch operations? (e.g., "Generating 50 images = ~$2-$4")
   - **Recommendation:** Add `--yes` flag to skip prompts; show estimated cost with confirmation

---

## 9. Conclusion

**GPT-Image-1.5 Integration is Feasible** with moderate effort (~2 weeks). Key advantages:
- ✅ Superior prompt adherence and artistic range
- ✅ Enterprise features (Entra ID, content safety, provenance)
- ✅ Mature Azure SDK with built-in retry logic
- ✅ Inpainting/editing capabilities (future extension)

**Key Challenges:**
- ⚠️ Fixed size constraints (3 presets only)
- ⚠️ Deployment name configuration complexity
- ⚠️ Beta SDK version risk (if using 2.8.0-beta.1)
- ⚠️ Separate Azure resource required (not Foundry-compatible)

**Recommendation:** **Proceed with integration** as a complementary generator (not replacement for Flux2/MAI). Position as "enterprise-grade, Azure-native" option for users with Azure OpenAI subscriptions.

**Next Steps:**
1. Confirm Azure.AI.OpenAI package version (stable vs. beta)
2. Create Azure OpenAI test deployment (Bruno's subscription)
3. Spike: Implement minimal generator + CLI adapter (1-2 days)
4. Review spike with team before full implementation

---

**End of Assessment**

River — AI/ML Specialist  
*"The model is only as good as the prompt — and the integration is only as good as your understanding of the constraints."*
