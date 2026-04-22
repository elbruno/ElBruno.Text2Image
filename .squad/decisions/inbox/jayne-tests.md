# Test Coverage Assessment — ElBruno.Text2Image
**Reviewer:** Jayne (Tester)  
**Date:** 2025-01-28  
**Project:** ElBruno.Text2Image  

---

## Executive Summary

**Overall Coverage:** ~40% (estimated based on file coverage analysis)  
**Total Test Methods:** 361 [Fact] and [Theory] tests  
**Files with Tests:** 17 test files  
**Production Code Files:** 72 source files (35 core library + 37 CLI)  

**Key Findings:**
- ✅ **GOOD:** HTTP layer for cloud providers (Flux2, GptImage2, GptImage1p5, MaiImage2) well-tested
- ✅ **GOOD:** CLI secrets infrastructure (DPAPI, EnvVar, PlainFile stores) comprehensively tested
- ✅ **GOOD:** Configuration system (ConfigStore, ConfigModel) well-covered
- ✅ **GOOD:** Constructor validation and HTTP error scenarios (429, 500, 503, timeouts) tested
- ❌ **CRITICAL GAP:** Zero tests for local Stable Diffusion pipeline components
- ❌ **CRITICAL GAP:** No tests for CLI commands (GenerateCommand, DoctorCommand, etc.)
- ❌ **CRITICAL GAP:** Provider adapter layer (4 adapters) completely untested
- ❌ **CRITICAL GAP:** ModelManager download/caching logic untested
- ⚠️ **MAJOR GAP:** Integration tests marked as "SkippableFact" — not run in CI
- ⚠️ **MAJOR GAP:** No end-to-end generation flow tests
- ⚠️ **MAJOR GAP:** No retry/resilience tests for network failures

---

## Test Coverage Assessment

### Current State

**Coverage by Layer:**

| Layer | Files | Tested | Coverage | Notes |
|-------|-------|--------|----------|-------|
| Foundry Cloud Generators | 4 | 4 | 100% | HTTP/validation well-tested |
| CLI Secrets Infrastructure | 5 | 5 | 100% | Comprehensive coverage |
| CLI Configuration | 3 | 3 | 100% | Config store/model/display tested |
| CLI Commands | 7 | 1 (partial) | 14% | Only InitCommand partially tested |
| CLI Provider Adapters | 4 | 0 | 0% | Zero coverage |
| Local SD Pipeline | 7 | 0 | 0% | Critical gap |
| Core Library (ImageGenOptions, etc.) | 8 | 1 | 13% | Only options class tested |
| Schedulers | 3 | 0 | 0% | No coverage |
| Model Management | 1 | 0 | 0% | Download/cache untested |

**Tested Areas:**
1. **Cloud Provider HTTP Layer** (Flux2, GptImage2, GptImage1p5, MaiImage2)
   - Constructor validation (null/empty/https checks)
   - Request formatting (Content-Length, JSON, headers)
   - HTTP error codes (429, 500, 503, timeout)
   - Response parsing (base64, URL-based image data)
   - Prompt length validation

2. **CLI Secrets Infrastructure**
   - `DpapiSecretStore` (Windows credential store)
   - `EnvVarSecretStore` (environment variables)
   - `PlainFileSecretStore` (JSON file storage)
   - `SecretResolver` (5-layer resolution chain)

3. **CLI Configuration**
   - `ConfigStore` (load/save/merge operations)
   - `ConfigModel` (deserialization, default values)
   - `ConfigDisplay` (help text rendering)

4. **ImageGenerationOptions**
   - Width/height validation (range, multiple-of-8)
   - NumInferenceSteps bounds checking
   - Default value initialization

**Untested Areas:**
1. **Local Stable Diffusion Pipeline** (0 tests)
   - `StableDiffusionPipeline.Generate()`
   - `TextEncoder.Encode()` / `EncodeWithGuidance()`
   - `UNetDenoiser` inference
   - `VaeDecoder` latent-to-image decoding
   - `ClipTokenizer.Tokenize()`
   - `TensorHelper` utility methods

