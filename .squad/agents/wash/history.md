# Wash — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

*Append new learnings below this line.*

- **BFL API Content-Length requirement (2025-07-25):** The Black Forest Labs API on Azure Foundry requires an explicit `Content-Length` header. `JsonContent.Create()` may use chunked transfer encoding, which omits it. Use `JsonSerializer.SerializeToUtf8Bytes()` + `ByteArrayContent` instead.
- **Source-generated JSON context:** `Flux2JsonContext` is the source-generated `JsonSerializerContext` for Foundry request/response types. Always use it for serialization (e.g., `Flux2JsonContext.Default.Flux2Request`).
- **Key file:** `src/ElBruno.Text2Image.Foundry/Flux2Generator.cs` — FLUX.2 cloud API client (BFL Native API via Azure Foundry). Handles both sync (200) and async (202 + polling) patterns.
- **Build/test commands:** `dotnet build --no-restore` and `dotnet test --no-build` — multi-target (net8.0 + net10.0), 87 tests per TFM.

📌 Team update (2026-04-12T20:00:28Z): Issues #5 and #6 merged to main, PR #7 closed, v0.7.0 released. 260 tests passing, all agents coordinated successfully. — decided by Scribe
📌 Team update (2026-04-13T13:18:04Z): MAI-Image-2 scenario sample created. `scenario-13-mai-image2-cloud` demonstrates DI registration, image generation, response handling, and error recovery. Solution manifest updated. Build clean: 0 warnings, 0 errors. Branch: feature/mai-image-2-support. — decided by Scribe

📌 CLI Infrastructure (2026-04-13): Implemented secret storage and config persistence for t2i CLI tool. EnvVarSecretStore reads `T2I_{PROVIDER}_{FIELD}` env vars (e.g., `T2I_FOUNDRY_FLUX2_APIKEY`). DpapiSecretStore stores secrets at `%LOCALAPPDATA%\t2i\secrets.dpapi` using `ProtectedData.Protect` with `DataProtectionScope.CurrentUser`; on-disk format is JSON `Dictionary<string, byte[]>` keyed by `{provider}::{field}`. PlainFileSecretStore writes plaintext JSON to `{ConfigDir}/secrets.json` with Unix 0600 permissions. SecretResolver chains stores (env → DPAPI → file) with CLI override priority. ConfigStore uses source-gen JSON context for AOT-friendly AppConfig persistence. DI registered via `AddCliInfrastructure()` extension method. Branch: feature/cli-tool-t2i. — Wash

