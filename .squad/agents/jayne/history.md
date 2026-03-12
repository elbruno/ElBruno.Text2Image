# Jayne — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

📌 Team update (2026-03-12T16:19:00Z): Multi-Platform NPU Support completed — Kaylee created QNN and OpenVINO wrappers, ExecutionProvider enum updated (QualcommQnn = 3, IntelOpenVino = 4), SessionOptionsHelper detection/creation implemented. Auto-detection order: CUDA → DirectML → QNN → OpenVINO → CPU. Build: 0 errors, 0 warnings. Tests: 91/91 passed. PR #4 created.

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

### 2026-02-28: NPU Execution Provider Tests

**Context:** Added comprehensive tests for two new NPU execution providers (QualcommQnn and IntelOpenVino) to support hardware acceleration on Qualcomm and Intel NPUs.

**Test file:** `src/ElBruno.Text2Image.Tests/ImageGenerationTests.cs` ExecutionProviderTests class

**Tests added:**
1. Updated `ExecutionProvider_HasExpectedValues` — added assertions for QualcommQnn (3) and IntelOpenVino (4) enum values
2. Updated `DetectBestProvider_ReturnsValidProvider` — added new providers to the valid set
3. `ResolveProvider_QualcommQnn_ReturnsSame` — verifies explicit QNN provider is returned unchanged
4. `ResolveProvider_IntelOpenVino_ReturnsSame` — verifies explicit OpenVINO provider is returned unchanged
5. `Create_QualcommQnn_ReturnsSessionOptions` — hardware-dependent test for QNN session creation
6. `Create_IntelOpenVino_ReturnsSessionOptions` — hardware-dependent test for OpenVINO session creation

**Key learning:** When testing execution providers that may not be available on all hardware, catch both `OnnxRuntimeException` (provider not supported) and `EntryPointNotFoundException` (native method missing from runtime). Use pattern `catch (Exception ex) when (ex is OnnxRuntimeException or EntryPointNotFoundException)` to handle both scenarios gracefully.

**Dependencies:** Tests require `using Microsoft.ML.OnnxRuntime;` for OnnxRuntimeException type.