2. **CLI Commands** (1 partial/7 total)
   - `GenerateCommand.ExecuteAsync()` — **ZERO TESTS**
   - `DoctorCommand.ExecuteAsync()` — **ZERO TESTS**
   - `ProvidersCommand.ExecuteAsync()` — **ZERO TESTS**
   - `SecretsCommand.ExecuteAsync()` — **ZERO TESTS**
   - `ConfigCommand.ExecuteAsync()` — **ZERO TESTS**
   - `VersionCommand.Execute()` — **ZERO TESTS**
   - `InitCommand.Execute()` — **PARTIAL** (only basic constructor test)

3. **Provider Adapters** (0/4 tested)
   - `FoundryFlux2Adapter` (CheckAsync, GenerateAsync)
   - `FoundryGptImage2Adapter`
   - `FoundryGptImage1p5Adapter`
   - `FoundryMaiImage2Adapter`
   - `ProviderRegistry.Get()` / `All`

4. **Model Management**
   - `ModelManager.EnsureModelAvailableAsync()` (HuggingFace downloads)
   - `ModelManager.IsModelAvailable()` (file existence checks)
   - Download progress reporting
   - Cancellation handling

5. **Schedulers** (0/3 tested)
   - `EulerAncestralDiscreteScheduler`
   - `LCMScheduler`
   - `LMSDiscreteScheduler`

6. **Integration Tests**
   - `GptImage1p5GeneratorIntegrationTests.cs` uses `[SkippableFact]` — likely skipped in CI
   - No true end-to-end tests that exercise full generation flow

---

## Critical Gaps (Must Test)

### 1. **CLI GenerateCommand — E2E Happy Path**
**Files:** `src/ElBruno.Text2Image.Cli/Commands/GenerateCommand.cs`  
**Why Critical:** This is the main user-facing command. A broken command = broken product.  
**Scenario:**
- User runs: `t2i "a cat" --provider foundry-flux2`
- Command resolves provider, secrets, calls adapter, saves output
- Test should mock provider adapter, verify file written

**Effort:** 2-3 hours (need to mock DI container, file I/O)

---

### 2. **Provider Adapter Layer — GenerateAsync Flow**
**Files:**
- `src/ElBruno.Text2Image.Cli/Providers/FoundryFlux2Adapter.cs`
- `src/ElBruno.Text2Image.Cli/Providers/FoundryGptImage2Adapter.cs`
- `src/ElBruno.Text2Image.Cli/Providers/FoundryMaiImage2Adapter.cs`
- `src/ElBruno.Text2Image.Cli/Providers/FoundryGptImage1p5Adapter.cs`

**Why Critical:** Adapters are the glue between CLI and Foundry generators. Bugs here affect all users.  
**Scenarios:**
- Adapter reads config/secrets correctly
- Adapter creates generator with correct endpoint/apiKey
- Adapter handles missing config (throws clear exception)
- Adapter propagates progress events
- Adapter handles cancellation

**Effort:** 3-4 hours (4 adapters × 4 tests each)

---

### 3. **ModelManager Download Logic — Network Resilience**
**Files:** `src/ElBruno.Text2Image/ModelManager.cs`  
**Why Critical:** Users on slow networks or with large models (1GB+) will experience download failures. No retries = bad UX.  
**Scenarios:**
- Model already cached → skip download
- Model missing → trigger HuggingFace download
- Download interrupted → throw with clear error
- Progress reporting works
- Cancellation during download

**Effort:** 2 hours (mock HuggingFaceDownloader)

---

### 4. **Local Pipeline — Critical Path Coverage**
**Files:**
- `src/ElBruno.Text2Image/Pipeline/StableDiffusionPipeline.cs`
- `src/ElBruno.Text2Image/Pipeline/TextEncoder.cs`
- `src/ElBruno.Text2Image/Pipeline/UNetDenoiser.cs`
- `src/ElBruno.Text2Image/Pipeline/VaeDecoder.cs`

