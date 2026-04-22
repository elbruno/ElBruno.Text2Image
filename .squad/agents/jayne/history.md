# Jayne — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Core Context

### Test Infrastructure & Patterns

- **FakeHttpHandler:** Test double that captures HTTP requests (headers/body), returns canned responses. Used for verifying serialization and model names in HTTP bodies.
- **FakeSecretStore:** In-memory test double for ISecretStore with IsAvailable toggle for platform simulation.
- **Temp directory isolation:** Each test class uses unique temp dir to prevent ConfigStore file lock conflicts.
- **ServiceCollection for DI:** When testing CLI adapters requiring IHttpClientFactory, construct via ServiceCollection.AddHttpClient() + BuildServiceProvider().
- **InternalsVisibleTo:** Test projects access internal classes via `[assembly: InternalsVisibleTo("ElBruno.Text2Image.Tests")]` in AssemblyInfo.cs.

### CLI Test Coverage Summary

- **CLI Infrastructure:** 50 tests (net10.0) — secrets, config, console helpers, commands
- **Flux2 HTTP:** 43+ tests validating Content-Length fix, img2img reference images
- **MAI-Image-2 HTTP:** 32 tests following Flux2 patterns (generation, polling, error handling)
- **InitCommand:** 6 tests verifying embedded SKILL.md resource writing
- **Configurable Models:** 21 tests (HTTP body, config persistence, masking policy)
- **Total:** 238 tests pass net10.0, 166 pass net8.0

## Learnings

### 2026-04-20 — GptImage2Generator Test Suite (91 tests)

**Coverage:**
- **Constructor validation (11 tests):** Null checks, empty string checks, whitespace checks, HTTPS validation, URI format validation, valid params acceptance
- **Property accessors (3 tests):** ModelName, DeploymentName, Endpoint
- **Prompt validation (7 tests):** Null/empty/whitespace prompts, max length (4000 chars), boundary testing, special character handling
- **Size mapping (8 tests):** All supported sizes (1024x1024, 1792x1024, 1024x1792), invalid size mapping, default size handling
- **Request/Response integration (8 tests):** Prompt passthrough, size options, image bytes extraction, metadata population, multiple requests
- **Error handling (9 tests):** HTTP errors (400, 401, 404, 429, 500, 503), network errors, timeouts, rate limiting
- **Logging (4 tests):** Generation start, success, errors, size mapping
- **Model availability (3 tests):** Cloud model immediate completion, progress reporting, cancellation
- **Dispose (2 tests):** Single and multiple dispose calls
- **HTTP Content-Length (5 tests):** ByteArrayContent usage, header verification, body size matching, JSON validity
- **HTTP Request structure (7 tests):** POST method, authorization header, prompt/size in body, deployment name in URL, API version
- **HTTP Response parsing (6 tests):** Image bytes extraction, model name, prompt preservation, dimensions, empty response handling, malformed JSON
- **HTTP Error responses (6 tests):** All HTTP status codes properly handled and thrown
- **HTTP Size mapping (4 tests):** Correct size strings sent in requests, unsupported size mapping
- **HTTP Edge cases (6 tests):** Very long prompts, Unicode characters, escaped characters, network timeouts, cancellation

**Key patterns:**
- Reused FakeHttpHandler pattern from Flux2/MAI-Image-2 tests for HTTP interception
- Created TestProgress<T> helper for synchronous progress reporting in tests (Progress<T> requires sync context)
- Testable wrapper classes allow dependency injection without modifying production code
- HTTP tests verify serialization, headers, and API contract compliance

**Test results:**
- 91 new tests created
- 91 passing (both net8.0 and net10.0)
- 0 failing
- Full coverage of happy path, error paths, edge cases, and HTTP layer

**Files created:**
- `GptImage2GeneratorTests.cs` — 60 unit tests (constructor, prompt, size, errors, logging, dispose)
- `GptImage2GeneratorHttpTests.cs` — 31 HTTP tests (requests, responses, errors, edge cases)

**Key learnings:**
- **TestProgress pattern** — Progress<T> only reports when sync context exists; TestProgress<T> calls handler directly
- **Size mapping** — GPT-Image-2 supports same sizes as GPT-Image-1.5 (1024x1024, 1792x1024, 1024x1792)
- **HTTP testing** — FakeHttpHandler captures request details (body, headers) and returns canned responses
- **Test organization** — Group tests by concern (constructor, validation, errors) for clarity and maintainability

📌 Test suite ready for GptImage2Generator implementation by Wash. Pattern established for future generator test suites.

### 2026-04-20 — Configurable Model Name Tests (21 tests)

**Coverage:**
- **HTTP serialization (4 tests):** Custom/default model names appear in request body JSON (MAI, FLUX)
- **Config persistence (5 tests):** ProviderConfig.Model round-trips, defaults work, multi-provider isolation
- **Masking policy (10 tests):** RequiredFields vs RequiredSecrets split; apiKey masked, endpoint+model plain
- **Backward compat (1 test):** Null model falls back to adapter default
- **Multi-provider (1 test):** Different models per provider persist independently

**Key patterns:**
- Reused FakeHttpHandler for HTTP body capture
- Temp directory isolation prevents file lock conflicts
- ServiceCollection + IHttpClientFactory for adapter DI testing

**Test results:**
- 240 tests total (net10.0), 166 (net8.0)
- 238 passing (net10.0), 166 (net8.0)
- 2 skipped (existing smoke tests)

**Files modified:**
- `MaiImage2GeneratorHttpTests.cs` — added MaiImage2GeneratorModelHttpTests (2 tests)
- `Flux2GeneratorHttpTests.cs` — added Flux2GeneratorModelHttpTests (2 tests)
- `Cli/ConfigModelTests.cs` — new file (5 tests)
- `Cli/ConfigDisplayTests.cs` — new file (10 tests)

**Key learnings:**
- **RequiredFields vs RequiredSecrets** — IProviderAdapter now has separate collections; display behavior differs
- **Backward compatibility** — adapters read model from config with fallback to defaults
- **Dependency injection in tests** — ServiceCollection pattern supports CLI adapter testing

📌 Team update (2026-04-20T14:23:47Z): CLI v0.10.0 shipped with configurable model names (RequiredFields pattern). Users can set `t2i config set foundry-mai2.model MAI-Image-2e`. Backward compatible, 238 tests pass net10.0. — decided by Kaylee
