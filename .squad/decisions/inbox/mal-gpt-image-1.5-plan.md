# GPT-Image-1.5 Integration — Technical Analysis & Implementation Plan

**Author:** Mal (Lead)  
**Date:** 2025-01-27  
**Requested by:** Bruno Capuano  
**Status:** Planning — awaiting approval

---

## Executive Summary

This document provides a comprehensive technical analysis and implementation plan for integrating **GPT-Image-1.5** support into ElBruno.Text2Image via the Azure OpenAI SDK. The integration follows established patterns from FLUX.2 and MAI-Image-2, maintaining architectural consistency while leveraging the native Azure OpenAI `ImageClient` API.

**Scope:**
- New `GptImage1p5Generator` class in `ElBruno.Text2Image.Foundry`
- CLI provider adapter (`FoundryGptImage1p5Adapter`)
- NuGet package updates (ElBruno.Text2Image.Foundry)
- Sample project (`scenario-15-gpt-image-1p5-cloud`)
- Test coverage (HTTP-level tests, configuration tests)
- Documentation updates

**Timeline Estimate:** 2-3 days (1 day core generator + 1 day CLI/tests + 0.5 day samples/docs)

---

## 1. Current Architecture Review

### 1.1 Existing Generator Implementations

The codebase currently supports two cloud-based generators:

#### **Flux2Generator** (BFL Native API)
- **Pattern:** Direct `HttpClient` usage with manual JSON serialization
- **API Style:** Asynchronous polling (202 → poll → 200)
- **Endpoint:** `.services.ai.azure.com/providers/blackforestlabs/v1/flux-2-pro`
- **Request Body:** `ByteArrayContent` with source-generated JSON context (`Flux2JsonContext`)
- **Authentication:** `api-key` header
- **Response Handling:** Base64 JSON or URL-based image data
- **Size Support:** Variable dimensions (e.g., 512×512)

#### **MaiImage2Generator** (MAI API)
- **Pattern:** Direct `HttpClient` usage with manual JSON serialization
- **API Style:** Synchronous response (200 OK with image data immediately)
- **Endpoint:** `.services.ai.azure.com/mai/v1/images/generations`
- **Request Body:** `ByteArrayContent` with source-generated JSON context (`MaiImage2JsonContext`)
- **Authentication:** `api-key` header
- **Response Handling:** Base64 JSON or URL-based image data
- **Size Support:** Default 1024×1024

### 1.2 Common Patterns Across Generators

**Interface Compliance:**
- All generators implement `IImageGenerator` (ElBruno.Text2Image)
- All generators implement `Microsoft.Extensions.AI.IImageGenerator`
- Both interfaces require `GenerateAsync`, `EnsureModelAvailableAsync`, `ModelName` property

**Constructor Pattern:**
```csharp
public XxxGenerator(
    string endpoint,
    string apiKey,
    string? modelName = null,
    string? modelId = null,
    HttpClient? httpClient = null)
```

**Endpoint Handling:**
- Auto-conversion from `.openai.azure.com` → `.services.ai.azure.com` (for BFL/MAI)
- Fallback URL construction if user provides base URL only
- Validation: HTTPS required, null/whitespace checks

**HTTP Client Management:**
- Accepts optional `HttpClient` injection (DI-friendly)
- Creates default `HttpClient` with 5-minute timeout if not provided
- `_ownsHttpClient` flag controls disposal

**Error Handling:**
- HTTP status validation with detailed error messages
- Body truncation for error logs (MaxErrorBodyLength = 1024)
- Endpoint hints in error messages (404 → endpoint guidance)

**Serialization:**
- Source-generated JSON contexts for AOT compatibility
- `ByteArrayContent` for request bodies (explicit Content-Length header)
- UTF-8 charset specification

**Testing Strategy:**
- `FakeHttpHandler` for HTTP-level unit tests
- Validates headers, request body, Content-Length, JSON structure
- No mocks for Azure SDKs (direct HTTP interception)

### 1.3 Service Registration

**DI Extension Pattern** (ServiceCollectionExtensions.cs):
```csharp
public static IServiceCollection AddFlux2Generator(
    this IServiceCollection services,
    string endpoint,
    string apiKey,
    string? modelName = null,
    string? modelId = null)
{
    services.AddSingleton<IImageGenerator>(
        new Flux2Generator(endpoint, apiKey, modelName, modelId));
    return services;
}
```

**CLI Provider Adapter Pattern** (IProviderAdapter):
- `Id` (e.g., "foundry-flux2")
- `DisplayName` (e.g., "FLUX.2 Pro (Cloud)")
- `RequiredSecrets` (e.g., ["apiKey"])
- `RequiredFields` (e.g., ["endpoint", "model"])
- `CheckAsync` (health check)
- `GenerateAsync` (orchestration)

**Configuration Flow:**
1. `ConfigStore` loads `AppConfig` with `ProviderConfig` per provider
2. `SecretResolver` resolves secrets from env vars → DPAPI (Windows) → plaintext file
3. Adapter reads `endpoint` and `model` from config, `apiKey` from secrets
4. Adapter instantiates generator and calls `GenerateAsync`

---

## 2. GPT-Image-1.5 Technical Specification

### 2.1 Azure OpenAI SDK Overview

**Sample Code Pattern (from user context):**
```csharp
using Azure.AI.OpenAI;
using Azure;

string endpoint = "https://your-resource.openai.azure.com/";
string deploymentName = "gpt-image-1.5";
string apiKey = "your-api-key";

var client = new AzureOpenAIClient(
    new Uri(endpoint), 
    new ApiKeyCredential(apiKey));

var imageClient = client.GetImageClient(deploymentName);

var result = await imageClient.GenerateImageAsync(
    "a serene landscape with mountains and a river",
    new ImageGenerationOptions
    {
        Size = GeneratedImageSize.W1024xH1024,
        ResponseFormat = GeneratedImageFormat.Bytes
    });

BinaryData bytes = result.Value.ImageBytes;
await File.WriteAllBytesAsync("output.png", bytes.ToArray());
```