**Why Critical:** Local generation (CPU/GPU) is a core feature. Zero tests = zero confidence.  
**Scenarios:**
- Pipeline loads ONNX models successfully
- Pipeline.Generate() produces image bytes
- Tokenization handles max length (77 tokens)
- CFG guidance works (conditional + unconditional embeddings)
- Scheduler timesteps generated correctly

**Effort:** 8-10 hours (need ONNX test models, may need fixtures)

---

### 5. **Network Error Handling — Retry Logic**
**Files:** All `Foundry/*Generator.cs` files  
**Why Critical:** Cloud APIs fail. No retries = poor user experience.  
**Scenarios:**
- Transient error (502, 503, network timeout) → should retry or give clear guidance
- Rate limit (429) → should wait/retry with backoff
- Auth failure (401, 403) → clear error message
- Timeout during polling (Flux2 async pattern) → throw TimeoutException

**Current State:** Some timeout tests exist, but **NO RETRY TESTS**

**Effort:** 4 hours (add retry wrapper or document "no auto-retry" design decision)

---

### 6. **DoctorCommand — Health Checks**
**Files:** `src/ElBruno.Text2Image.Cli/Commands/DoctorCommand.cs`  
**Why Critical:** Diagnostic tool is first line of support. If it's broken, users can't self-diagnose.  
**Scenarios:**
- Doctor detects missing config
- Doctor checks provider health (calls `CheckAsync()`)
- Doctor reports GPU availability
- Doctor lists secret store status

**Effort:** 2 hours

---

### 7. **Prompt Length Validation — All Providers**
**Files:** All `Foundry/*Generator.cs`  
**Why Critical:** Different providers have different limits. Exceeding = API error.  
**Current State:** Tests exist for each provider individually, but no consistency checks.  
**Scenarios:**
- Flux2: 1000 chars max (tested ✓)
- GptImage2: 4000 chars max (tested ✓)
- GptImage1p5: 4000 chars max (tested ✓)
- MaiImage2: 4000 chars max (tested ✓)
- **GAP:** What happens at boundary (4000 vs 4001)? Unicode handling?

**Effort:** 1 hour (add boundary tests)

---

### 8. **Configuration Merge Logic — CLI Overrides**
**Files:**
- `src/ElBruno.Text2Image.Cli/Commands/GenerateCommand.cs`
- `src/ElBruno.Text2Image.Cli/Secrets/SecretResolver.cs`

**Why Critical:** Users expect `--endpoint` CLI flag to override config. No tests = no guarantee.  
**Scenarios:**
- CLI `--endpoint` overrides config file
- CLI `--api-key` overrides secrets
- Config file provides defaults when no CLI flags
- Resolution chain: CLI → config → secrets → error

**Effort:** 2 hours

---

### 9. **FLUX.2 Async Polling — Timeout Handling**
**Files:** `src/ElBruno.Text2Image.Foundry/Flux2Generator.cs:353-411`  
**Why Critical:** Flux2 uses 202 + polling. Infinite polls = hung CLI.  
**Current State:** `MaxPollAttempts = 120` (4 minutes). What if server never responds?  
**Scenarios:**
- 202 → poll → 200 success (happy path)
- 202 → poll → 202 forever → timeout at 120 attempts
- 202 → poll → 500 error → throw
- 202 → poll → malformed JSON → throw

**Effort:** 3 hours

---

### 10. **Image File Save — Filesystem Errors**
**Files:** `src/ElBruno.Text2Image/ImageGenerationResult.cs:SaveAsync()`  
**Why Critical:** Users save to readonly drives, network paths, etc.  
**Scenarios:**
- Save to valid path → success
- Save to readonly path → throw UnauthorizedAccessException
- Save to non-existent directory → create directory (or throw?)
- Save with invalid filename chars → throw ArgumentException

**Current State:** Only happy path tested.

**Effort:** 1 hour

---

## High-Priority Gaps (Should Test)

### 11. **Scheduler Correctness — Timestep Generation**
**Files:**
- `src/ElBruno.Text2Image/Schedulers/EulerAncestralDiscreteScheduler.cs`
- `src/ElBruno.Text2Image/Schedulers/LCMScheduler.cs`

