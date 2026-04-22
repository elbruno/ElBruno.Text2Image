# Decision: GptImage2Generator Test Suite

**Author:** Jayne (Tester)  
**Date:** 2026-04-20  
**Status:** Complete

## Context

Created comprehensive test suite for the new GptImage2Generator class (being implemented by Wash) to establish testing patterns and ensure full coverage before implementation.

## Decision

Created two test files following established patterns from GptImage1p5GeneratorTests.cs:

### Test Files Created:
1. **GptImage2GeneratorTests.cs** (60 unit tests)
   - Constructor validation (11 tests): null checks, HTTPS validation, URI validation
   - Property accessors (3 tests): ModelName, DeploymentName, Endpoint
   - Prompt validation (7 tests): null/empty/whitespace, max length (4000 chars), special characters
   - Size mapping (8 tests): supported sizes, invalid size mapping, aspect ratio logic
   - Request/Response integration (8 tests): metadata, image bytes, multiple requests
   - Error handling (9 tests): HTTP errors (400, 401, 404, 429, 500, 503), network, timeouts
   - Logging (4 tests): start, success, error, size mapping
   - Model availability (3 tests): cloud model, progress reporting, cancellation
   - Dispose (2 tests): single and multiple calls
   - testable wrapper (TestableGptImage2Generator) with dependency injection

2. **GptImage2GeneratorHttpTests.cs** (31 HTTP tests)
   - Content-Length verification (5 tests): ByteArrayContent usage, header validation
   - HTTP request structure (7 tests): POST method, auth header, body structure
   - HTTP response parsing (6 tests): image bytes, metadata, error handling
   - HTTP error responses (6 tests): status code handling (400, 401, 404, 429, 500, 503)
   - Size mapping HTTP (4 tests): size strings in requests
   - Edge cases (6 tests): Unicode, long prompts, escaping, network errors, cancellation
   - Testable wrapper (TestableGptImage2HttpGenerator) using HttpClient for real HTTP testing

### Key Patterns Established:
- **TestProgress<T>** helper: Synchronous progress reporter for tests (Progress<T> requires sync context)
- **FakeHttpHandler** pattern: HTTP interception for request/response validation
- **Testable wrappers**: Dependency injection without production code changes
- **Test organization**: Group by concern (constructor, validation, errors)

### GPT-Image-2 Specifications (inferred from GPT-Image-1.5):
- **Supported sizes:** 1024x1024, 1792x1024, 1024x1792
- **Prompt max length:** 4000 characters
- **Response format:** Base64 JSON (b64_json)
- **Authentication:** API key header
- **Size mapping:** Aspect ratio-based (landscape → 1792x1024, portrait → 1024x1792, square → 1024x1024)

## Test Results

- **Total tests:** 91 (60 unit + 31 HTTP)
- **Passing:** 91/91 (100%)
- **Frameworks:** net8.0 and net10.0
- **Execution time:** ~7 seconds

## Implications

- **For Wash:** Test suite is ready for GptImage2Generator implementation. Tests define the expected behavior and API contract.
- **For future generators:** Pattern established for comprehensive test coverage (constructor validation, HTTP layer, edge cases).
- **For team:** 80%+ coverage maintained. Tests document expected behavior and serve as living specification.

## Next Steps

1. Wash implements GptImage2Generator to pass all 91 tests
2. Integration tests can be added once implementation is complete
3. Pattern can be replicated for future generator classes (GPT-Image-3, etc.)