### 2.2 Key Differences from FLUX.2/MAI-Image-2

| Aspect | FLUX.2 / MAI-Image-2 | GPT-Image-1.5 |
|--------|----------------------|---------------|
| **SDK** | Direct `HttpClient` | Azure.AI.OpenAI SDK |
| **Endpoint** | `.services.ai.azure.com` | `.openai.azure.com` |
| **Authentication** | `api-key` header | `ApiKeyCredential` |
| **Client Type** | Manual JSON | `ImageClient` |
| **Request Body** | Custom JSON classes | `ImageGenerationOptions` (SDK) |
| **Response Type** | JSON → Base64/URL | `BinaryData` bytes |
| **Size Format** | `int Width, int Height` | `GeneratedImageSize` enum |
| **Deployment Naming** | Model ID in body | Deployment name in client |

### 2.3 Size Constraints

**GPT-Image-1.5 Supported Sizes (SDK enum):**
- 1024×1024 (most common)
- 1792×1024 (landscape)
- 1024×1792 (portrait)

**Constraint:** Unlike FLUX.2/MAI-Image-2, GPT-Image-1.5 doesn't support arbitrary dimensions. We must map user-requested sizes to supported enum values.

---

## 3. Architectural Decision Points

### 3.1 SDK vs. Manual HttpClient

**Decision:** Use **Azure.AI.OpenAI SDK** directly (not manual HttpClient like FLUX.2/MAI).

**Rationale:**
1. **Official Support:** The SDK is the official, supported way to interact with Azure OpenAI
2. **Type Safety:** `ImageClient`, `ImageGenerationOptions`, `GeneratedImageSize` provide compile-time safety
3. **Authentication:** `ApiKeyCredential` handles header formatting and future token refresh scenarios
4. **Maintenance:** SDK updates for new features (e.g., DALL-E 4) happen upstream, not in our code
5. **Consistency with Ecosystem:** Aligns with Microsoft.Extensions.AI patterns

**Trade-offs:**
- ❌ Adds `Azure.AI.OpenAI` NuGet dependency (~200KB)
- ❌ Cannot use `FakeHttpHandler` for testing (need alternative approach)
- ✅ Reduces code maintenance (no manual JSON, no endpoint versioning)
- ✅ Better error messages from SDK (already localized, detailed)

**Alternative Considered:** Manual HttpClient with OpenAI REST API
- Rejected: More code to maintain, no benefit over SDK

### 3.2 Interface Compliance

**Decision:** Implement both `IImageGenerator` and `Microsoft.Extensions.AI.IImageGenerator`.

**Rationale:** Maintains consistency with FLUX.2 and MAI-Image-2. Existing code expects both interfaces.

**Challenge:** Map `ImageGenerationOptions` (local) → SDK's `ImageGenerationOptions` (Azure.AI.OpenAI).

**Resolution:**
```csharp
// Local options (512×512) → SDK options (1024×1024 fallback)
var sdkSize = MapToGeneratedImageSize(localOptions?.Width ?? 1024, localOptions?.Height ?? 1024);
var sdkOptions = new Azure.AI.OpenAI.ImageGenerationOptions
{
    Size = sdkSize,
    ResponseFormat = GeneratedImageFormat.Bytes
};
```

### 3.3 Size Mapping Strategy

**Decision:** Implement **best-fit mapping** with explicit documentation.

**Mapping Logic:**
```csharp
private static GeneratedImageSize MapToGeneratedImageSize(int width, int height)
{
    // Exact matches first
    if (width == 1024 && height == 1024) return GeneratedImageSize.W1024xH1024;
    if (width == 1792 && height == 1024) return GeneratedImageSize.W1792xH1024;
    if (width == 1024 && height == 1792) return GeneratedImageSize.W1024xH1792;
    
    // Best-fit heuristic
    double aspectRatio = (double)width / height;
    
    if (aspectRatio > 1.5) return GeneratedImageSize.W1792xH1024; // Landscape
    if (aspectRatio < 0.7) return GeneratedImageSize.W1024xH1792; // Portrait
    return GeneratedImageSize.W1024xH1024; // Square fallback
}
```

**Documentation Note:** XML doc on `GenerateAsync` will state:
> GPT-Image-1.5 supports only 1024×1024, 1792×1024, and 1024×1792. Arbitrary sizes are mapped to the nearest supported size.

### 3.4 Configuration Management

**Decision:** Follow existing CLI pattern with `ProviderConfig`.

**Config Structure:**
```json
{
  "defaultProvider": "foundry-gpt-image-1p5",
  "providers": {
    "foundry-gpt-image-1p5": {
      "endpoint": "https://your-resource.openai.azure.com/",
      "model": "gpt-image-1.5"
    }
  }
}
```

**Secret Storage:**
```bash
# Environment variable
T2I_FOUNDRY_GPT_IMAGE_1P5_APIKEY=your-api-key

# OR DPAPI (Windows)
t2i config set foundry-gpt-image-1p5.apiKey your-api-key

# OR plaintext file (fallback)
~/.config/t2i/secrets.json
```

**Field Mapping:**
- `endpoint` → Azure OpenAI endpoint (e.g., `https://myresource.openai.azure.com/`)
- `model` → Deployment name (e.g., `gpt-image-1.5`, `dall-e-3`)
- `apiKey` → API key (secret)

### 3.5 Error Handling

**Decision:** Delegate to SDK's exception model, with context wrapper.

**SDK Exceptions:**
- `Azure.RequestFailedException` (HTTP errors)
- `Azure.AI.OpenAI.ClientResultException` (parsing errors)