**Scenarios:**
- Scheduler produces correct timestep array for `numSteps=20`
- Scheduler `InitNoiseSigma` is non-zero
- LCM scheduler differs from Euler (LCM skips CFG)

**Effort:** 2 hours

---

### 12. **ConsoleHelpers — TTY Detection**
**Files:** `src/ElBruno.Text2Image.Cli/Tui/ConsoleHelpers.cs`  
**Why Important:** Interactive prompts should only run in TTY. Non-TTY (CI, scripts) should error clearly.  
**Current State:** Only basic tests exist.  
**Scenarios:**
- `IsInteractive()` returns false in non-TTY (CI/CD)
- `PrintError()` writes to stderr (not stdout)
- Color output respects NO_COLOR env var

**Effort:** 1 hour

---

### 13. **SetupWizard — Interactive Provider Selection**
**Files:** `src/ElBruno.Text2Image.Cli/Tui/SetupWizard.cs`  
**Why Important:** First-run experience. If broken, users abandon tool.  
**Scenarios:**
- Wizard prompts for provider selection
- Wizard saves config after setup
- Wizard handles Ctrl+C gracefully
- Wizard skips if non-interactive

**Effort:** 3 hours (complex UI logic)

---

### 14. **ProviderRegistry — Case-Insensitive Lookup**
**Files:** `src/ElBruno.Text2Image.Cli/Providers/ProviderRegistry.cs:23`  
**Why Important:** User types `FOUNDRY-FLUX2` (uppercase) → should match `foundry-flux2`.  
**Current State:** Code uses `StringComparer.OrdinalIgnoreCase` but **NO TESTS**.  
**Scenarios:**
- `Get("FOUNDRY-FLUX2")` → returns adapter
- `Get("foundry-flux2")` → returns adapter
- `Get("unknown-provider")` → returns null

**Effort:** 30 minutes

---

### 15. **Image Size Mapping — GPT-Image-2 Fixed Sizes**
**Files:** `src/ElBruno.Text2Image.Foundry/GptImage2Generator.cs:85-94`  
**Why Important:** GPT-Image-2 only supports 1024×1024, 1024×1536, 1536×1024. Other sizes map to nearest.  
**Scenarios:**
- 1024×1024 → "1024x1024"
- 1920×1080 (16:9) → "1536x1024"
- 768×1024 (portrait) → "1024x1536"
- Edge case: 1024×1025 → maps to?

**Effort:** 1 hour

---

### 16. **MaiImage2Generator — URL-based Image Download**
**Files:** `src/ElBruno.Text2Image.Foundry/MaiImage2Generator.cs:223-230`  
**Why Important:** API may return URL instead of base64. Download path untested.  
**Scenarios:**
- Response has `b64_json` → decode base64 (tested ✓)
- Response has `url` → fetch image via HTTP
- URL fetch fails (404, timeout) → throw
- **SECURITY:** API key not leaked in URL fetch (tested via comment, but no test)

**Effort:** 2 hours

---

### 17. **ExecutionProvider Auto-Detection**
**Files:** `src/ElBruno.Text2Image/ExecutionProvider.cs`  
**Why Important:** Users expect GPU auto-detection. Failures = silent fallback to CPU (slow).  
**Scenarios:**
- `Auto` detects CUDA if available
- `Auto` falls back to CPU if no GPU
- Invalid provider throws exception

**Effort:** 2 hours (may need mocked ONNX runtime)

---

### 18. **Progress Reporting — All Providers**
**Files:** Provider adapters, `ProgressRenderer.cs`  
**Why Important:** Long operations (10s+) need progress feedback.  
**Scenarios:**
- Flux2 async polling reports progress updates
- Model download reports download progress
- CLI renders progress bar (hard to test, may need screenshot comparison)

**Effort:** 3 hours

---

