# Jayne — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

*Append new learnings below this line.*

### 2026-02-28: SkippableFact Package Fix

**Context:** Platform-conditional tests were using `[SkippableFact]` attribute (added in commit 5540edb) but the `Xunit.SkippableFact` NuGet package was not referenced in the test project.

**Issue:** The tests compiled and ran locally on Windows, but the missing package reference would cause build failures in environments that don't have the package cached.

**Fix:** Added `<PackageReference Include="Xunit.SkippableFact" Version="1.*" />` to ElBruno.Text2Image.Tests.csproj.

**Tests affected:** 4 tests in ExecutionProviderTests that conditionally skip when ONNX Runtime native library is unavailable:
- DetectBestProvider_ReturnsValidProvider
- ResolveProvider_Auto_ReturnsConcreteProvider  
- Create_Auto_ReturnsSessionOptions
- DetectBestProvider_IsCached

**Key learning:** Always verify that attribute packages are referenced when introducing new xUnit extensions like SkippableFact/SkippableTheory.

### 2025-07-25: HTTP-level tests for Flux2Generator (Issues #5 & #6)

**Context:** Wrote 43 new tests covering Wash's Content-Length fix (Issue #5) and Kaylee's img2img reference images feature (Issue #6). Baseline was 87 tests, now 130 — all green on net8.0 and net10.0.

**Test file:** `src/ElBruno.Text2Image.Tests/Flux2GeneratorHttpTests.cs`

**Key patterns established:**
- `FakeHttpHandler` — a reusable `HttpMessageHandler` that intercepts requests, captures headers/body, and returns canned responses. Inject via `Flux2Generator(..., httpClient: httpClient)`.
- `FakeHttpHandler.CreateSuccessResponse()` — returns a minimal `{"created":1234,"data":[{"b64_json":"..."}]}` so GenerateAsync completes successfully.
- `InternalsVisibleTo` added to `ElBruno.Text2Image.Foundry.csproj` → allows direct `Flux2Request` / `Flux2JsonContext` serialization tests.

**Edge cases discovered:**
- `ArgumentException.ThrowIfNullOrWhiteSpace(null)` throws `ArgumentNullException` (subclass), so tests must use `Assert.ThrowsAny<ArgumentException>`, not `Assert.Throws<ArgumentException>`.
- Non-existent file path may throw `DirectoryNotFoundException` (not `FileNotFoundException`) if the parent directory is also missing. Use `Assert.ThrowsAny<IOException>`.
- `JsonIgnore(Condition = WhenWritingNull)` on `List<string>?` correctly omits null but preserves empty arrays — important for the API contract.

**Source stubs added:**
- `ImageGenerationOptions.ReferenceImages` (property) and `AddReferenceImageFromFile()` (convenience method) — merged with Kaylee's existing property, added file-to-DataURI helper.
- `Flux2Request.ReferenceImages` with `[JsonIgnore(WhenWritingNull)]` — already present from Kaylee's work.

**Test classes and counts:**
- `Flux2GeneratorContentLengthTests` — 10 tests (Content-Length, valid JSON, fields, API key, POST method, error handling)
- `ImageGenerationOptionsReferenceImagesTests` — 8 tests (default null, set, round-trip, empty, single, multi, reset to null)
- `AddReferenceImageFromFileTests` — 11 tests (PNG/JPEG/WEBP, multiple files, append, unknown ext, null/empty/whitespace/missing file)
- `Flux2RequestSerializationTests` — 5 tests (null omits, single, empty array, multiple, required fields)
- `Flux2GeneratorReferenceImagesTests` — 9 tests (include/omit/null/multi/empty in request body, file-based, result validity)

📌 Team update (2026-04-12T20:00:28Z): Issues #5 and #6 merged to main, PR #7 closed, v0.7.0 released. 260 tests passing, all agents coordinated successfully. — decided by Scribe
📌 Team update (2026-04-13T13:18:04Z): MAI-Image-2 HTTP tests complete. Created `MaiImage2GeneratorHttpTests.cs` with 32 tests using `FakeHttpHandler` pattern. Coverage: generation, polling, error handling, validation, options passthrough. 324 total tests passing. Branch: feature/mai-image-2-support. — decided by Scribe