**Wrapper Pattern:**
```csharp
try
{
    var result = await imageClient.GenerateImageAsync(prompt, sdkOptions, cancellationToken);
    // ...
}
catch (RequestFailedException ex) when (ex.Status == 404)
{
    throw new HttpRequestException(
        $"GPT-Image-1.5 endpoint not found. Verify deployment name '{_modelId}' exists at {_endpoint}.\n" +
        $"Hint: Check Azure portal → Azure OpenAI → Deployments.",
        ex);
}
catch (RequestFailedException ex)
{
    throw new HttpRequestException(
        $"GPT-Image-1.5 API error ({ex.Status}): {ex.Message}",
        ex);
}
```

### 3.6 Testing Strategy

**Decision:** Use SDK's built-in test helpers (if available) OR integration tests only.

**Challenge:** `FakeHttpHandler` doesn't work with Azure SDK's internal HTTP pipeline.

**Options:**
1. **SDK Test Helpers:** Azure SDKs sometimes provide test client factories (research needed)
2. **Integration Tests:** Mark as `[SkippableFact]`, require env vars for real endpoint
3. **Minimal Unit Tests:** Test size mapping, endpoint validation, constructor logic only

**Preferred Approach:**
- Unit tests for `MapToGeneratedImageSize`, endpoint validation, constructor
- Integration tests for actual generation (skippable if env vars missing)
- Document in test file: "Note: Azure.AI.OpenAI SDK does not support FakeHttpHandler"

---

## 4. Implementation Scope

### 4.1 File Structure

```
src/
├── ElBruno.Text2Image.Foundry/
│   ├── GptImage1p5Generator.cs              ← NEW (main generator)
│   ├── ServiceCollectionExtensions.cs        ← UPDATE (add DI method)
│   ├── ElBruno.Text2Image.Foundry.csproj    ← UPDATE (add Azure.AI.OpenAI)
│   └── ...
│
├── ElBruno.Text2Image.Cli/
│   ├── Providers/
│   │   ├── FoundryGptImage1p5Adapter.cs     ← NEW (CLI adapter)
│   │   └── ProviderRegistry.cs              ← NO CHANGE (auto-discovers adapters)
│   └── Infrastructure/
│       └── ProviderServiceCollectionExtensions.cs ← UPDATE (register adapter)
│
├── ElBruno.Text2Image.Tests/
│   ├── GptImage1p5GeneratorTests.cs         ← NEW (unit tests)
│   └── GptImage1p5GeneratorIntegrationTests.cs ← NEW (skippable)
│
└── samples/
    └── scenario-15-gpt-image-1p5-cloud/     ← NEW
        ├── Program.cs
        ├── scenario-15-gpt-image-1p5-cloud.csproj
        └── README.md
```

### 4.2 Dependencies

**New NuGet Dependency:**
```xml
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.*" />
```

**Reasoning:** Use 2.1.x for .NET 8/10 compatibility. Pin to minor version (2.1.*) for stability, allow patch updates.

**Impact on Package Size:**
- `Azure.AI.OpenAI`: ~180KB
- Transitive dependencies: `Azure.Core`, `System.ClientModel` (already in .NET SDK)

### 4.3 GptImage1p5Generator.cs

**Class Signature:**
```csharp
namespace ElBruno.Text2Image.Foundry;

/// <summary>
/// GPT-Image-1.5 text-to-image generator using Azure OpenAI.
/// Supports 1024×1024, 1792×1024, and 1024×1792 image sizes.
/// This is a cloud API model — no local ONNX models are needed.
/// </summary>
public sealed class GptImage1p5Generator : IImageGenerator, Microsoft.Extensions.AI.IImageGenerator
{
    private readonly AzureOpenAIClient _client;
    private readonly ImageClient _imageClient;
    private readonly string _modelDisplayName;
    private readonly string _deploymentName;
    private readonly string _endpoint;
    
    public string ModelName => _modelDisplayName;
    public string DeploymentName => _deploymentName;
    public string Endpoint => _endpoint;
    
    public GptImage1p5Generator(
        string endpoint,
        string apiKey,
        string? modelName = null,
        string? deploymentName = null)
    {
        // Validation, client initialization
        // ...
    }
    
    public Task EnsureModelAvailableAsync(...) { /* No-op for cloud */ }
    public async Task<ImageGenerationResult> GenerateAsync(...) { /* SDK call */ }
    Task<ImageGenerationResponse> Microsoft.Extensions.AI.IImageGenerator.GenerateAsync(...) { /* Adapter */ }
    object? Microsoft.Extensions.AI.IImageGenerator.GetService(...) { /* Service locator */ }
    public void Dispose() { /* No resources to dispose */ }
}
```

**Key Implementation Details:**
1. Constructor validates `endpoint` is HTTPS, creates `AzureOpenAIClient` and `ImageClient`
2. `GenerateAsync` maps size, calls SDK, converts `BinaryData` → `byte[]`
3. No custom `HttpClient` support (SDK manages its own HTTP pipeline)
4. `Dispose` is no-op (SDK clients are lightweight, no unmanaged resources)

### 4.4 CLI Integration

**FoundryGptImage1p5Adapter.cs:**
```csharp
internal sealed class FoundryGptImage1p5Adapter : IProviderAdapter
{
    public string Id => "foundry-gpt-image-1p5";
    public string DisplayName => "GPT-Image-1.5 (Cloud)";
    public ProviderKind Kind => ProviderKind.Cloud;
    public IReadOnlyList<string> RequiredSecrets => new[] { "apiKey" };
    public IReadOnlyList<string> RequiredFields => new[] { "endpoint", "model" };
    
    // CheckAsync: Validate endpoint/apiKey exist (no actual API call — SDK doesn't support HEAD)
    // GenerateAsync: Instantiate GptImage1p5Generator, call GenerateAsync
}
```

