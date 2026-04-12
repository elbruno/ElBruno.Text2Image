# Decision: HTTP-Level Test Infrastructure for Flux2Generator

**Author:** Jayne (Tester)
**Date:** 2025-07-25
**Status:** Implemented

## Context

Issues #5 (Content-Length fix) and #6 (img2img reference images) both require verifying the HTTP request that `Flux2Generator` sends. The existing test suite had no way to inspect outgoing HTTP requests.

## Decision

Introduced `FakeHttpHandler` — a test-only `HttpMessageHandler` that captures request headers, body, and returns canned responses. Tests inject it via `Flux2Generator(..., httpClient: new HttpClient(handler))`.

This pattern should be used for all future `Flux2Generator` HTTP behavior tests.

## Key details

- `InternalsVisibleTo` added to `ElBruno.Text2Image.Foundry.csproj` so tests can directly verify `Flux2Request` serialization via `Flux2JsonContext`.
- All new tests live in `Flux2GeneratorHttpTests.cs`, separate from the original `ImageGenerationTests.cs`.
- `AddReferenceImageFromFile` was added to `ImageGenerationOptions` as a convenience method for the img2img feature.

## Impact

- Kaylee / Wash: Future HTTP-level behavior changes should have corresponding tests using `FakeHttpHandler`.
- Mal: The `InternalsVisibleTo` is test-project-only and doesn't affect the public API surface.