### 19. **Cancellation Token Propagation**
**Files:** All `*Async()` methods  
**Why Important:** Users expect Ctrl+C to cancel. Ignored CancellationToken = hung process.  
**Scenarios:**
- GenerateAsync() cancellation during HTTP request
- ModelManager download cancellation
- Pipeline generation cancellation (if long-running)

**Effort:** 2 hours

---

### 20. **Error Message Quality — User-Facing**
**Files:** All exception throws  
**Why Important:** Cryptic errors = support burden.  
**Scenarios:**
- Missing config → "Run t2i config to set up provider"
- Missing API key → "Run t2i secrets to configure API key"
- Network timeout → "Request timed out. Check your network connection."
- Invalid endpoint → "Endpoint must use HTTPS"

**Current State:** Some good error messages exist, but not verified by tests.

**Effort:** 1 hour (add assertion checks for exception messages)

---

## Medium-Priority Gaps (Consider Testing)

### 21. **Seed Reproducibility**
**Scenario:** Same prompt + same seed → same image (for deterministic models)  
**Effort:** 2 hours

### 22. **Negative Prompts (if supported)**
**Scenario:** "a cat, NOT a dog" → verify request contains negative prompt  
**Effort:** 1 hour

### 23. **Batch Generation (Samples)**
**Scenario:** `scenario-09-batch-generation` sample works  
**Effort:** 2 hours

### 24. **GPU Memory Management**
**Scenario:** Large image (1920×1080) doesn't OOM on 4GB GPU  
**Effort:** 4 hours (needs real GPU)

### 25. **Unicode Prompt Handling**
**Scenario:** "一只猫" (Chinese) → API accepts UTF-8  
**Effort:** 1 hour

### 26. **File Path Edge Cases**
**Scenario:** Save to path with spaces, unicode chars, long path (>260 on Windows)  
**Effort:** 1 hour

### 27. **Multiple Providers in Config**
**Scenario:** Config has both `foundry-flux2` and `foundry-mai2` → doctor shows both  
**Effort:** 1 hour

### 28. **Secret Store Priority**
**Scenario:** DPAPI + file both have secret → DPAPI wins  
**Effort:** 1 hour (SecretResolver tests exist, but priority not explicit)

### 29. **Config File Corruption**
**Scenario:** Malformed JSON config → clear error, not crash  
**Effort:** 1 hour

### 30. **HTTP Redirect Handling**
**Scenario:** Endpoint redirects (301, 302) → follow or fail?  
**Effort:** 1 hour

---

## Edge Cases Not Covered

### Input Validation Edge Cases (10+ scenarios):
1. **Empty prompt** → Should throw or use default?
2. **Prompt = single space** → Trimmed or error?
3. **Prompt = 10,000 chars** → Truncate or error?
4. **Width = 0** → Throws (tested ✓)
5. **Width = -512** → Throws (need test)
6. **Width = Int32.MaxValue** → Throws (need test)
7. **Seed = Int32.MinValue** → Valid or error?
8. **NumInferenceSteps = 1** → Valid (tested ✓), but does it work?
9. **GuidanceScale = 0** → What happens? (LCM mode?)
10. **GuidanceScale = 100** → Extreme CFG, should work but slow
11. **Null options** → Uses defaults (tested ✓)
12. **Null HttpClient** → Creates new (tested ✓)

### Network Edge Cases (10+ scenarios):
13. **Slow connection (1 KB/s)** → Times out correctly?
14. **Connection drops mid-download** → Throws or retries?
15. **DNS resolution failure** → Clear error message?
16. **SSL certificate error** → Throws or ignores?
17. **Chunked transfer encoding** → FLUX.2 rejects (tested ✓)
18. **Gzip response encoding** → HttpClient handles, but verify
19. **Response = 5MB (very large)** → Buffer overflow risk?
20. **Concurrent requests** → Thread-safe?
21. **API returns 202 but never 200** → Timeout (tested ✓)
22. **API returns 200 but empty body** → Throws InvalidOperationException
23. **API returns 200 but malformed JSON** → JsonException