**CLI Commands:**
```bash
# Setup
t2i config set foundry-gpt-image-1p5.endpoint https://myresource.openai.azure.com/
t2i config set foundry-gpt-image-1p5.model gpt-image-1.5
t2i secrets set foundry-gpt-image-1p5 apiKey your-key-here

# Generate
t2i --provider foundry-gpt-image-1p5 "a serene mountain landscape"

# Set as default
t2i config set defaultProvider foundry-gpt-image-1p5
t2i "a cat astronaut"  # uses gpt-image-1.5
```

### 4.5 Testing Strategy

**Unit Tests (GptImage1p5GeneratorTests.cs):**
- ✅ Constructor validation (null endpoint, non-HTTPS, null API key)
- ✅ `MapToGeneratedImageSize` logic (512×512 → 1024×1024, 1920×1080 → 1792×1024)
- ✅ `ModelName` property reflects constructor input
- ✅ `EnsureModelAvailableAsync` completes immediately (cloud model)

**Integration Tests (GptImage1p5GeneratorIntegrationTests.cs):**
```csharp
[SkippableFact]
public async Task GenerateAsync_RealEndpoint_ProducesImage()
{
    var endpoint = Environment.GetEnvironmentVariable("GPT_IMAGE_15_ENDPOINT");
    var apiKey = Environment.GetEnvironmentVariable("GPT_IMAGE_15_API_KEY");
    Skip.If(string.IsNullOrEmpty(endpoint), "GPT_IMAGE_15_ENDPOINT not set");
    
    using var generator = new GptImage1p5Generator(endpoint, apiKey);
    var result = await generator.GenerateAsync("test prompt");
    
    Assert.NotEmpty(result.ImageBytes);
    Assert.Equal(1024, result.Width);
    Assert.Equal(1024, result.Height);
}
```

**CLI Tests:**
- ✅ Provider registration (verify `foundry-gpt-image-1p5` appears in `t2i providers`)
- ✅ Config round-trip (set endpoint → show config → verify display)
- ✅ Secret resolution (env var → DPAPI fallback)

### 4.6 NuGet Package Updates

**ElBruno.Text2Image.Foundry v0.10.0:**
- Description: Add "GPT-Image-1.5 via Azure OpenAI"
- Tags: Add "gpt-image", "azure-openai", "dall-e"
- Changelog entry:
  ```markdown
  ## 0.10.0 (2025-01-xx)
  - Added GptImage1p5Generator for Azure OpenAI GPT-Image-1.5 support
  - New dependency: Azure.AI.OpenAI 2.1.x
  - Breaking: None (additive change)
  ```

**ElBruno.Text2Image.Cli v0.11.0:**
- Changelog entry:
  ```markdown
  ## 0.11.0 (2025-01-xx)
  - Added foundry-gpt-image-1p5 provider for GPT-Image-1.5
  - New config: `t2i config set foundry-gpt-image-1p5.endpoint ...`
  ```

### 4.7 Sample Project

**scenario-15-gpt-image-1p5-cloud/Program.cs:**
```csharp
using ElBruno.Text2Image;
using ElBruno.Text2Image.Foundry;
using Microsoft.Extensions.Configuration;

// Same pattern as scenario-13 (MAI-Image-2) and scenario-03 (FLUX.2)
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true)
    .Build();

var endpoint = config["GPT_IMAGE_15_ENDPOINT"];
var apiKey = config["GPT_IMAGE_15_API_KEY"];
var deploymentName = config["GPT_IMAGE_15_DEPLOYMENT"] ?? "gpt-image-1.5";

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("ERROR: GPT_IMAGE_15_ENDPOINT and GPT_IMAGE_15_API_KEY not configured.");
    // ... help text ...
    return;
}

using var generator = new GptImage1p5Generator(endpoint, apiKey, deploymentName: deploymentName);

var result = await generator.GenerateAsync(
    "a serene landscape with mountains and a river",
    new ImageGenerationOptions { Width = 1024, Height = 1024 });

await result.SaveAsync("gpt_image_1p5_output.png");
Console.WriteLine($"Image saved: {Path.GetFullPath("gpt_image_1p5_output.png")}");
```

---

## 5. Architecture Diagrams

### 5.1 Component Diagram (ASCII Art)

```
┌─────────────────────────────────────────────────────────────────┐
│                     ElBruno.Text2Image                          │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ IImageGenerator                                          │   │
│  │  + GenerateAsync(prompt, options)                        │   │
│  │  + EnsureModelAvailableAsync()                           │   │
│  │  + ModelName: string                                     │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ implements
                              │
┌─────────────────────────────────────────────────────────────────┐
│              ElBruno.Text2Image.Foundry                         │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │ Flux2Generator   │  │ MaiImage2Gen...  │  │ GptImage1p5  │  │
│  │  (BFL API)       │  │  (MAI API)       │  │  Generator   │  │
│  │                  │  │                  │  │ (Azure SDK)  │  │
│  │ ┌──────────────┐ │  │ ┌──────────────┐ │  │ ┌──────────┐ │  │
│  │ │ HttpClient   │ │  │ │ HttpClient   │ │  │ │ ImageClient│ │
│  │ │ (manual JSON)│ │  │ │ (manual JSON)│ │  │ │ (SDK)     │ │
│  │ └──────────────┘ │  │ └──────────────┘ │  │ └──────────┘ │  │
│  └──────────────────┘  └──────────────────┘  └──────────────┘  │
│                                                                 │
│  Dependencies:                                                  │
│   - Azure.AI.OpenAI 2.1.* (new)                                │
│   - Microsoft.Extensions.AI.Abstractions 10.3.0                │
└─────────────────────────────────────────────────────────────────┘
                              ▲
                              │ uses
                              │
┌─────────────────────────────────────────────────────────────────┐
│               ElBruno.Text2Image.Cli                            │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ProviderRegistry                                          │  │
│  │  - FoundryFlux2Adapter                                    │  │
│  │  - FoundryMaiImage2Adapter                                │  │
│  │  - FoundryGptImage1p5Adapter  ← NEW                       │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │ ConfigStore + SecretResolver                              │  │
│  │  → EnvVarSecretStore                                      │  │
│  │  → DpapiSecretStore (Windows)                             │  │
│  │  → PlainFileSecretStore (fallback)                        │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Request Flow Diagram

```
User
  │
  │ t2i "prompt" --provider foundry-gpt-image-1p5
  ▼
