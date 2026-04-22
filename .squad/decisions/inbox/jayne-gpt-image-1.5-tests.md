# Test Strategy: GPT-Image-1.5 Generator Implementation

**Author:** Jayne (Tester)  
**Date:** 2025-01-20  
**Status:** Proposed  
**Target:** GPT-Image-1.5 generator using Azure.AI.Inference ImageClient

---

## Executive Summary

This document defines a comprehensive test strategy for the GPT-Image-1.5 generator implementation. The strategy follows established patterns from Flux2 and MAI-Image-2 generators while addressing Azure.AI.Inference SDK-specific concerns.

**Coverage Goal:** 85% minimum (class level), 90% target. If it's not tested, it doesn't work.

---

## 1. Current Test Patterns Analysis

### 1.1 Established Testing Infrastructure

**From Flux2GeneratorHttpTests.cs:**
- **FakeHttpHandler pattern:** Intercepts HttpClient requests to verify headers, body, Content-Length
- **Content-Length validation:** Critical for BFL API (rejects chunked encoding) — ByteArrayContent usage
- **JSON body inspection:** Parse request body to verify prompt, model, dimensions, format
- **Error handling:** HttpStatusCode tests (BadRequest, NotFound) with hint messages
- **Reference images:** Base64 data URI testing, AddReferenceImageFromFile validation

**From MaiImage2GeneratorHttpTests.cs:**
- **Similar FakeHttpHandler approach:** Content-Length, JSON structure, api-key header
- **Validation tests:** Null/empty endpoint/apiKey, HTTP-only endpoint rejection, prompt length limits (32,000 chars)
- **Endpoint building:** Base URL auto-append `/mai/v1/images/generations`, full URL as-is
- **Default dimensions:** MAI uses 1024x1024, FLUX uses 512x512

**Common Test Structure:**
- **Test class organization:** Group by concern (ContentLength, Response, Validation, Endpoint, Model)
- **Async patterns:** All generation tests use `async Task` with xUnit
- **Disposable resources:** `using` statements for HttpClient, generators
- **Minimal PNG payload:** `new byte[] { 0x89, 0x50, 0x4E, 0x47 }` for base64 responses

**Shared Test Utilities:**
- **FakeHttpHandler:** Captures LastRequest, LastRequestBody, returns canned responses
- **FakeSecretStore:** In-memory ISecretStore with IsAvailable toggle (for CLI adapter testing)
- **Temp directory isolation:** Prevents ConfigStore file lock conflicts (Guid-based paths)
- **ServiceCollection DI:** For CLI adapter testing with IHttpClientFactory

**InternalsVisibleTo:**
- Test project accesses internal classes via `[assembly: InternalsVisibleTo("ElBruno.Text2Image.Tests")]`
- Required for testing internal request/response types

### 1.2 Test Naming Conventions

Pattern: `{MethodUnderTest}_{Scenario}_{ExpectedOutcome}`

Examples:
- `GenerateAsync_Request_HasContentLengthHeader`
- `Constructor_NullEndpoint_Throws`
- `GenerateAsync_PromptExceedsMaxLength_Throws`
- `AddReferenceImageFromFile_UnknownExtension_UsesOctetStream`

### 1.3 Coverage Metrics (Baseline)

Current test counts:
- **Flux2 HTTP:** 43+ tests
- **MAI-Image-2 HTTP:** 32+ tests
- **CLI Infrastructure:** 50 tests (secrets, config, console helpers, commands)
- **Total:** 238 tests pass (net10.0), 166 pass (net8.0)

---

## 2. GPT-Image-1.5 Generator Test Scenarios

### 2.1 Unit Test Matrix

#### 2.1.1 Constructor Validation Tests (GptImage1p5GeneratorValidationTests)

| Test Case | Input | Expected Outcome |
|-----------|-------|------------------|
| `Constructor_NullEndpoint_Throws` | endpoint: null | ArgumentException/ArgumentNullException |
| `Constructor_EmptyEndpoint_Throws` | endpoint: "" | ArgumentException |
| `Constructor_WhitespaceEndpoint_Throws` | endpoint: "   " | ArgumentException |
| `Constructor_NullApiKey_Throws` | apiKey: null | ArgumentException/ArgumentNullException |
| `Constructor_EmptyApiKey_Throws` | apiKey: "" | ArgumentException |
| `Constructor_WhitespaceApiKey_Throws` | apiKey: "   " | ArgumentException |
| `Constructor_HttpEndpoint_Throws` | endpoint: "http://..." | ArgumentException (require HTTPS) |
| `Constructor_NullDeployment_Throws` | deployment: null | ArgumentException/ArgumentNullException |
| `Constructor_EmptyDeployment_Throws` | deployment: "" | ArgumentException |
| `Constructor_ValidParams_Succeeds` | Valid endpoint/key/deployment | Instance created |

**Rationale:** Azure.AI.Inference ImageClient requires endpoint, deployment, and API key. Validate early to fail fast.

#### 2.1.2 Request Validation Tests (GptImage1p5GeneratorPromptValidationTests)

| Test Case | Input | Expected Outcome |
|-----------|-------|------------------|
| `GenerateAsync_NullPrompt_Throws` | prompt: null | ArgumentException/ArgumentNullException |
| `GenerateAsync_EmptyPrompt_Throws` | prompt: "" | ArgumentException |
| `GenerateAsync_WhitespacePrompt_Throws` | prompt: "   " | ArgumentException |
| `GenerateAsync_PromptExceedsMaxLength_Throws` | prompt: 4001+ chars | ArgumentOutOfRangeException |
| `GenerateAsync_ValidPrompt_Succeeds` | prompt: "a cat" | ImageGenerationResult |

