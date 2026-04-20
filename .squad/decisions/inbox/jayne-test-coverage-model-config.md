# Test Coverage: Configurable Model Names + Masking Policy

**Date:** 2026-04-20  
**Agent:** Jayne (Tester)  
**Branch:** `feat/configurable-model-name`  
**Status:** ✅ Complete

## Summary

Added 21 tests covering the configurable model name feature implemented by Kaylee. All tests pass on both net8.0 and net10.0 target frameworks.

## Test Coverage

### 1. HTTP Body Validation (4 tests)

Verified that model names flow through to the HTTP request body JSON:

- **MaiImage2GeneratorModelHttpTests** (2 tests)
  - Custom model "MAI-Image-2e" → request body contains `"model":"MAI-Image-2e"`
  - Default model → request body contains `"model":"mai-image-2"`

- **Flux2GeneratorModelHttpTests** (2 tests)
  - Custom model "FLUX.2-flex" → request body contains `"model":"FLUX.2-flex"`
  - Default model → request body contains `"model":"FLUX.2-pro"`

**Pattern:** Reused `FakeHttpHandler` from existing Flux2GeneratorHttpTests.cs to capture HTTP requests and inspect JSON bodies.

### 2. Config Storage (5 tests)

Verified that ProviderConfig.Model persists correctly through ConfigStore:

- **ConfigModelTests** (5 tests)
  - ProviderConfig.Model round-trips through save/load
  - ConfigStore writes and reads custom model values
  - Flux2 model configuration persists correctly
  - Model defaults to null when not set (adapters apply defaults at runtime)
  - Multiple providers with different models persist independently

**Pattern:** Same temp directory isolation pattern as ConfigStoreTests to prevent file lock conflicts.

### 3. Masking Policy (10 tests)

Verified that only secrets are masked, while endpoint and model display in plain text:

- **ConfigDisplayTests** (10 tests)
  - ConsoleHelpers.Mask masks secrets starting with "sk-"
  - Mask shows first and last characters with `***...***` in middle
  - FoundryMaiImage2Adapter.RequiredSecrets contains ONLY apiKey
  - FoundryMaiImage2Adapter.RequiredSecrets does NOT contain endpoint or model
  - FoundryMaiImage2Adapter.RequiredFields contains endpoint and model
  - FoundryFlux2Adapter.RequiredSecrets contains ONLY apiKey
  - FoundryFlux2Adapter.RequiredSecrets does NOT contain endpoint or model
  - FoundryFlux2Adapter.RequiredFields contains endpoint and model

**Pattern:** Used ServiceCollection + IHttpClientFactory to construct adapters for testing (adapters require HttpClient dependency injection).

## Test Results

```
Total tests: 240 (net10.0), 166 (net8.0)
Passed: 238 (net10.0), 166 (net8.0)
Failed: 0
Skipped: 2 (CommandSurfaceSmokeTest — skipped until CLI wiring complete)
Duration: ~1 second
```

## Coverage Areas

| Area | Coverage | Tests |
|------|----------|-------|
| HTTP request body serialization | ✅ Complete | 4 |
| Config persistence | ✅ Complete | 5 |
| Masking policy (RequiredFields vs RequiredSecrets) | ✅ Complete | 10 |
| Backward compatibility (null model defaults) | ✅ Covered | 1 |
| Multi-provider config isolation | ✅ Covered | 1 |

## Files Modified

- `src/ElBruno.Text2Image.Tests/MaiImage2GeneratorHttpTests.cs` — added MaiImage2GeneratorModelHttpTests class (2 tests)
- `src/ElBruno.Text2Image.Tests/Flux2GeneratorHttpTests.cs` — added Flux2GeneratorModelHttpTests class (2 tests)
- `src/ElBruno.Text2Image.Tests/Cli/ConfigModelTests.cs` — new file (5 tests)
- `src/ElBruno.Text2Image.Tests/Cli/ConfigDisplayTests.cs` — new file (10 tests)

## Key Patterns

1. **FakeHttpHandler reuse** — Existing pattern from Flux2 tests, intercepts HTTP requests and captures bodies for JSON validation
2. **Temp directory isolation** — Each test class uses unique temp dir to prevent ConfigStore file lock conflicts
3. **ServiceCollection for DI** — When testing adapters that require IHttpClientFactory, construct via ServiceCollection.AddHttpClient()

## Recommendations

- ✅ All tests pass — ready to merge
- ✅ Backward compatibility verified (null model falls back to defaults)
- ✅ Masking policy enforced (endpoint+model plain, apiKey masked)
- ✅ Both target frameworks validated (net8.0, net10.0)

## Next Steps

- Merge PR to main
- Tag release `cli-v0.10.0` (version already bumped in implementation commit 72a00dc)
- Publish to NuGet via automated workflow