┌────────────────────────────────────────────────────────────────┐
│ GenerateCommand                                                │
│  1. Load AppConfig from ~/.config/t2i/config.json             │
│  2. Resolve secrets from SecretResolver                        │
│  3. Get adapter from ProviderRegistry                          │
│  4. Call adapter.GenerateAsync(...)                            │
└────────────────────────────────────────────────────────────────┘
  │
  ▼
┌────────────────────────────────────────────────────────────────┐
│ FoundryGptImage1p5Adapter                                      │
│  1. Read endpoint from config.Providers["foundry-gpt-image..."]│
│  2. Read apiKey from SecretResolver (env → DPAPI → file)      │
│  3. Instantiate GptImage1p5Generator(endpoint, apiKey)        │
│  4. Call generator.GenerateAsync(prompt, options)             │
└────────────────────────────────────────────────────────────────┘
  │
  ▼
┌────────────────────────────────────────────────────────────────┐
│ GptImage1p5Generator                                           │
│  1. Map size (512×512 → 1024×1024 via MapToGeneratedImageSize)│
│  2. Create SDK options (Size, ResponseFormat)                  │
│  3. Call imageClient.GenerateImageAsync(prompt, options)       │
│  4. Convert BinaryData → byte[]                                │
│  5. Return ImageGenerationResult                               │
└────────────────────────────────────────────────────────────────┘
  │
  ▼