### Filesystem Edge Cases (5+ scenarios):
24. **Save to network UNC path** → Works or throws?
25. **Save to path with trailing slash** → Error or corrects?
26. **Save when disk full** → IOException
27. **Save with existing file** → Overwrite or throw?
28. **Concurrent saves to same path** → File lock contention

### Configuration Edge Cases (5+ scenarios):
29. **Config file doesn't exist** → Creates default
30. **Config file is read-only** → Can't save, throws
31. **Config has unknown provider** → Ignored or warning?
32. **Config has duplicate keys** → Last wins or error?
33. **Config has future version schema** → Backward compat?

### Security Edge Cases (3+ scenarios):
34. **API key logged in plaintext** → Should redact
35. **Error message contains API key** → Should redact
36. **SSRF via malicious API response URL** → MAI Image2 mitigation exists (tested via comment)

---

## Error Handling Gaps

### Exception Types Tested:
- ✅ `ArgumentException` (null/empty endpoint, apiKey)
- ✅ `ArgumentOutOfRangeException` (prompt too long, width/height bounds)
- ✅ `HttpRequestException` (HTTP 429, 500, 503)
- ✅ `TimeoutException` (network timeout)
- ✅ `InvalidOperationException` (missing response data)

### Exception Types NOT Tested:
- ❌ `OperationCanceledException` (cancellation)
- ❌ `JsonException` (malformed API response)
- ❌ `FileNotFoundException` (model files missing)
- ❌ `UnauthorizedAccessException` (file save to readonly path)
- ❌ `OutOfMemoryException` (large images on constrained GPU)
- ❌ `AggregateException` (async failures)

### Error Messages NOT Verified:
- ❌ Exception messages not asserted (only exception type checked)
- ❌ No tests for "helpful hint" messages (e.g., Flux2Generator.cs:201)
- ❌ No tests for error redaction (API keys in logs)

### Retry Logic NOT Tested:
- ❌ No retry tests for any provider
- ❌ No backoff tests for rate limiting (429)
- ❌ No circuit breaker pattern

---

## Integration Tests

### Existing Integration Tests:
- `GptImage1p5GeneratorIntegrationTests.cs` — **SKIPPED** (uses `[SkippableFact]`)
  - Requires real Azure OpenAI endpoint + API key
  - Only runs if environment variables set
  - Not run in CI/CD

### Missing Integration Tests:
1. **No end-to-end CLI tests** — Generate command with real provider
2. **No local pipeline integration test** — Load real ONNX model, generate image
3. **No multi-provider test** — Switch between Flux2, GptImage2, MaiImage2
4. **No config migration test** — Old config format → new format
5. **No cross-platform test** — Windows vs Linux vs macOS (DPAPI availability)

### Mock vs Real API:
- **Current:** All tests use `FakeHttpHandler` or mocked clients (good for unit tests)
- **Missing:** Optional integration tests against real Azure APIs (for release validation)

### End-to-End Flow:
**CRITICAL MISSING TEST:**
```
User runs: t2i "a cat" --provider foundry-flux2 --out cat.png
Expected: cat.png created with valid PNG bytes

Current Coverage: ZERO END-TO-END TESTS
```

**Recommendation:** Add E2E test that mocks HTTP but exercises full CLI→Adapter→Generator→FileSave flow.

---

## Test Quality Assessment

### ✅ **Strengths:**

1. **Test Naming:** Excellent pattern: `MethodName_Condition_Expectation`
   - Example: `Constructor_NullEndpoint_Throws`
   - Example: `GenerateAsync_500InternalServerError_ThrowsException`

2. **Test Isolation:** No shared state, each test is independent

3. **Mocking Strategy:** Clean fake implementations
   - `FakeHttpHandler` (intercepts HTTP)
   - `FakeGptImage2Client` (mocks Azure SDK)
   - `FakeSecretStore` (test helper)

4. **Constructor Validation:** Thorough tests for all null/empty/whitespace cases

5. **HTTP Error Coverage:** Tests for 429, 500, 503, timeout, network errors

6. **Arrange-Act-Assert:** Clear test structure

### ⚠️ **Weaknesses:**

