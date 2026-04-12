# Decisions

> Shared decision log for the ElBruno.Text2Image team. All agents read this before starting work.

<!-- Scribe merges decisions from .squad/decisions/inbox/ into this file. Do not edit directly — use the inbox. -->

### Decision: Use ByteArrayContent for BFL API requests

**Date:** 2025-07-25  
**Author:** Wash (Backend Dev)  
**Status:** Implemented  
**Issue:** #5

**Context:** The BFL API on Azure Foundry began requiring an explicit `Content-Length` header. The previous `JsonContent.Create()` could use chunked transfer encoding and omit this header.

**Decision:** Serialize JSON to UTF-8 bytes using `Flux2JsonContext`, then construct `ByteArrayContent` with `application/json; charset=utf-8`. `ByteArrayContent` always sets `Content-Length`.

**Implications:**
- Future HTTP POST/PUT to BFL API should follow this pattern
- Safer for APIs rejecting chunked transfer encoding
- No public API change — internal implementation detail

### Decision: img2img ReferenceImages API shape

**Author:** Kaylee (Core Dev)  
**Date:** 2025-07-25  
**Status:** Implemented

**Context:** Issue #6 requested image-to-image support. BFL API accepts reference images as URLs, base64, or Data URIs in a `referenceImages` array.

**Decision:**
- `ReferenceImages` is `List<string>?` on `ImageGenerationOptions` and `Flux2Request` with `[JsonIgnore(WhenWritingNull)]`
- Convenience overload `GenerateAsync(prompt, referenceImagePath, options?, ct)` handles file→base64 conversion
- M.E.AI consumers pass via `AdditionalProperties["reference_images"]`

**Implications:**
- Tests need coverage for new overload and property (throws `FileNotFoundException` for missing paths)
- Model limits (FLUX.2-pro: 8, FLUX.2-flex: 10) not enforced client-side
- `IImageGenerator` interface unchanged; overload is Flux2Generator-specific

### Decision: HTTP-Level Test Infrastructure for Flux2Generator

**Author:** Jayne (Tester)  
**Date:** 2025-07-25  
**Status:** Implemented

**Context:** Issues #5 and #6 require verifying HTTP requests sent by `Flux2Generator`. Existing suite had no request inspection capability.

**Decision:** Introduced `FakeHttpHandler` — test-only `HttpMessageHandler` capturing headers and body, returning canned responses. Inject via `Flux2Generator(..., httpClient: new HttpClient(handler))`.

**Key details:**
- `InternalsVisibleTo` added to test project
- New tests in `Flux2GeneratorHttpTests.cs` separate from `ImageGenerationTests.cs`
- `AddReferenceImageFromFile` added to `ImageGenerationOptions`

**Impact:**
- Future HTTP behavior changes should use `FakeHttpHandler` tests
- `InternalsVisibleTo` is test-only; no public API impact
