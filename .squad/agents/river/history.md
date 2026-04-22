# River — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Learnings

### 2025-01-21: Provider Adapter Implementation

**Provider Patterns:**
- **Local adapters** (CPU/CUDA/DirectML): All use `StableDiffusion15` generator with different `ExecutionProvider` settings. Default: 512×512, 20 steps, SD 1.5 model.
- **CUDA detection**: `SessionOptionsHelper.DetectBestProvider()` safely probes CUDA availability. If CUDA listed but fails to initialize, likely missing CUDA runtime DLLs (cublas, cudnn, etc.).
- **DirectML**: Windows-only. Check `OperatingSystem.IsWindows()` before attempting to create session options.
- **Cloud adapters** (Flux2/MAI-Image-2): Both use `IHttpClientFactory` for HTTP client lifetime management. HEAD requests to endpoints for quick health checks.
- **Flux2**: Default 512×512, 20 steps, model "FLUX.2-pro". Endpoint auto-conversion from `.openai.azure.com` to `.services.ai.azure.com` handled by generator.
- **MAI-Image-2**: Default 1024×1024 (MAI has min 768px dimension constraint and max 1M total pixels). No steps parameter — MAI API doesn't expose it.
- **Endpoint rewriting**: MAI-Image-2 generator internally rewrites `.openai.azure.com` → `.services.ai.azure.com`. Adapters pass user input verbatim; let the generator handle it.

**Quirks:**
- `StableDiffusion15` does lazy model download — `EnsureModelAvailableAsync` must be called. First run takes several minutes (GB-scale model download).
- Cloud generators are synchronous (no polling) except Flux2 which supports both 200 (sync) and 202 (async+poll) patterns. MaiImage2 always returns 200.
- `IHttpClientFactory.CreateClient()` returns a fresh client each time; set timeout on returned instance, not globally.
- `ImageGenerationResult.SaveAsync` handles directory creation automatically.

*Append new learnings below this line.*

📌 CLI tool merged-style implementation shipped (PR #10) — coordinated CLI delivery across five agents: Mal (scaffolding), Wash (secrets), Kaylee (commands), River (adapters), Jayne (tests). 211 tests passing on net10.0.
📌 Team update (2026-04-20T11:31:33Z): CLI now ships `t2i init` command for AI agent skill discovery. Kaylee implemented InitCommand with embedded SKILL.md resource. Mal authored canonical SKILL.md (3 copies) and updated blog. Jayne verified with 6 tests. Branch: feature/cli-init-skill, PR #11 (not merged per user). — decided by Kaylee
📌 Team update (2026-04-20T14:50:59Z): MAI-Image-2 adapter now silently auto-bumps dimensions <768px to 1024px with progress note. No breaking changes; generic 512×512 default works transparently with MAI. Coordinator merged PRs #14, #15; released cli-v0.10.1.
📌 Team update (2026-04-20T15:26:24Z): Blog post hero image generated (dogfooding t2i). Convention established: repo blog images at `images/{YYYYMMDD}-{slug}.png` + companion prompt doc. Azure MAI deployment named `MAI-Image-2e` (not `MAI-Image-2`). Kaylee reorganized 4 blog docs to `docs/blogs/`, fixed 8 links. Files staged for Bruno's review. — decided by River & Kaylee