**Note:** GPT-Image-1.5 max prompt length is 4000 characters (verify against official docs).

#### 2.1.3 Size Parameter Tests (GptImage1p5GeneratorSizeTests)

| Test Case | Input | Expected Outcome |
|-----------|-------|------------------|
| `GenerateAsync_DefaultOptions_Uses1024x1024` | No options | Request contains "1024x1024" |
| `GenerateAsync_ValidSize1024x1024_Succeeds` | size: "1024x1024" | Request contains size |
| `GenerateAsync_ValidSize1792x1024_Succeeds` | size: "1792x1024" | Request contains size |
| `GenerateAsync_ValidSize1024x1792_Succeeds` | size: "1024x1792" | Request contains size |
| `GenerateAsync_InvalidSize_Throws` | size: "512x512" | ArgumentException (unsupported) |
| `GenerateAsync_CustomSize_AppearsInRequest` | Custom supported size | Verify in request body |

**Rationale:** GPT-Image-1.5 supports limited size options (1024x1024, 1792x1024, 1024x1792). Test valid and invalid cases.

#### 2.1.4 HTTP Request Structure Tests (GptImage1p5GeneratorRequestTests)

**Note:** Azure.AI.Inference SDK abstracts HTTP layer, so direct HTTP inspection may not be feasible like Flux2/MAI tests. Alternative approach:

- **If SDK exposes HttpClient:** Use FakeHttpHandler to capture requests
- **If SDK is sealed:** Test via public API behavior (prompt → response), integration tests for HTTP

**Preferred Tests (if HttpClient injectable):**

| Test Case | Verification |
|-----------|--------------|
| `GenerateAsync_Request_UsesPostMethod` | Assert POST method |
| `GenerateAsync_Request_HasApiKeyHeader` | Assert api-key or Authorization header |
| `GenerateAsync_Request_ContainsPrompt` | Parse JSON body → verify prompt field |
| `GenerateAsync_Request_ContainsSize` | Parse JSON body → verify size field |
| `GenerateAsync_Request_ContainsModel` | Parse JSON body → verify model/deployment |
| `GenerateAsync_Request_ContentTypeIsJsonUtf8` | Assert application/json; charset=utf-8 |

**If SDK is opaque:** Skip HTTP-level tests, rely on integration tests with real endpoint (or mock ImageClient if possible).

#### 2.1.5 Response Parsing Tests (GptImage1p5GeneratorResponseTests)

| Test Case | Mock Response | Expected Outcome |
|-----------|---------------|------------------|
| `GenerateAsync_SuccessResponse_ReturnsResult` | Valid ImageGenerationResult | Non-null result with bytes |
| `GenerateAsync_SuccessResponse_ParsesBinaryData` | BinaryData with PNG bytes | result.ImageBytes not empty |
| `GenerateAsync_SuccessResponse_PopulatesMetadata` | Valid response | result.Prompt, ModelName set |
| `GenerateAsync_SuccessResponse_SetsCorrectDimensions` | 1024x1024 response | result.Width = 1024, Height = 1024 |

**Implementation Note:** Azure.AI.Inference SDK returns `ImageGenerationResult` with `BinaryData`. Test parsing to byte array and metadata extraction.

#### 2.1.6 Error Handling Tests (GptImage1p5GeneratorErrorTests)

| Test Case | Mock Scenario | Expected Outcome |
|-----------|---------------|------------------|
| `GenerateAsync_BadRequest_ThrowsException` | HTTP 400 | HttpRequestException or SDK-specific exception |
| `GenerateAsync_Unauthorized_ThrowsException` | HTTP 401 | Exception with auth hint |
| `GenerateAsync_NotFound_ThrowsException` | HTTP 404 | Exception with deployment hint |
| `GenerateAsync_RateLimited_ThrowsException` | HTTP 429 | Exception with retry hint |
| `GenerateAsync_ServerError_ThrowsException` | HTTP 500 | Exception with server error message |
| `GenerateAsync_Timeout_ThrowsException` | Network timeout | TaskCanceledException or TimeoutException |
| `GenerateAsync_InvalidJson_ThrowsException` | Malformed response | JsonException or SDK exception |

**Hint Messages:**
- 404: "Deployment '{deployment}' not found. Verify deployment name in Azure AI Foundry."
- 401: "Invalid API key. Check credentials with: t2i config"
- 429: "Rate limit exceeded. Wait and retry."

#### 2.1.7 Async/Await Pattern Tests

| Test Case | Verification |
|-----------|--------------|
| `GenerateAsync_ReturnsTask` | Method signature returns Task<ImageGenerationResult> |
| `GenerateAsync_SupportsCancellation` | Pass CancellationToken, verify cancellation |
| `GenerateAsync_CancellationToken_PropagatesCancel` | Cancel token → OperationCanceledException |

#### 2.1.8 File I/O Tests (SaveAsync behavior)

| Test Case | Scenario | Expected Outcome |
|-----------|----------|------------------|
| `SaveAsync_ValidPath_WritesFile` | Save to temp file | File exists, PNG header present |
| `SaveAsync_InvalidPath_Throws` | Save to nonexistent dir | DirectoryNotFoundException or IOException |
| `SaveAsync_ReadOnlyLocation_Throws` | Save to read-only path | UnauthorizedAccessException |
| `SaveAsync_Overwrites_ExistingFile` | Save to existing file | File replaced |