1. **Integration Tests Skipped:** `[SkippableFact]` means tests don't run in CI

2. **No Performance Tests:** No tests for slow operations (10s+ generation)

3. **No Concurrency Tests:** No tests for thread safety, race conditions

4. **No Flaky Test Detection:** No retry logic for transient failures

5. **No Code Coverage Metrics:** No coverage reporting in CI/CD

6. **Test Data:** Hardcoded test data (magic bytes), no test fixtures

7. **Assertion Messages:** Many assertions lack failure messages
   ```csharp
   Assert.Equal(512, options.Width);  // ❌ No message
   Assert.Equal(512, options.Width, "Width should default to 512");  // ✅ Better
   ```

8. **Exception Message Verification:** Tests check exception type but not message content

9. **Test File Size:** Some test files are large (33KB+) — consider splitting

10. **Smoke Tests Skipped:** `CommandSurfaceSmokeTest` has 2 tests, both skipped

---

## Test Implementation Recommendations

### Priority 1: Critical Gaps (Next Sprint)

**File:** `src/ElBruno.Text2Image.Tests/Cli/Commands/GenerateCommandTests.cs` (NEW)
```csharp
public class GenerateCommandTests
{
    [Fact]
    public async Task Execute_ValidPrompt_CreatesImageFile()
    {
        // Mock provider adapter, verify file written
    }

    [Fact]
    public async Task Execute_MissingProvider_ShowsError()
    {
        // No default provider → error message
    }

    [Fact]
    public async Task Execute_CliOverride_OverridesConfig()
    {
        // --endpoint flag overrides config file
    }
}
```

**File:** `src/ElBruno.Text2Image.Tests/Cli/Providers/FoundryFlux2AdapterTests.cs` (NEW)
```csharp
public class FoundryFlux2AdapterTests
{
    [Fact]
    public async Task GenerateAsync_ValidRequest_CallsGenerator()
    {
        // Mock IHttpClientFactory, ConfigStore, SecretResolver
    }

    [Fact]
    public async Task CheckAsync_MissingApiKey_ReturnsUnhealthy()
    {
        // Health check detects missing secrets
    }
}
```

**File:** `src/ElBruno.Text2Image.Tests/ModelManagerTests.cs` (NEW)
```csharp
public class ModelManagerTests
{
    [Fact]
    public async Task EnsureModelAvailable_ModelExists_SkipsDownload()
    {
        // Model cached → no download
    }

    [Fact]
    public async Task EnsureModelAvailable_ModelMissing_DownloadsFromHuggingFace()
    {
        // Mock HuggingFaceDownloader
    }

    [Fact]
    public async Task EnsureModelAvailable_DownloadCancelled_ThrowsOperationCanceledException()
    {
        // Cancellation during download
    }
}
```

### Priority 2: High-Value Unit Tests

**File:** `src/ElBruno.Text2Image.Tests/Schedulers/SchedulerTests.cs` (NEW)
```csharp
public class EulerSchedulerTests
{
    [Fact]
    public void SetTimesteps_20Steps_ReturnsCorrectArray()
    {
        var scheduler = new EulerAncestralDiscreteScheduler();
        var timesteps = scheduler.SetTimesteps(20);
        Assert.Equal(20, timesteps.Length);
        Assert.True(timesteps[0] > timesteps[19], "Timesteps should be descending");
    }
}
```

### Priority 3: Integration Tests (Opt-In)

**File:** `src/ElBruno.Text2Image.Tests/E2E/CliE2ETests.cs` (NEW)
```csharp
[Trait("Category", "E2E")]
public class CliE2ETests
{
    [Fact(Skip = "Requires real API credentials")]
    public async Task Cli_GenerateCommand_EndToEnd()
    {
        // Full flow: CLI args → adapter → generator → file save
        // Uses mocked HTTP, but exercises full stack
    }
}
```

### Tooling Recommendations:

1. **Add Code Coverage:** Use Coverlet + ReportGenerator
   ```xml
   <PackageReference Include="coverlet.msbuild" Version="6.*" />
   ```

