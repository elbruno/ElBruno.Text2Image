---
date: 2026-04-22
author: River
issue: #19
---

# Decision: Configurable Timeout for Image Generation Providers

## Context

Azure GPT-Image-2 API can take 3-4 minutes to generate images, but the default HttpClient timeout (100 seconds) was causing generations to fail. Users needed a way to configure longer timeouts for slow providers.

## Decision

Added configurable timeout support across the entire stack:

1. **CLI Level:** `--timeout` option (default: 300 seconds)
2. **Generator Level:** Optional `timeoutSeconds` constructor parameter on all cloud generators
3. **Adapter Level:** Parse timeout from request ExtraOptions and pass to generators
4. **HttpClient Level:** Set `HttpClient.Timeout` on the injected instance

## Rationale

**Why 300 seconds default?**
- GPT-Image-2 takes 3-4 minutes (180-240 seconds)
- 300 seconds (5 minutes) provides safety margin
- Much better than HttpClient's 100-second default for cloud image generation

**Why constructor parameter instead of GenerateAsync parameter?**
- HttpClient.Timeout is a client-level setting, not per-request
- IHttpClientFactory returns fresh clients per call, so constructor-time configuration is equivalent to per-request
- Keeps timeout configuration alongside other client configuration (endpoint, apiKey)

**Why optional parameter with null meaning "don't change"?**
- Preserves backward compatibility
- Allows pre-configured HttpClient instances to keep their settings
- Explicit opt-in for timeout modification

## Impact

- **Users:** Can now successfully generate images with slow providers by using `--timeout 300` or higher
- **Developers:** All generators have consistent timeout configuration API
- **Testing:** 31 new tests verify timeout configuration correctness across all generators

## Alternatives Considered

1. **Per-request timeout in GenerateAsync:** Rejected because HttpClient.Timeout is client-level, not per-request
2. **Global timeout configuration:** Rejected because different providers have different performance characteristics
3. **Always set 300s timeout:** Rejected to preserve backward compatibility and allow pre-configured clients

## Files Modified

- CLI: `GenerateCommand.cs`
- Adapters: `FoundryFlux2Adapter.cs`, `FoundryMaiImage2Adapter.cs`, `FoundryGptImage1p5Adapter.cs`, `FoundryGptImage2Adapter.cs`
- Generators: `Flux2Generator.cs`, `MaiImage2Generator.cs`, `GptImage1p5Generator.cs`, `GptImage2Generator.cs`
- Docs: `README.md`, `docs/cli-tool.md`
- Tests: `TimeoutConfigurationTests.cs` (new)