**Note:** If SaveAsync is not part of GptImage1p5Generator, test via ImageGenerationResult.SaveAsync (shared implementation).

### 2.2 Edge Cases & Negative Tests Checklist

**Input Edge Cases:**
- [ ] Null prompt
- [ ] Empty prompt
- [ ] Whitespace-only prompt
- [ ] Extremely long prompt (4000+ chars)
- [ ] Prompt with special characters (Unicode, emoji, newlines)
- [ ] Prompt with JSON special chars (`"`, `\`, control chars)
- [ ] Null/empty/whitespace endpoint
- [ ] Null/empty/whitespace API key
- [ ] Null/empty/whitespace deployment name
- [ ] HTTP endpoint (non-HTTPS)
- [ ] Invalid size strings ("abc", "512", "1024", "2048x2048")
- [ ] Null ImageGenerationOptions (should use defaults)

**API Response Edge Cases:**
- [ ] Empty BinaryData
- [ ] Null BinaryData
- [ ] Malformed JSON response
- [ ] Missing required fields in response
- [ ] Response with error field set
- [ ] Partial response (truncated)
- [ ] Non-PNG binary data (if supported)

**Network Edge Cases:**
- [ ] Network timeout (long-running request)
- [ ] Connection refused
- [ ] DNS resolution failure
- [ ] SSL/TLS errors (cert validation)
- [ ] Proxy authentication required

**File I/O Edge Cases:**
- [ ] Disk full (simulate IOException)
- [ ] Path too long (PathTooLongException)
- [ ] Invalid path characters
- [ ] Permission denied (write-protected directory)
- [ ] File already open by another process (sharing violation)

---

## 3. Integration Test Strategy

### 3.1 Prerequisites

**Azure Resources Required:**
- Azure AI Foundry project with GPT-Image-1.5 deployment
- Valid API key (user secrets or CI/CD secrets)
- Endpoint URL (e.g., `https://{project}.services.ai.azure.com`)

**Secret Management:**
- **Local Dev:** User secrets (`dotnet user-secrets set "AzureAI:ApiKey" "..."`)
- **CI/CD:** Environment variables or GitHub secrets
- **Test Isolation:** Use separate deployment for tests (avoid production quota)

### 3.2 Integration Test Scenarios

#### 3.2.1 End-to-End Generation Tests (GptImage1p5GeneratorIntegrationTests)

**Test Class Attributes:**
```csharp
[Collection("Integration")]
[Trait("Category", "Integration")]
public class GptImage1p5GeneratorIntegrationTests : IDisposable
{
    // Skip if secrets unavailable: [Fact(Skip = "Integration test - requires Azure credentials")]
    // OR use [ConditionalFact] with environment variable check
}
```

**Scenarios:**

| Test Case | Input | Verification |
|-----------|-------|--------------|
| `GenerateAsync_SimplePrompt_ProducesImage` | "a red apple" | File written, PNG header, size > 1KB |
| `GenerateAsync_ComplexPrompt_ProducesImage` | "A futuristic city at sunset..." | File written, valid PNG |
| `GenerateAsync_Size1792x1024_ProducesImage` | size: "1792x1024" | File dimensions match |
| `GenerateAsync_Size1024x1792_ProducesImage` | size: "1024x1792" | File dimensions match |
| `GenerateAsync_MultipleRequests_AllSucceed` | 3 sequential calls | All produce valid images |
| `GenerateAsync_RealEndpoint_ReturnsMetadata` | Valid request | Verify prompt, model, dimensions in result |

**Cost Considerations:**
- Mark integration tests with `[Trait("Category", "Integration")]` → exclude from default test runs
- Run integration tests manually or in nightly CI builds
- Limit integration test count to avoid quota/cost explosion
- Use smallest size (1024x1024) to minimize cost

#### 3.2.2 Rate Limiting & Retry Tests

**Scenarios:**
- Test rate limit handling (429 response) if SDK supports retry policy
- Verify exponential backoff behavior (if implemented)
- Test concurrent request limits (Azure API limits)

**Note:** May require manual throttling or dedicated test deployment with low quotas.

#### 3.2.3 Configuration Integration Tests

| Test Case | Setup | Verification |
|-----------|-------|--------------|
| `LoadFromUserSecrets_Succeeds` | User secrets configured | Generator initializes |
| `LoadFromEnvVars_Succeeds` | Env vars set | Generator initializes |
| `MissingSecrets_ThrowsConfigException` | No secrets | Exception with hint |

---

## 4. Mock/Fake Implementation Approach

### 4.1 Azure.AI.Inference SDK Challenges

**Problem:** Azure.AI.Inference SDK uses `ImageClient` class, which may not be mockable:
- Class may be sealed → can't use Moq/NSubstitute
- Constructor may require real credentials → can't easily fake

**Solution Options:**

#### Option A: Wrapper Interface (Preferred)

Create `IImageClient` abstraction in production code:

```csharp
public interface IImageClient : IDisposable
{
    Task<ImageGenerationResult> GenerateImageAsync(string prompt, ImageGenerationOptions? options, CancellationToken ct);
}

internal sealed class AzureImageClientAdapter : IImageClient
{
    private readonly ImageClient _client;
    
    public AzureImageClientAdapter(string endpoint, string deployment, string apiKey)
    {
        _client = new ImageClient(endpoint, deployment, new AzureKeyCredential(apiKey));
    }
    
    public Task<ImageGenerationResult> GenerateImageAsync(string prompt, ImageGenerationOptions? options, CancellationToken ct)
        => _client.GenerateImageAsync(prompt, options, ct);
    
    public void Dispose() => _client.Dispose();
}
```

**Test Implementation:**

```csharp
internal sealed class FakeImageClient : IImageClient
{
    public string? LastPrompt { get; private set; }
    public ImageGenerationOptions? LastOptions { get; private set; }
    public Func<string, ImageGenerationOptions?, ImageGenerationResult>? ResponseFactory { get; set; }
    
    public Task<ImageGenerationResult> GenerateImageAsync(string prompt, ImageGenerationOptions? options, CancellationToken ct)
    {
        LastPrompt = prompt;
        LastOptions = options;
        
        if (ResponseFactory != null)
            return Task.FromResult(ResponseFactory(prompt, options));
        
        // Default: return minimal fake response
        return Task.FromResult(CreateFakeResult(prompt, options?.Size ?? "1024x1024"));
    }
    
    private static ImageGenerationResult CreateFakeResult(string prompt, string size)
    {
        // Create minimal PNG bytes
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var binaryData = BinaryData.FromBytes(pngBytes);
        
        // Construct ImageGenerationResult (may require reflection or internal access)
        // OR return null if SDK doesn't allow construction → forces integration tests
    }
    
    public void Dispose() { }
}
```

**Trade-offs:**
- **Pros:** Full control over fake behavior, testable without Azure
- **Cons:** Adds abstraction layer, need InternalsVisibleTo for internal types

#### Option B: HttpClient Injection (If SDK Supports)

If Azure.AI.Inference SDK accepts HttpClient:

```csharp
var handler = new FakeHttpHandler(_ => CreateGptImageSuccessResponse());
using var httpClient = new HttpClient(handler);
using var client = new ImageClient(endpoint, deployment, credential, new ImageClientOptions { Transport = new HttpClientTransport(httpClient) });
```

**Test Pattern:** Same as Flux2/MAI-Image-2 tests (FakeHttpHandler).

**Note:** Verify if Azure.AI.Inference SDK supports custom HttpClient/Transport. Check SDK docs.

#### Option C: Integration Tests Only (Fallback)

If SDK is sealed and doesn't support mocking:
- Skip unit-level HTTP tests
- Rely on integration tests with real Azure endpoint
- Test validation/parsing logic separately (extract to testable methods)

**Recommendation:** **Option A (Wrapper Interface)** for maximum test coverage and isolation.

### 4.2 FakeSecretStore for CLI Adapter Tests

Use existing `FakeSecretStore` pattern for testing CLI adapter:

```csharp
public class GptImage1p5AdapterTests
{
    [Fact]
    public async Task GenerateAsync_ValidConfig_CallsImageClient()
    {
        var secretStore = new FakeSecretStore { Name = "test" };
        secretStore.Data[("foundry-gpt-image-1.5", "apiKey")] = "test-key";
        secretStore.Data[("foundry-gpt-image-1.5", "endpoint")] = "https://test.services.ai.azure.com";
        secretStore.Data[("foundry-gpt-image-1.5", "deployment")] = "gpt-image-15";
        
        var resolver = new SecretResolver(new[] { secretStore });
        var configStore = new ConfigStore();
        var httpFactory = CreateFakeHttpClientFactory();
        
        var adapter = new GptImage1p5Adapter(httpFactory, resolver, configStore);
        
        var result = await adapter.GenerateAsync(
            new GenerationRequest("test prompt", 1024, 1024, 0, "output.png", new Dictionary<string, string?>()),
            null,
            CancellationToken.None);
        
        Assert.NotNull(result);
        Assert.Equal("output.png", result.OutputPath);
    }
}
```

---

## 5. Test File Structure & Naming

### 5.1 Test File Organization

**New Files to Create:**

```
src/ElBruno.Text2Image.Tests/
├── GptImage1p5GeneratorTests.cs                # Main unit tests (if HttpClient injectable)
│   └── Test classes:
│       ├── GptImage1p5GeneratorValidationTests       # Constructor & config validation
│       ├── GptImage1p5GeneratorPromptValidationTests # Prompt validation
│       ├── GptImage1p5GeneratorSizeTests             # Size parameter tests
│       ├── GptImage1p5GeneratorRequestTests          # HTTP request structure (if applicable)
│       ├── GptImage1p5GeneratorResponseTests         # Response parsing
│       └── GptImage1p5GeneratorErrorTests            # Error handling
│
├── GptImage1p5GeneratorIntegrationTests.cs     # Integration tests (Azure endpoint)
│   └── Test classes:
│       ├── GptImage1p5GeneratorEndToEndTests         # E2E generation tests
│       └── GptImage1p5GeneratorConfigTests           # Config/secrets integration
│
├── Cli/
│   └── Providers/
│       └── GptImage1p5AdapterTests.cs         # CLI adapter tests
│           └── Test classes:
│               ├── GptImage1p5AdapterValidationTests # Config/secret validation
│               ├── GptImage1p5AdapterGenerationTests # Generation via adapter
│               └── GptImage1p5AdapterHealthTests     # CheckAsync tests
│
└── Helpers/
    └── FakeImageClient.cs                     # Mock IImageClient (if wrapper used)
```

### 5.2 Test Class Naming Convention

Pattern: `{ComponentUnderTest}{Concern}Tests`

Examples:
- `GptImage1p5GeneratorValidationTests` — Constructor & config validation
- `GptImage1p5GeneratorSizeTests` — Size parameter handling
- `GptImage1p5AdapterGenerationTests` — CLI adapter generation logic

### 5.3 Conditional Compilation

**For net8.0 vs net10.0 differences:**

```csharp
#if NET10_0_OR_GREATER
public class GptImage1p5AdapterTests
{
    // CLI tests only run on net10.0 (CLI targets net10.0 only)
}
#endif
```

**For integration tests:**

```csharp
[Fact(Skip = "Integration test - requires Azure credentials")]
public async Task GenerateAsync_RealEndpoint_ProducesImage()
{
    // ...
}
```

OR use conditional test execution:

```csharp
[ConditionalFact(Skip = "Integration")]
public async Task GenerateAsync_RealEndpoint_ProducesImage()
{
    // Runs only if environment variable ENABLE_INTEGRATION_TESTS=true
}
```

---

## 6. Coverage Goals & Metrics

### 6.1 Coverage Targets

**Minimum Coverage (Required):**
- **Class-level:** 85%
- **Branch coverage:** 75%

**Target Coverage (Aspirational):**
- **Class-level:** 90%
- **Branch coverage:** 80%

**Critical Paths (100% Coverage):**
- Constructor validation (null/empty checks)
- Prompt validation (null/empty/length)
- Error handling (HTTP errors, timeouts)
- Response parsing (BinaryData to byte array)

### 6.2 Coverage Exclusions

**Acceptable to exclude:**
- SDK-internal code (Azure.AI.Inference internals)
- Logging statements (if using ILogger)
- Dispose patterns (if minimal logic)

### 6.3 Coverage Measurement

**Tools:**
- `dotnet test --collect:"XPlat Code Coverage"`
- ReportGenerator for HTML reports
- CI/CD: Upload coverage to Codecov or SonarQube

**CI/CD Gate:**
- Fail build if coverage drops below 85% (class-level)
- Block PR if new code is untested (require >80% coverage for new files)

---

## 7. CLI & Sample Testing

### 7.1 CLI Command Tests

**GenerateCommand Tests (E2E):**

```csharp
[Fact]
public async Task GenerateCommand_GptImage1p5Provider_GeneratesImage()
{
    // Setup: Configure provider via ConfigStore
    var config = new AppConfig
    {
        DefaultProvider = "foundry-gpt-image-1.5",
        Providers =
        {
            ["foundry-gpt-image-1.5"] = new ProviderConfig
            {
                Endpoint = "https://test.services.ai.azure.com",
                Deployment = "gpt-image-15"
            }
        }
    };
    
    var secretStore = new FakeSecretStore();
    secretStore.Data[("foundry-gpt-image-1.5", "apiKey")] = "test-key";
    
    // Execute: Run GenerateCommand
    var settings = new GenerateCommand.Settings
    {
        Prompt = "a red apple",
        Provider = "foundry-gpt-image-1.5",
        Output = "output.png"
    };
    
    var command = new GenerateCommand(providerRegistry, secretResolver, configStore, console);
    var exitCode = await command.ExecuteAsync(settings);
    
    // Verify: Exit code 0, file written
    Assert.Equal(0, exitCode);
    Assert.True(File.Exists("output.png"));
}
```

**ConfigCommand Tests:**

```csharp
[Fact]
public async Task ConfigSetCommand_GptImage1p5_PersistsEndpoint()
{
    var command = new ConfigCommand(configStore);
    
    await command.SetAsync("foundry-gpt-image-1.5.endpoint", "https://my.services.ai.azure.com");
    
    var config = await configStore.LoadAsync(CancellationToken.None);
    Assert.Equal("https://my.services.ai.azure.com", config.Providers["foundry-gpt-image-1.5"].Endpoint);
}

[Fact]
public async Task ConfigSetCommand_GptImage1p5_PersistsDeployment()
{
    var command = new ConfigCommand(configStore);
    
    await command.SetAsync("foundry-gpt-image-1.5.deployment", "my-gpt-image-deployment");
    
    var config = await configStore.LoadAsync(CancellationToken.None);
    Assert.Equal("my-gpt-image-deployment", config.Providers["foundry-gpt-image-1.5"].Deployment);
}
```

**SecretsCommand Tests:**

```csharp
[Fact]
public async Task SecretsSetCommand_GptImage1p5_StoresApiKey()
{
    var secretStore = new FakeSecretStore();
    var resolver = new SecretResolver(new[] { secretStore });
    var command = new SecretsCommand(resolver);
    
    await command.SetAsync("foundry-gpt-image-1.5", "apiKey", "test-key-12345");
    
    Assert.Equal("test-key-12345", secretStore.Data[("foundry-gpt-image-1.5", "apiKey")]);
}
```

**DoctorCommand Tests:**

```csharp
[Fact]
public async Task DoctorCommand_GptImage1p5Configured_ShowsHealthy()
{
    // Setup: Configure provider with valid credentials
    var adapter = new GptImage1p5Adapter(...);
    var registry = new ProviderRegistry(new[] { adapter });
    var command = new DoctorCommand(registry);
    
    var health = await adapter.CheckAsync(CancellationToken.None);
    
    Assert.True(health.Ok);
}

[Fact]
public async Task DoctorCommand_GptImage1p5MissingApiKey_ShowsUnhealthy()
{
    // Setup: Missing API key
    var secretStore = new FakeSecretStore();
    var resolver = new SecretResolver(new[] { secretStore });
    var adapter = new GptImage1p5Adapter(httpFactory, resolver, configStore);
    
    var health = await adapter.CheckAsync(CancellationToken.None);
    
    Assert.False(health.Ok);
    Assert.Contains("Missing", health.Reason);
}
```

### 7.2 Sample Project Validation

**Sample Scenarios:**

1. **scenario-XX-gpt-image-1.5-basic:**
   - Simple prompt → image generation
   - Verify: Runs without errors, produces output.png

2. **scenario-XX-gpt-image-1.5-sizes:**
   - Test all supported sizes (1024x1024, 1792x1024, 1024x1792)
   - Verify: Each size produces correct dimensions

3. **scenario-XX-gpt-image-1.5-cli:**
   - CLI command: `t2i "a futuristic city" --provider foundry-gpt-image-1.5 --size 1792x1024`
   - Verify: Image generated, metadata correct

**Automated Sample Tests:**

```bash
# Add to CI/CD pipeline
dotnet run --project samples/scenario-XX-gpt-image-1.5-basic
if [ ! -f output.png ]; then
  echo "Sample failed: output.png not created"
  exit 1
fi
```

### 7.3 User Secrets Integration Tests

**Local Dev Scenario:**

```csharp
[Fact(Skip = "Requires user secrets configured locally")]
public async Task GenerateAsync_WithUserSecrets_Succeeds()
{
    // Read from user secrets (dotnet user-secrets set "AzureAI:ApiKey" "...")
    var config = new ConfigurationBuilder()
        .AddUserSecrets<GptImage1p5GeneratorIntegrationTests>()
        .Build();
    
    var endpoint = config["AzureAI:Endpoint"];
    var deployment = config["AzureAI:Deployment"];
    var apiKey = config["AzureAI:ApiKey"];
    
    using var generator = new GptImage1p5Generator(endpoint, deployment, apiKey);
    
    var result = await generator.GenerateAsync("a red apple");
    
    Assert.NotNull(result);
    Assert.NotEmpty(result.ImageBytes);
}
```

---

## 8. Special Setup Requirements

### 8.1 Azure AI Foundry Setup

**Prerequisites:**
1. Azure subscription
2. Azure AI Foundry project created
3. GPT-Image-1.5 model deployed (deployment name configured)
4. API key generated

**Configuration:**

**User Secrets (Local Dev):**
```bash
dotnet user-secrets set "AzureAI:Endpoint" "https://{project}.services.ai.azure.com"
dotnet user-secrets set "AzureAI:Deployment" "gpt-image-15"
dotnet user-secrets set "AzureAI:ApiKey" "{your-api-key}"
```

**Environment Variables (CI/CD):**
```bash
export AZURE_AI_ENDPOINT="https://{project}.services.ai.azure.com"
export AZURE_AI_DEPLOYMENT="gpt-image-15"
export AZURE_AI_API_KEY="{your-api-key}"
```

**GitHub Secrets (CI/CD):**
- `AZURE_AI_ENDPOINT`
- `AZURE_AI_DEPLOYMENT`
- `AZURE_AI_API_KEY`

### 8.2 Test Data Requirements

**Prompts:**
- Simple: "a red apple"
- Complex: "A futuristic city at sunset with flying cars and neon lights"
- Edge case: 4000-character prompt (generate programmatically)
- Special chars: "A cat 🐱 with \"quotes\" and backslashes \\"

**Expected Outputs:**
- PNG files (verify magic bytes: `0x89 0x50 0x4E 0x47`)
- Minimum size: 1KB (avoid empty files)
- Valid dimensions: 1024x1024, 1792x1024, 1024x1792

### 8.3 CI/CD Integration

**Test Execution Strategy:**

1. **Unit Tests:** Always run (fast, no Azure dependency)
2. **Integration Tests:** Run on:
   - Nightly builds
   - Manual trigger (GitHub Actions workflow_dispatch)
   - Pre-release validation

**GitHub Actions Workflow:**

```yaml
name: Integration Tests

on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM daily
  workflow_dispatch:

jobs:
  integration:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Run integration tests
        env:
          AZURE_AI_ENDPOINT: ${{ secrets.AZURE_AI_ENDPOINT }}
          AZURE_AI_DEPLOYMENT: ${{ secrets.AZURE_AI_DEPLOYMENT }}
          AZURE_AI_API_KEY: ${{ secrets.AZURE_AI_API_KEY }}
        run: |
          dotnet test --filter "Category=Integration" --logger "trx;LogFileName=integration-results.trx"
      
      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: integration-test-results
          path: '**/integration-results.trx'
```

**Cost Control:**
- Limit integration tests to 10 scenarios max
- Use smallest size (1024x1024)
- Run nightly (not on every commit)
- Monitor Azure costs via alerts

---

## 9. Test Implementation Checklist

### 9.1 Phase 1: Validation & Structure Tests (Day 1-2)

- [ ] Create `GptImage1p5GeneratorValidationTests.cs`
  - [ ] Constructor validation (10 tests)
  - [ ] Prompt validation (5 tests)
- [ ] Create `GptImage1p5GeneratorSizeTests.cs`
  - [ ] Size parameter tests (6 tests)
- [ ] Create `FakeImageClient.cs` (if wrapper approach used)
- [ ] Verify test naming conventions match project standards

### 9.2 Phase 2: Request/Response Tests (Day 3-4)

- [ ] Create `GptImage1p5GeneratorResponseTests.cs`
  - [ ] Response parsing (4 tests)
  - [ ] Metadata extraction (3 tests)
- [ ] Create `GptImage1p5GeneratorErrorTests.cs`
  - [ ] HTTP error handling (7 tests)
  - [ ] Timeout tests (1 test)
  - [ ] Invalid JSON tests (1 test)
- [ ] Add hint messages for common errors (404, 401, 429)

### 9.3 Phase 3: Integration Tests (Day 5-6)

- [ ] Create `GptImage1p5GeneratorIntegrationTests.cs`
  - [ ] E2E generation tests (6 tests)
  - [ ] Multiple sizes (3 tests)
  - [ ] Real endpoint validation (1 test)
- [ ] Setup user secrets for local testing
- [ ] Document Azure setup in README
- [ ] Add `[Trait("Category", "Integration")]` to all integration tests

### 9.4 Phase 4: CLI Adapter Tests (Day 7-8)

- [ ] Create `GptImage1p5AdapterTests.cs`
  - [ ] Config validation (5 tests)
  - [ ] Secret resolution (4 tests)
  - [ ] Generation via adapter (3 tests)
  - [ ] Health check tests (3 tests)
- [ ] Test CLI commands (GenerateCommand, ConfigCommand, SecretsCommand)
- [ ] Verify temp directory isolation for config tests

### 9.5 Phase 5: Sample & Documentation (Day 9)

- [ ] Create sample project: `scenario-XX-gpt-image-1.5-basic`
- [ ] Create sample project: `scenario-XX-gpt-image-1.5-sizes`
- [ ] Create sample project: `scenario-XX-gpt-image-1.5-cli`
- [ ] Document user secrets setup in samples README
- [ ] Add integration test execution guide

### 9.6 Phase 6: CI/CD & Coverage (Day 10)

- [ ] Add integration test workflow to GitHub Actions
- [ ] Configure GitHub secrets (AZURE_AI_*)
- [ ] Run coverage report (target: 85%+)
- [ ] Fix coverage gaps if below 85%
- [ ] Document coverage metrics in team decisions

---

## 10. Risk Assessment & Mitigation

### 10.1 Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Azure.AI.Inference SDK is sealed/unmockable | High | Medium | Use wrapper interface (IImageClient) |
| Integration tests exceed Azure quota | Medium | High | Limit test count, run nightly only |
| SDK behavior changes in updates | High | Low | Pin SDK version, test on upgrades |
| User secrets not configured locally | Low | Medium | Provide setup docs, skip tests gracefully |
| CI/CD secrets exposed in logs | Critical | Low | Mask secrets, audit logs |
| Flaky integration tests (network) | Medium | Medium | Add retry logic, timeout handling |

### 10.2 Mitigation Strategies

**SDK Mockability:**
- **Primary:** Wrapper interface (`IImageClient`) with fake implementation
- **Fallback:** Integration tests only, test validation logic separately

**Cost Control:**
- Mark integration tests with `[Trait("Category", "Integration")]`
- Exclude from default test runs (`dotnet test --filter "Category!=Integration"`)
- Run on schedule (nightly) or manual trigger
- Monitor Azure costs weekly

**SDK Updates:**
- Pin `Azure.AI.Inference` version in `.csproj`
- Test on SDK updates before upgrading
- Document breaking changes in team decisions

**Flaky Tests:**
- Use `[Retry]` attribute (if available in xUnit extensions)
- Add timeout policies (5s default, 30s for integration)
- Log detailed error messages (include HTTP response body)

---

## 11. Success Criteria

### 11.1 Definition of Done

**Test Coverage:**
- ✅ Class-level coverage ≥ 85%
- ✅ Branch coverage ≥ 75%
- ✅ All critical paths at 100% coverage

**Test Quality:**
- ✅ All tests follow naming conventions
- ✅ No flaky tests (0% flakiness over 10 runs)
- ✅ Integration tests run successfully in CI/CD
- ✅ User secrets setup documented

**Documentation:**
- ✅ Test strategy reviewed and approved
- ✅ Sample projects validated
- ✅ README updated with integration test setup

### 11.2 Acceptance Criteria

**Unit Tests:**
- [ ] 60+ unit tests passing (net10.0)
- [ ] 0 skipped tests (except integration)
- [ ] 0 build warnings

**Integration Tests:**
- [ ] 10+ integration tests passing (with Azure)
- [ ] Can run locally with user secrets
- [ ] Can run in CI/CD with GitHub secrets

**CLI Tests:**
- [ ] 15+ CLI adapter tests passing
- [ ] GenerateCommand works with GPT-Image-1.5 provider
- [ ] ConfigCommand persists endpoint/deployment
- [ ] SecretsCommand stores API key

**Samples:**
- [ ] 3 sample projects run without errors
- [ ] All samples produce valid PNG outputs
- [ ] Sample READMEs include setup instructions

---

## 12. Open Questions & Decisions Needed

### 12.1 Questions for Team

1. **SDK Mockability:** Can Azure.AI.Inference SDK accept HttpClient injection?
   - **Action:** Spike: Test if `ImageClient` supports custom transport
   - **Owner:** Kaylee (Core Dev) — investigate SDK constructor options

2. **Size Parameter Format:** Does SDK accept "1024x1024" string or separate Width/Height int?
   - **Action:** Check Azure.AI.Inference SDK docs or ImageGenerationOptions API
   - **Owner:** River (AI/ML) — verify API contract

3. **Deployment Name vs Model Name:** Is deployment name required separately from model?
   - **Action:** Verify Azure AI Foundry deployment model
   - **Owner:** Wash (Backend) — test with real Azure endpoint

4. **Prompt Length Limit:** Is 4000 chars the correct GPT-Image-1.5 limit?
   - **Action:** Confirm with Azure docs or API error messages
   - **Owner:** River (AI/ML) — validate against official specs

5. **Cost Budget:** What's the acceptable monthly cost for integration tests?
   - **Action:** Define budget cap ($10/month?)
   - **Owner:** Bruno (Product Owner) — approve budget

### 12.2 Decisions Required

**Decision 1: Mock Strategy**
- **Options:** Wrapper interface vs HttpClient injection vs integration-only
- **Recommendation:** Wrapper interface (IImageClient) for full test coverage
- **Blocker:** Need to confirm SDK design before implementation

**Decision 2: Integration Test Frequency**
- **Options:** Every commit, nightly, manual only
- **Recommendation:** Nightly + manual trigger
- **Blocker:** None — can implement immediately

**Decision 3: Coverage Target**
- **Options:** 80%, 85%, 90%
- **Recommendation:** 85% minimum (align with project standard)
- **Blocker:** None — team decision

---

## 13. References

### 13.1 Code Examples

**Flux2 HTTP Tests:** `src/ElBruno.Text2Image.Tests/Flux2GeneratorHttpTests.cs`  
**MAI-Image-2 HTTP Tests:** `src/ElBruno.Text2Image.Tests/MaiImage2GeneratorHttpTests.cs`  
**Secret Resolver Tests:** `src/ElBruno.Text2Image.Tests/Cli/Secrets/SecretResolverTests.cs`  
**Config Store Tests:** `src/ElBruno.Text2Image.Tests/Cli/ConfigStoreTests.cs`

### 13.2 Documentation

**Azure.AI.Inference SDK:** https://learn.microsoft.com/en-us/dotnet/api/azure.ai.inference  
**GPT-Image-1.5 Model:** https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models  
**xUnit Docs:** https://xunit.net/  
**Moq Docs:** https://github.com/moq/moq (if needed for mocking)

### 13.3 Team Decisions

**CLI uses Spectre.Console.Cli:** `.squad/decisions.md` (2026-04-19)  
**Secret Store Interface Design:** `.squad/decisions.md` (2026-04-19)  
**Provider Adapter Default Parameters:** `.squad/decisions.md` (2026-04-19)  
**Configurable Model Names:** Jayne history (2026-04-20)

---

## Appendix A: Sample Test Code

### A.1 Basic Validation Test

```csharp
namespace ElBruno.Text2Image.Tests;

public class GptImage1p5GeneratorValidationTests
{
    [Fact]
    public void Constructor_NullEndpoint_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new GptImage1p5Generator(null!, "deployment", "api-key"));
    }

    [Fact]
    public void Constructor_EmptyEndpoint_Throws()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new GptImage1p5Generator("", "deployment", "api-key"));
    }

    [Fact]
    public void Constructor_HttpEndpoint_Throws()
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() =>
            new GptImage1p5Generator("http://example.com", "deployment", "api-key"));
        Assert.Contains("HTTPS", ex.Message);
    }
}
```

### A.2 Response Parsing Test

```csharp
public class GptImage1p5GeneratorResponseTests
{
    [Fact]
    public async Task GenerateAsync_SuccessResponse_ReturnsResult()
    {
        var fakeClient = new FakeImageClient
        {
            ResponseFactory = (prompt, opts) =>
            {
                var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
                return new ImageGenerationResult(
                    ImageBytes: pngBytes,
                    Prompt: prompt,
                    ModelName: "gpt-image-1.5",
                    Width: 1024,
                    Height: 1024);
            }
        };

        using var generator = new GptImage1p5Generator(fakeClient);

        var result = await generator.GenerateAsync("a red apple");

        Assert.NotNull(result);
        Assert.NotEmpty(result.ImageBytes);
        Assert.Equal("a red apple", result.Prompt);
        Assert.Equal("gpt-image-1.5", result.ModelName);
    }
}
```

### A.3 Integration Test

```csharp
[Collection("Integration")]
[Trait("Category", "Integration")]
public class GptImage1p5GeneratorIntegrationTests : IDisposable
{
    private readonly string _outputDir;

    public GptImage1p5GeneratorIntegrationTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), $"t2i-integration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputDir);
    }

    [Fact]
    public async Task GenerateAsync_SimplePrompt_ProducesImage()
    {
        // Skip if secrets unavailable
        var endpoint = Environment.GetEnvironmentVariable("AZURE_AI_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("AZURE_AI_DEPLOYMENT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_AI_API_KEY");

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            return; // Skip test
        }

        using var generator = new GptImage1p5Generator(endpoint, deployment, apiKey);

        var result = await generator.GenerateAsync("a red apple");
        var outputPath = Path.Combine(_outputDir, "output.png");
        await result.SaveAsync(outputPath);

        Assert.True(File.Exists(outputPath));
        var bytes = await File.ReadAllBytesAsync(outputPath);
        Assert.True(bytes.Length > 1024); // At least 1KB
        Assert.Equal(0x89, bytes[0]); // PNG magic byte
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
        {
            try { Directory.Delete(_outputDir, recursive: true); } catch { }
        }
    }
}
```

---

## Summary

This test strategy provides a **comprehensive, battle-tested approach** to validating the GPT-Image-1.5 generator implementation. Key highlights:

1. **80+ planned tests** across unit, integration, and CLI layers
2. **85% coverage minimum** with clear success criteria
3. **Mock/fake strategy** using wrapper interface for testability
4. **Integration test cost control** via nightly runs and quota limits
5. **CI/CD integration** with GitHub Actions and secret management
6. **Clear test organization** following Flux2/MAI-Image-2 patterns

**Next Steps:**
1. Spike Azure.AI.Inference SDK mockability (Kaylee)
2. Verify prompt/size limits with Azure docs (River)
3. Approve integration test budget (Bruno)
4. Implement Phase 1 tests (Jayne)

If it's not tested, it doesn't work. Let's make sure GPT-Image-1.5 works.

— Jayne, Tester