┌────────────────────────────────────────────────────────────────┐
│ Azure.AI.OpenAI.ImageClient                                    │
│  1. Build HTTP request (POST /openai/deployments/.../images/..│
│  2. Add Authorization header (ApiKeyCredential)                │
│  3. Send to https://myresource.openai.azure.com/              │
│  4. Parse response (BinaryData or URL)                         │
│  5. Return GenerateImageResult                                 │
└────────────────────────────────────────────────────────────────┘
  │
  ▼
Azure OpenAI Service
  │
  │ Generate image with GPT-Image-1.5
  │
  ▼
Response: BinaryData (PNG bytes)
```

---

## 6. Integration Points

### 6.1 Foundry Library Integration

**File:** `src/ElBruno.Text2Image.Foundry/GptImage1p5Generator.cs`

**Integration Steps:**
1. Add `Azure.AI.OpenAI` NuGet reference to `.csproj`
2. Implement `IImageGenerator` and `Microsoft.Extensions.AI.IImageGenerator`
3. Follow naming conventions: `GptImage1p5Generator` (not `GptImage15Generator`)
4. Add XML documentation with `<summary>`, `<param>`, `<returns>`
5. Register in `ServiceCollectionExtensions.cs`:
   ```csharp
   public static IServiceCollection AddGptImage1p5Generator(
       this IServiceCollection services,
       string endpoint,
       string apiKey,
       string? modelName = null,
       string? deploymentName = null)
   ```

### 6.2 CLI Integration

**File:** `src/ElBruno.Text2Image.Cli/Providers/FoundryGptImage1p5Adapter.cs`

**Integration Steps:**
1. Implement `IProviderAdapter` interface
2. Register in `ProviderServiceCollectionExtensions.cs`:
   ```csharp
   services.AddSingleton<IProviderAdapter, FoundryGptImage1p5Adapter>();
   ```
3. No changes needed to `ProviderRegistry` (auto-discovery via DI)
4. No changes needed to `GenerateCommand` (uses `IProviderAdapter` abstraction)

**Configuration Example:**
```json
{
  "defaultProvider": "foundry-gpt-image-1p5",
  "providers": {
    "foundry-gpt-image-1p5": {
      "endpoint": "https://myresource.openai.azure.com/",
      "model": "gpt-image-1.5"
    }
  }
}
```

### 6.3 Sample Integration

**File:** `src/samples/scenario-15-gpt-image-1p5-cloud/Program.cs`

**Pattern:** Follow scenario-03 (FLUX.2) and scenario-13 (MAI-Image-2)
1. ConfigurationBuilder with user secrets support
2. Env var validation with helpful error messages
3. Generator instantiation and `GenerateAsync` call
4. Save result to file with `result.SaveAsync()`

**README.md:**
- Deployment instructions (Azure portal → Create deployment → Copy endpoint/key)
- Configuration options (user secrets, env vars, appsettings.json)
- Size constraints (1024×1024, 1792×1024, 1024×1792)

---

## 7. Testing Strategy

### 7.1 Unit Test Coverage

**File:** `src/ElBruno.Text2Image.Tests/GptImage1p5GeneratorTests.cs`

**Test Cases:**
1. ✅ `Constructor_NullEndpoint_ThrowsArgumentException`
2. ✅ `Constructor_NonHttpsEndpoint_ThrowsArgumentException`
3. ✅ `Constructor_NullApiKey_ThrowsArgumentException`
4. ✅ `Constructor_ValidInputs_SetsProperties`
5. ✅ `MapToGeneratedImageSize_ExactMatch_ReturnsCorrectEnum`
6. ✅ `MapToGeneratedImageSize_Landscape_ReturnsW1792xH1024`
7. ✅ `MapToGeneratedImageSize_Portrait_ReturnsW1024xH1792`
8. ✅ `MapToGeneratedImageSize_Square_ReturnsW1024xH1024`
9. ✅ `EnsureModelAvailableAsync_CompletesImmediately`
10. ✅ `ModelName_ReflectsConstructorInput`

**Note:** Cannot test `GenerateAsync` with `FakeHttpHandler` (SDK's internal HTTP pipeline). Use integration tests instead.

### 7.2 Integration Test Coverage

**File:** `src/ElBruno.Text2Image.Tests/GptImage1p5GeneratorIntegrationTests.cs`

**Test Cases (all marked `[SkippableFact]`):**
1. ✅ `GenerateAsync_1024x1024_ProducesImage`
2. ✅ `GenerateAsync_1792x1024_ProducesImage`
3. ✅ `GenerateAsync_CustomPrompt_ContainsImageBytes`
4. ✅ `GenerateAsync_InvalidDeployment_ThrowsRequestFailedException`

**Environment Variables Required:**
- `GPT_IMAGE_15_ENDPOINT`
- `GPT_IMAGE_15_API_KEY`
- `GPT_IMAGE_15_DEPLOYMENT` (optional, defaults to "gpt-image-1.5")

### 7.3 CLI Test Coverage

**File:** `src/ElBruno.Text2Image.Tests/Cli/ProviderRegistryTests.cs` (update)

**Test Cases:**
1. ✅ `ProviderRegistry_IncludesGptImage1p5Adapter`
2. ✅ `GptImage1p5Adapter_Id_IsCorrect`
3. ✅ `GptImage1p5Adapter_RequiredSecrets_ContainsApiKey`
4. ✅ `GptImage1p5Adapter_RequiredFields_ContainsEndpointAndModel`

### 7.4 Test Doubles and Mocking

**Challenge:** Azure.AI.OpenAI SDK doesn't expose mockable interfaces.

**Solutions:**
1. **Adapter Pattern (recommended):** Create `IGptImageClient` wrapper interface, inject into generator
   - ❌ Adds complexity, not used by other generators
2. **Integration Tests Only:** Accept that some tests require real endpoint
   - ✅ Simpler, matches SDK design philosophy
   - ✅ `[SkippableFact]` ensures CI doesn't fail without credentials

**Decision:** Use integration tests with `[SkippableFact]`. Document in test file:
```csharp
// Note: Azure.AI.OpenAI SDK does not support FakeHttpHandler mocking.
// These tests require real Azure OpenAI credentials via environment variables.
// Use [SkippableFact] to avoid CI failures when credentials are unavailable.
```

---

## 8. CLI Usage Examples

### 8.1 Initial Setup

```bash
# Install CLI (if not already installed)
dotnet tool install --global ElBruno.Text2Image.Cli

# Configure GPT-Image-1.5 provider
t2i config set foundry-gpt-image-1p5.endpoint https://myresource.openai.azure.com/
t2i config set foundry-gpt-image-1p5.model gpt-image-1.5

# Store API key securely (DPAPI on Windows, file on Linux/macOS)
t2i secrets set foundry-gpt-image-1p5 apiKey sk-proj-...

# Verify configuration
t2i config show
# Output:
#   defaultProvider: (not set)
#   providers:
#     foundry-gpt-image-1p5:
#       endpoint: https://myresource.openai.azure.com/
#       model: gpt-image-1.5
#       apiKey: ********** (masked)
```

### 8.2 Generate Images

```bash
# Generate with explicit provider flag
t2i --provider foundry-gpt-image-1p5 "a serene mountain landscape"

# Set as default provider
t2i config set defaultProvider foundry-gpt-image-1p5

# Generate with default provider (no flag needed)
t2i "a cat astronaut in space"

# Specify output file
t2i "sunset over ocean" --output sunset.png

# Custom size (maps to 1024×1024)
t2i "portrait of a robot" --width 800 --height 800
# Note: GPT-Image-1.5 only supports 1024×1024, 1792×1024, 1024×1792.
#       Your request (800×800) will be mapped to 1024×1024.

# Landscape size
t2i "panoramic city skyline" --width 1792 --height 1024

# Portrait size
t2i "tall skyscraper" --width 1024 --height 1792
```

### 8.3 Health Check

```bash
# Verify provider is configured and reachable
t2i providers
# Output:
#   Available providers:
#     ✓ foundry-flux2       FLUX.2 Pro (Cloud)
#     ✓ foundry-mai2        MAI-Image-2 (Cloud)
#     ✓ foundry-gpt-image-1p5  GPT-Image-1.5 (Cloud)  ← NEW
#
#   Default: foundry-gpt-image-1p5

t2i doctor
# Output:
#   Checking foundry-gpt-image-1p5...
#     ✓ Endpoint configured
#     ✓ API key found (from DPAPI)
#     ✓ Model configured: gpt-image-1.5
```

### 8.4 Environment Variable Alternative

```bash
# Set via environment variables (useful for CI/CD)
export T2I_FOUNDRY_GPT_IMAGE_1P5_ENDPOINT=https://myresource.openai.azure.com/
export T2I_FOUNDRY_GPT_IMAGE_1P5_APIKEY=sk-proj-...
export T2I_FOUNDRY_GPT_IMAGE_1P5_MODEL=gpt-image-1.5

# Generate (reads from env vars)
t2i --provider foundry-gpt-image-1p5 "a cat astronaut"
```

---

## 9. Implementation Phases

### Phase 1: Core Generator (Day 1)

**Tasks:**
1. ✅ Add `Azure.AI.OpenAI` NuGet reference to Foundry project
2. ✅ Implement `GptImage1p5Generator.cs`
   - Constructor with validation
   - `MapToGeneratedImageSize` helper
   - `GenerateAsync` with SDK call
   - `EnsureModelAvailableAsync` no-op
   - M.E.AI interface implementation
3. ✅ Add `AddGptImage1p5Generator` DI extension
4. ✅ Write unit tests (constructor, size mapping, properties)
5. ✅ Write integration tests (marked skippable)
6. ✅ Build and run tests (skip integration if no creds)

**Acceptance Criteria:**
- All unit tests pass
- Integration tests skip gracefully if env vars missing
- Generator compiles without warnings
- No breaking changes to existing code

### Phase 2: CLI Integration (Day 2)

**Tasks:**
1. ✅ Implement `FoundryGptImage1p5Adapter.cs`
   - `Id`, `DisplayName`, `RequiredSecrets`, `RequiredFields`
   - `CheckAsync` validation
   - `GenerateAsync` orchestration
2. ✅ Register adapter in `ProviderServiceCollectionExtensions.cs`
3. ✅ Write CLI tests (provider registry, config round-trip)
4. ✅ Manual testing:
   - `t2i config set ...`
   - `t2i secrets set ...`
   - `t2i --provider foundry-gpt-image-1p5 "test"`
   - `t2i providers` (verify appears in list)
   - `t2i doctor` (verify health check)
5. ✅ Update CLI README.md with new provider

**Acceptance Criteria:**
- `t2i providers` lists `foundry-gpt-image-1p5`
- `t2i config set` stores endpoint and model
- `t2i secrets set` stores API key securely
- `t2i doctor` validates configuration
- Generate command produces image with GPT-Image-1.5

### Phase 3: Samples & Documentation (Day 2.5)

**Tasks:**
1. ✅ Create `scenario-15-gpt-image-1p5-cloud` sample project
   - Program.cs (config builder, generator usage)
   - .csproj (project references)
   - README.md (setup instructions)
2. ✅ Update main README.md
   - Add GPT-Image-1.5 to feature list
   - Add CLI usage example
   - Add NuGet badge (if Foundry v0.10.0 released)
3. ✅ Create setup guide: `docs/gpt-image-1p5-setup-guide.md`
   - Azure portal instructions
   - Configuration examples
   - Size constraints explanation
4. ✅ Update CHANGELOG.md for both packages
5. ✅ Update `docs/model-support.md` with GPT-Image-1.5 entry

**Acceptance Criteria:**
- Sample runs successfully with user secrets
- Documentation is clear and complete
- All links work (no 404s)
- Changelog entries follow existing format

### Phase 4: Release (Day 3)

**Tasks:**
1. ✅ Run full test suite (net8.0 + net10.0)
2. ✅ Build NuGet packages (`dotnet pack`)
3. ✅ Test package installation in clean project
4. ✅ Update NuGet package metadata (description, tags, version)
5. ✅ Create PR with all changes
6. ✅ Code review by Mal (self-review checklist)
7. ✅ Merge to main
8. ✅ Tag release (v0.10.0 for Foundry, v0.11.0 for CLI)
9. ✅ Publish to NuGet.org

**Acceptance Criteria:**
- All tests pass (324 existing + new tests)
- No build warnings
- NuGet package installs cleanly
- CLI tool updates successfully (`dotnet tool update`)
- GitHub Actions publish workflow succeeds

---

## 10. Risk Assessment & Mitigation

### 10.1 Risks

#### **Risk 1: Azure.AI.OpenAI SDK Breaking Changes**
- **Likelihood:** Medium (SDK is stable but evolving)
- **Impact:** High (generator stops working)
- **Mitigation:**
  - Pin to minor version (2.1.*) in .csproj
  - Monitor SDK release notes for deprecations
  - Add integration tests to catch breaking changes early

#### **Risk 2: Size Mapping Confusion**
- **Likelihood:** High (users expect arbitrary sizes like FLUX.2)
- **Impact:** Medium (user frustration, support burden)
- **Mitigation:**
  - Clear XML documentation on `GenerateAsync`
  - CLI warning when size is mapped: `"Note: GPT-Image-1.5 mapped your request (800×800) to 1024×1024"`
  - Document in setup guide and sample README

#### **Risk 3: Testing Without SDK Mocks**
- **Likelihood:** High (SDK doesn't support `FakeHttpHandler`)
- **Impact:** Low (slower CI, requires credentials)
- **Mitigation:**
  - Use `[SkippableFact]` for integration tests
  - Document env var requirements in test file
  - Focus unit tests on logic we control (size mapping, validation)

#### **Risk 4: Endpoint Confusion (`.openai.azure.com` vs `.services.ai.azure.com`)**
- **Likelihood:** Medium (users copy-paste from FLUX.2/MAI docs)
- **Impact:** Medium (404 errors, support tickets)
- **Mitigation:**
  - Accept both endpoint formats, no auto-conversion (GPT uses `.openai`)
  - Error messages include hints: "GPT-Image-1.5 requires .openai.azure.com endpoint"
  - Setup guide has clear examples

#### **Risk 5: Deployment Name vs Model Name Confusion**
- **Likelihood:** Medium (Azure portal uses "deployment name", docs say "model")
- **Impact:** Low (easily resolved)
- **Mitigation:**
  - Use consistent terminology: `deploymentName` in code, "deployment" in docs
  - CLI config field remains `model` for consistency with FLUX.2/MAI
  - XML docs explain: "The deployment name you created in Azure portal"

### 10.2 Rollback Plan

**If integration fails:**
1. Revert PR before merge (no release impact)
2. If post-release: Publish v0.10.1 with generator removed, mark v0.10.0 unlisted on NuGet

**If SDK has critical bug:**
1. Downgrade to Azure.AI.OpenAI 2.0.x
2. Publish hotfix version (v0.10.2)
3. File issue with Azure SDK team

---

## 11. Success Criteria

### 11.1 Functional Requirements

✅ **FR-1:** Generator implements `IImageGenerator` and `Microsoft.Extensions.AI.IImageGenerator`  
✅ **FR-2:** Supports 1024×1024, 1792×1024, 1024×1792 sizes  
✅ **FR-3:** Maps arbitrary sizes to nearest supported size  
✅ **FR-4:** CLI adapter registered and discoverable via `t2i providers`  
✅ **FR-5:** Configuration stored in `config.json`, secrets in DPAPI/env vars  
✅ **FR-6:** Sample project demonstrates usage  
✅ **FR-7:** Error messages include helpful hints (deployment name, endpoint format)  

### 11.2 Non-Functional Requirements

✅ **NFR-1:** Code follows existing conventions (naming, error handling, DI)  
✅ **NFR-2:** Test coverage ≥80% for new code (unit tests)  
✅ **NFR-3:** No breaking changes to existing APIs  
✅ **NFR-4:** Documentation complete (XML docs, README, setup guide)  
✅ **NFR-5:** Build time increase <5 seconds (Azure SDK is pre-compiled)  
✅ **NFR-6:** NuGet package size increase <200KB  

### 11.3 Acceptance Test Scenarios

**Scenario 1: Developer Integration**
```csharp
// Can I add the generator to my project and generate an image?
using var generator = new GptImage1p5Generator(endpoint, apiKey);
var result = await generator.GenerateAsync("test prompt");
await result.SaveAsync("output.png");
// ✅ PASS if output.png exists and is valid PNG
```

**Scenario 2: CLI End-User**
```bash
# Can I configure and use the CLI without reading docs?
t2i config  # Interactive wizard
t2i "a cat astronaut"
# ✅ PASS if wizard prompts for endpoint/key, generates image
```

**Scenario 3: CI/CD Pipeline**
```bash
# Can I use env vars in a GitHub Action?
export T2I_FOUNDRY_GPT_IMAGE_1P5_ENDPOINT=...
export T2I_FOUNDRY_GPT_IMAGE_1P5_APIKEY=...
t2i --provider foundry-gpt-image-1p5 "logo"
# ✅ PASS if image generated without config files
```

---

## 12. Open Questions

### Q1: Should we support `dall-e-3` deployments?
**Context:** Azure OpenAI also supports DALL-E 3 via the same SDK. Should the generator be generic?

**Options:**
- A: Keep name `GptImage1p5Generator`, document that it works with DALL-E 3 too
- B: Rename to `AzureOpenAIImageGenerator`, make deployment name required
- C: Create separate `DallE3Generator` class

**Recommendation:** **Option A** — Keep `GptImage1p5Generator`, document compatibility. Rename if user feedback demands it (reversible decision).

### Q2: Should we warn users when size is mapped?
**Context:** User requests 512×512, generator uses 1024×1024. Silent or verbose?

**Options:**
- A: Silent mapping (matches FLUX.2 behavior)
- B: Log warning to console (CLI only)
- C: Add `ActualSize` property to result metadata

**Recommendation:** **Option C** — Add to result metadata, let CLI adapter log if sizes differ. Non-breaking, informative.

### Q3: Should health check call the API?
**Context:** `CheckAsync` currently only validates config exists. Should it call Azure to verify deployment?

**Options:**
- A: No API call (fast, matches MAI/FLUX behavior)
- B: Call `imageClient.GetDeploymentsAsync()` (slow, requires extra SDK method)
- C: Generate test image with timeout (expensive, slow)

**Recommendation:** **Option A** — No API call. Users will discover deployment issues on first `GenerateAsync`. `t2i doctor` is fast.

---

## 13. Next Steps

1. **Approval:** Bruno reviews this plan
2. **Spike:** Verify Azure.AI.OpenAI SDK works as expected (1 hour)
3. **Implementation:** Kaylee (Core Dev) implements Phase 1-3
4. **Code Review:** Mal reviews before merge
5. **Release:** Publish v0.10.0 (Foundry) and v0.11.0 (CLI) to NuGet
6. **Announcement:** Blog post / social media (Bruno)

---

## 14. Appendix

### 14.1 Relevant Documentation Links

- [Azure.AI.OpenAI SDK Docs](https://learn.microsoft.com/en-us/dotnet/api/azure.ai.openai)
- [GPT-Image-1.5 API Reference](https://learn.microsoft.com/en-us/azure/ai-services/openai/reference)
- [Microsoft.Extensions.AI GitHub](https://github.com/dotnet/extensions)
- [Project Architecture Doc](docs/architecture.md)

### 14.2 Code Review Checklist

**Before PR:**
- [ ] All unit tests pass (net8.0 + net10.0)
- [ ] Integration tests pass (if credentials available) OR skip gracefully
- [ ] XML documentation complete (all public members)
- [ ] No build warnings (treat warnings as errors)
- [ ] Code follows existing patterns (see Flux2Generator, MaiImage2Generator)
- [ ] Error messages are helpful (include hints, example values)
- [ ] Configuration documented (README.md, setup guide)
- [ ] Sample project runs successfully
- [ ] CHANGELOG.md updated
- [ ] NuGet package metadata updated (description, tags)
- [ ] Security: No hardcoded secrets in code or tests
- [ ] Performance: No obvious bottlenecks (SDK handles HTTP pooling)

**During Review:**
- [ ] Mal reviews for architectural consistency
- [ ] Kaylee reviews for code quality
- [ ] Bruno reviews for usability (sample project, CLI commands)

---

## 15. Conclusion

This plan provides a comprehensive roadmap for integrating GPT-Image-1.5 into ElBruno.Text2Image. The integration follows established architectural patterns, maintains backward compatibility, and leverages the official Azure.AI.OpenAI SDK for robust, maintainable code.

**Key Takeaways:**
- ✅ **Consistent Architecture:** Follows FLUX.2/MAI-Image-2 patterns (constructor, DI, CLI adapter)
- ✅ **SDK First:** Uses official Azure SDK for reliability and future-proofing
- ✅ **User-Friendly:** Clear error messages, size mapping with metadata, CLI wizard support
- ✅ **Well-Tested:** Unit tests for logic, integration tests for real API (skippable)
- ✅ **Documented:** Setup guide, sample project, XML docs, CHANGELOG entries

**Timeline:** 2-3 days for implementation, testing, and release.

**Risks:** Mitigated through SDK version pinning, clear documentation, and skippable integration tests.

**Next:** Await Bruno's approval, then proceed to implementation (Kaylee, Core Dev).

---

**Questions or feedback?** Reach out to Mal (Lead) or post in team chat.

---

*This plan will be merged into `.squad/decisions.md` after approval and implementation.*