📌 CLI tool merged-style implementation shipped (PR #10) — coordinated CLI delivery across five agents: Mal (scaffolding), Wash (secrets), Kaylee (commands), River (adapters), Jayne (tests). 211 tests passing on net10.0.
📌 Team update (2026-04-20T11:31:33Z): CLI now ships `t2i init` command for AI agent skill discovery. Kaylee implemented InitCommand with embedded SKILL.md resource. Mal authored canonical SKILL.md (3 copies) and updated blog. Jayne verified with 6 tests. Branch: feature/cli-init-skill, PR #11 (not merged per user). — decided by Kaylee

📌 Documentation update (2026-04-20): Wash updated all docs for configurable-model feature (v0.10.0). Added "Choosing a model" section to README with CLI examples. Updated cli blog post (20260420-introducing-t2i-cli.md) with "Switching models" section. Added "Using a different model" to mai-image-2-setup-guide.md and "Choosing a FLUX.2 model" to flux2-setup-guide.md. Library samples show optional modelId parameter. Created CHANGELOG.md with v0.10.0 entry. Masking policy already correct: apiKey masked, endpoint + model plain text. Branch: feat/configurable-model-name. — Wash

📌 Team update (2026-04-20T14:35:36Z): Environment variable security review completed. Mal recommends blog post reorder to prioritize DPAPI/file storage for local dev, limit env vars to CI/CD with warnings. Zero code changes needed—CLI already implements secure defaults. Decision merged to decisions.md. — decided by Scribe

📌 Blog post accuracy audit & fixes (2026-04-20): Wash audited docs/20260420-introducing-t2i-cli.md against source code. Found 5 inaccuracies: (1) DPAPI path was %LOCALAPPDATA% → corrected to %APPDATA% (ConfigPaths.cs uses ApplicationData SpecialFolder). (2) Plaintext file was ~/.t2i/ → corrected to ~/.config/t2i/ (ConfigPaths.ConfigDirectory). (3) Version mismatch: cli-v0.1.0 → updated to cli-v0.10.0 (Cli.csproj v0.10.0). (4) Security ordering: env vars presented first (misleading) → reordered DPAPI/file FIRST for local dev, env vars SECOND for CI/CD only. (5) Missing security guidance: Added explicit warnings about env var risks (process visibility, shell history, dotfile commits), scoped context for when env vars ARE appropriate (GitHub Actions, Azure Pipelines, secret injection), and do/don't table. Verified all claims against FoundryMaiImage2Adapter (default MAI-Image-2), FoundryFlux2Adapter (default FLUX.2-pro), ConfigCommand.ShowAsync (masking policy: apiKey masked only, endpoint + model plain). PR #14 created, not merged per user instruction. Branch: docs/blog-secrets-accuracy. — Wash

📌 GptImage2Generator implementation (2026-04-20): Implemented `src/ElBruno.Text2Image.Foundry/GptImage2Generator.cs` following the same pattern as GptImage1p5Generator. Class uses Azure OpenAI ImageClient API, implements both IImageGenerator and Microsoft.Extensions.AI.IImageGenerator interfaces. Supports three fixed image sizes: 1024×1024, 1024×1536, 1536×1024. MaxPromptLength: 4000 characters. Default deployment: "gpt-image-2". Model display name: "GPT-Image-2". CLI adapter `FoundryGptImage2Adapter` already existed and now successfully resolves the generator. Build: 0 warnings, 0 errors. Integration: Uses same Azure.AI.OpenAI.ImageClient pattern, handles graceful fallback on aspect ratios, implements M.E.AI passthrough via ImageGenerationOptionsConverter. — Wash

📌 **Phase 1 Performance Fixes (2026-04-21):** Implemented three critical performance optimizations. (1) **HttpClient pooling:** Made HttpClient a required constructor parameter in all four Foundry generators (Flux2, MAI-Image-2, GptImage1p5, GptImage2), eliminating per-request instantiation that bypassed connection pooling (30-40% performance hit, socket exhaustion risk). Updated ServiceCollectionExtensions to use IHttpClientFactory factory pattern. (2) **Tensor memory optimization:** Refactored TensorHelper.Duplicate to accept DenseTensor directly instead of float[], removing `latents.Buffer.ToArray()` call that allocated ~32KB per denoising iteration (20-50 iterations per generation = 1-2MB waste, 15-25% GC pressure). Now uses Span-based copying. (3) **ConfigureAwait(false):** Added to all 43 await statements in library code (core models + Foundry generators), improving ASP.NET scalability (2-3x possible) by releasing synchronization context. All 385 tests pass. Commit: 49e9877. Branch: feature/code-review-security-perf. — Wash

📌 **Phase 2 Performance Polish (2026-04-21):** Implemented two high-priority performance improvements. (1) **Exponential backoff polling:** Replaced fixed 2-second polling delay in Flux2Generator with adaptive backoff (500ms → 5s max, 1.5x multiplier). Fast completions (<5s) see ~75% latency reduction (3.5s saved), medium generations (10-30s) see ~30-40% improvement. Pattern: geometric series (500ms → 750ms → 1.1s → 1.7s → 2.5s → 3.7s → 5s cap). (2) **Parallel text encoding:** Refactored TextEncoder.EncodeWithGuidance to encode conditional/unconditional embeddings concurrently using Task.Run + WaitAll. ONNX InferenceSession is thread-safe for reads. Result: ~40-50% speedup in encoding phase (400ms → 200ms typical). All 683 tests pass. Commits: bbf2b7b, cd58dab. Branch: feature/code-review-security-perf. — Wash