2. **Add Mutation Testing:** Use Stryker.NET
   - Detects weak assertions
   - Finds untested code paths

3. **Add Benchmark Tests:** Use BenchmarkDotNet
   - Detect performance regressions
   - Compare schedulers, models

4. **Add Approval Tests:** For complex output (config files, help text)

5. **CI/CD Integration:**
   - Run tests on PR
   - Fail PR if coverage drops
   - Publish coverage reports

---

## Risk Assessment

### High Risk (No Tests + High Impact):

| Scenario | Impact | Likelihood | Risk Score |
|----------|--------|------------|------------|
| GenerateCommand fails | Users can't generate images | Medium | **HIGH** |
| ModelManager download hangs | First-run experience broken | High | **HIGH** |
| Provider adapter config bug | Wrong endpoint, all calls fail | Medium | **HIGH** |
| Pipeline ONNX load failure | Local generation broken | Low | **MEDIUM** |
| Retry logic missing | Poor UX on transient errors | High | **HIGH** |

### Medium Risk:

| Scenario | Impact | Likelihood | Risk Score |
|----------|--------|------------|------------|
| Doctor command broken | Users can't diagnose issues | Low | **MEDIUM** |
| Scheduler produces wrong timesteps | Poor image quality | Low | **MEDIUM** |
| Secret store corruption | Users lose API keys | Low | **MEDIUM** |

### Low Risk:

| Scenario | Impact | Likelihood | Risk Score |
|----------|--------|------------|------------|
| Config display formatting off | Cosmetic | Low | **LOW** |
| Help text typos | Minor UX issue | Low | **LOW** |

---

## Estimated Effort Summary

| Priority | Tasks | Estimated Hours |
|----------|-------|-----------------|
| Critical (Must Test) | 10 gaps | **30-35 hours** |
| High-Priority (Should Test) | 10 gaps | **20-25 hours** |
| Medium-Priority (Consider) | 10 gaps | **10-15 hours** |
| **TOTAL** | **30 test scenarios** | **60-75 hours** |

**Recommendation:** Focus on Critical gaps first (2 weeks of dedicated testing effort).

---

## Action Items

### Immediate (This Sprint):
1. ✅ Add `GenerateCommandTests.cs` — E2E happy path
2. ✅ Add `FoundryFlux2AdapterTests.cs` — Provider adapter layer
3. ✅ Add `ModelManagerTests.cs` — Download/caching
4. ✅ Add retry tests for HTTP 429, 503 errors
5. ✅ Enable code coverage reporting in CI

### Short-Term (Next Sprint):
6. ✅ Add scheduler tests
7. ✅ Add DoctorCommand tests
8. ✅ Add pipeline component tests (if feasible without ONNX models)
9. ✅ Add cancellation token tests
10. ✅ Add error message verification tests

### Long-Term (Backlog):
11. ✅ Add mutation testing (Stryker.NET)
12. ✅ Add performance benchmarks (BenchmarkDotNet)
13. ✅ Add E2E integration test suite (opt-in, requires API keys)
14. ✅ Add cross-platform tests (Windows/Linux/macOS)
15. ✅ Add GPU memory stress tests

---

## Conclusion

**Overall Assessment:** The codebase has **strong unit test coverage for HTTP/API layers** but **critical gaps in CLI command execution, provider adapters, and local pipeline components**. The test quality is high (good naming, isolation, mocking), but integration tests are skipped and E2E coverage is zero.

**Top Recommendation:** Prioritize testing the user-facing CLI commands (GenerateCommand, DoctorCommand) and the provider adapter layer, as these are the most critical untested areas with high user impact.

**Test Coverage Goal:** Aim for **70%+ code coverage** (currently ~40%) by adding the Critical and High-Priority gaps identified above.

---

**Next Steps:**
1. Review this document with team
2. Prioritize Critical gaps (10 scenarios, ~30 hours)
3. Create test tasks in backlog
4. Set coverage target: 70% by end of quarter
5. Enable coverage reporting in CI/CD

**Questions?** Contact Jayne (Tester) for test implementation guidance.
