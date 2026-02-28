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
