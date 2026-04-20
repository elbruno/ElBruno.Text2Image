# Jayne — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

*Append new learnings below this line.*

### 2025-01-XX: CLI Infrastructure Tests (50 tests, 100% pass rate on net10.0)

**Context:** Added comprehensive test coverage for the new CLI tool infrastructure on branch `feature/cli-tool-t2i`. Tests target `net10.0` only via conditional project reference and `#if NET10_0_OR_GREATER` directives, since the CLI project is net10.0-only.

**Test coverage breakdown:**
- **SecretResolverTests** (7 tests) — resolution chain, CLI override priority, fallback logic, InspectAsync, SetAsync store selection
- **EnvVarSecretStoreTests** (6 tests) — naming convention (`T2I_PROVIDER_FIELD`), list fields, NotSupported on set/delete
- **DpapiSecretStoreTests** (4 tests) — round-trip encryption on Windows, platform availability, field listing
- **PlainFileSecretStoreTests** (6 tests) — JSON persistence, deletion, field filtering, Unix file mode 0600 on Linux
- **ConfigStoreTests** (4 tests) — round-trip JSON serialization, default on missing file, atomic writes (no `.tmp` leftover), parent directory creation
- **ConsoleHelpersTests** (21 tests) — `Slug()` (slugification, unicode handling, truncation, default fallback), `Mask()` (prefix+suffix reveal, short secret handling, null/empty)
- **CommandSurfaceSmokeTest** (2 skipped) — scaffolded for future end-to-end validation when CLI commands are wired

**Key patterns established:**
- `FakeSecretStore` — test double implementing `ISecretStore` with in-memory dictionary, `IsAvailable` toggle for platform simulation
- `[Collection]` attribute + unique temp directories per test class to prevent file lock conflicts (ConfigStore and PlainFileSecretStore use shared file paths)
- Environment variable manipulation for config path overrides (`APPDATA`, `XDG_CONFIG_HOME`) with cleanup in `Dispose()`
- xUnit `[Theory]` + `[InlineData]` for parameterized tests (slug variations, mask edge cases)

**Bug discovered:**
- Filed `.squad/decisions/inbox/jayne-found-bug-secretresolver-missing-delete.md` — `SecretResolver` is missing a `DeleteAsync` method but commands in `SecretsCommand.cs` and `ConfigCommand.cs` attempt to call it. Wash needs to add this method to match the established pattern.

**Integration notes:**
- Tests run ONLY on net10.0 (via `#if NET10_0_OR_GREATER` + conditional `<ProjectReference>` in test csproj)
- Existing tests (net8.0 + net10.0) continue to pass — CLI tests are isolated
- Smoke tests skipped until command wiring is complete
- File locking issues resolved by using separate temp directories per test + Thread.Sleep(100) before cleanup

**Test execution:**
```
dotnet test src/ElBruno.Text2Image.Tests/ --no-restore --verbosity minimal --framework net10.0 --filter "FullyQualifiedName~Cli"
Passed: 50, Skipped: 2
```

**Files added:**
- `src/ElBruno.Text2Image.Tests/Cli/Secrets/FakeSecretStore.cs`
- `src/ElBruno.Text2Image.Tests/Cli/Secrets/SecretResolverTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/Secrets/EnvVarSecretStoreTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/Secrets/DpapiSecretStoreTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/Secrets/PlainFileSecretStoreTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/ConfigStoreTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/ConsoleHelpersTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/CommandSurfaceSmokeTest.cs`

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

📌 CLI tool merged-style implementation shipped (PR #10) — coordinated CLI delivery across five agents: Mal (scaffolding), Wash (secrets), Kaylee (commands), River (adapters), Jayne (tests). 211 tests passing on net10.0.

### 2025-MM-DD: InitCommand Test Coverage (6 tests, 100% pass rate on net10.0)

**Context:** Kaylee implemented `InitCommand` which writes an embedded `SKILL.md` resource (from `ElBruno.Text2Image.Cli.Skills.SKILL.md`) to `.github/skills/t2i/SKILL.md` and/or `.claude/skills/t2i/SKILL.md` based on the `--target` setting (github, claude, all). Supports `--force` to overwrite existing files.

**Test file:** `src/ElBruno.Text2Image.Tests/InitCommandTests.cs`

**Test coverage breakdown:**
- `Init_WritesBothFiles_WhenTargetAll` — Both .github and .claude paths exist, contain "# t2i" marker
- `Init_WritesOnlyGithub_WhenTargetGithub` — Only .github path exists, .claude does not
- `Init_WritesOnlyClaude_WhenTargetClaude` — Only .claude path exists, .github does not
- `Init_SkipsExistingFile_WithoutForce` — Pre-existing file is preserved (sentinel content remains)
- `Init_OverwritesExistingFile_WithForce` — Pre-existing file is replaced with skill content
- `Init_CreatesParentDirectories` — Parent directories are created automatically

**Key patterns established:**
- **CommandContext construction** — `new CommandContext(Array.Empty<string>(), fakeRemainingArgs, "init", null)` where `fakeRemainingArgs` implements `IRemainingArguments` with `Raw` as `IReadOnlyList<string>` and `Parsed` as empty `ILookup`
- **CWD isolation** — Use `Directory.SetCurrentDirectory(_testDir)` in constructor, restore in `Dispose()` for tests that rely on current working directory (InitCommand writes relative to CWD)
- **Temp directory per test class** — `Path.Combine(Path.GetTempPath(), $"t2i-init-test-{Guid.NewGuid():N}")` prevents cross-test pollution
- **Embedded resource verification** — Check for content marker ("# t2i") to validate embedded resource was written correctly without hardcoding full content
- **Sentinel pattern** — Pre-create files with "PRE-EXISTING CONTENT" to verify skip/overwrite behavior
- **Spectre.Console.Cli dependency** — Test project needs conditional `<PackageReference Include="Spectre.Console.Cli" Version="0.49.1" Condition="'$(TargetFramework)' == 'net10.0'" />` to construct `CommandContext`

**InternalsVisibleTo:** Already configured in `src/ElBruno.Text2Image.Cli/Properties/AssemblyInfo.cs` — no changes needed.

**Test execution:**
```
dotnet test src/ElBruno.Text2Image.Tests/ --verbosity minimal --filter "FullyQualifiedName~InitCommand" --framework net10.0
Test summary: total: 6, failed: 0, succeeded: 6, skipped: 0
```

**Files added:**
- `src/ElBruno.Text2Image.Tests/InitCommandTests.cs`

**Files modified:**
- `src/ElBruno.Text2Image.Tests/ElBruno.Text2Image.Tests.csproj` (added Spectre.Console.Cli reference)

### 2026-04-20: Configurable Model Name Tests (21 tests, 100% pass rate on net8.0 + net10.0)

**Context:** Added comprehensive test coverage for configurable model names on branch `feat/configurable-model-name`. Kaylee implemented the feature to allow MAI-Image-2 and FLUX.2 model names to be configurable via CLI config (e.g., `MAI-Image-2e`, `FLUX.2-flex`). Tests validate that model names flow through HTTP requests, config storage, and masking policy.

**Test coverage breakdown:**
- **MaiImage2GeneratorModelHttpTests** (2 tests) — custom model "MAI-Image-2e" and default "mai-image-2" appear in request body JSON
- **Flux2GeneratorModelHttpTests** (2 tests) — custom model "FLUX.2-flex" and default "FLUX.2-pro" appear in request body JSON
- **ConfigModelTests** (5 tests) — ProviderConfig.Model round-trips through ConfigStore, defaults to null when not set, multiple providers with different models
- **ConfigDisplayTests** (10 tests) — ConsoleHelpers.Mask applied to secrets starting with "sk-", RequiredFields contains endpoint+model, RequiredSecrets contains only apiKey (both adapters)

**Key patterns reused:**
- **FakeHttpHandler** — already established in Flux2GeneratorHttpTests.cs, captures request body for JSON assertions
- **Temp directories per test class** — same pattern as ConfigStoreTests to prevent file lock conflicts
- **ServiceCollection + IHttpClientFactory** — needed to construct adapters (FoundryMaiImage2Adapter, FoundryFlux2Adapter) which depend on Microsoft.Extensions.Http

**Test execution:**
```
dotnet test src/ElBruno.Text2Image.Tests/ --no-restore --verbosity minimal
Passed: 238 (net10.0), 166 (net8.0), Skipped: 2, Total: 240 (net10.0), 166 (net8.0)
```

**Files added:**
- `src/ElBruno.Text2Image.Tests/Cli/ConfigModelTests.cs`
- `src/ElBruno.Text2Image.Tests/Cli/ConfigDisplayTests.cs`

**Files modified:**
- `src/ElBruno.Text2Image.Tests/MaiImage2GeneratorHttpTests.cs` — added MaiImage2GeneratorModelHttpTests class
- `src/ElBruno.Text2Image.Tests/Flux2GeneratorHttpTests.cs` — added Flux2GeneratorModelHttpTests class

**Key learnings:**
- **RequiredFields vs RequiredSecrets** — IProviderAdapter now has separate collections; RequiredFields (endpoint, model) display plain text, RequiredSecrets (apiKey) are masked in `config show`
- **Backward compatibility** — adapters read model from config with fallback to defaults ("MAI-Image-2", "FLUX.2-pro") so existing configs without Model field continue working
- **Dependency injection in tests** — when testing CLI adapters that require IHttpClientFactory, use ServiceCollection.AddHttpClient() + BuildServiceProvider() to construct dependencies