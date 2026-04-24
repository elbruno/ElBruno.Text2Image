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

### 2026-04-22: Configurable Timeout for Slow Providers (#19)

**Problem Context:**
Azure GPT-Image-2 API can take 3-4 minutes to generate images, but default HttpClient timeout is 100 seconds. Generations were failing with timeout errors, especially for complex prompts.

**Implementation:**
- **CLI Integration:** Added `--timeout` option to GenerateCommand (default: 300 seconds). Value passed through ExtraOptions dictionary in GenerationRequest.
- **Generator Constructors:** Added optional `timeoutSeconds: int?` parameter to all cloud generators (Flux2Generator, MaiImage2Generator, GptImage1p5Generator, GptImage2Generator). When provided, sets `HttpClient.Timeout` on injected client instance.
- **Adapter Pattern:** All four provider adapters (FoundryFlux2Adapter, FoundryMaiImage2Adapter, FoundryGptImage1p5Adapter, FoundryGptImage2Adapter) parse timeout from `req.ExtraOptions["timeout"]` and pass to generator constructor.
- **Backward Compatibility:** Null timeout parameter means "don't modify HttpClient.Timeout" — preserves existing behavior and any pre-configured timeouts.

**Key Design Choices:**
- **Default 300 seconds (5 minutes):** Chosen to accommodate GPT-Image-2's 3-4 minute generation time with safety margin. Overrides HttpClient's 100-second default which is too short for slow providers.
- **Per-request timeout:** Timeout configured per GenerateAsync call via HttpClient instance, not globally. Each provider adapter creates fresh HttpClient from IHttpClientFactory, sets timeout, uses it for one generation.
- **ExtraOptions transport:** Timeout flows from CLI → GenerationRequest.ExtraOptions → Adapter → Generator constructor. Keeps timeout in same channel as endpoint/apiKey overrides.

**Testing Coverage (Jayne):**
31 tests in TimeoutConfigurationTests.cs covering constructor parameter acceptance, HttpClient.Timeout property modification, backward compatibility, boundary values (1s, 86400s, infinite), and independence of multiple generator instances.

**Documentation:**
- Updated `docs/cli-tool.md` with `--timeout` option in Generate Command Options table
- Added "Extended Timeout for Slow Providers" example section showing GPT-Image-2 usage with `--timeout 300`
- Updated README.md with timeout example in GPT-Image-2 CLI example

**Files Modified:**
- CLI: `GenerateCommand.cs` (added --timeout option, pass via ExtraOptions)
- Adapters: `FoundryFlux2Adapter.cs`, `FoundryMaiImage2Adapter.cs`, `FoundryGptImage1p5Adapter.cs`, `FoundryGptImage2Adapter.cs` (parse timeout, pass to generator)
- Generators: `Flux2Generator.cs`, `MaiImage2Generator.cs`, `GptImage1p5Generator.cs`, `GptImage2Generator.cs` (add timeoutSeconds parameter, set HttpClient.Timeout)
- Docs: `README.md`, `docs/cli-tool.md` (document --timeout option)
- Tests: `TimeoutConfigurationTests.cs` (new file, 31 tests)

**Branch:** `squad/19-configurable-timeout`
**Commit:** `c9e1f9c` — "feat: add configurable timeout for image generation providers (#19)"

📌 CLI tool merged-style implementation shipped (PR #10) — coordinated CLI delivery across five agents: Mal (scaffolding), Wash (secrets), Kaylee (commands), River (adapters), Jayne (tests). 211 tests passing on net10.0.
📌 Team update (2026-04-20T11:31:33Z): CLI now ships `t2i init` command for AI agent skill discovery. Kaylee implemented InitCommand with embedded SKILL.md resource. Mal authored canonical SKILL.md (3 copies) and updated blog. Jayne verified with 6 tests. Branch: feature/cli-init-skill, PR #11 (not merged per user). — decided by Kaylee
📌 Team update (2026-04-20T14:50:59Z): MAI-Image-2 adapter now silently auto-bumps dimensions <768px to 1024px with progress note. No breaking changes; generic 512×512 default works transparently with MAI. Coordinator merged PRs #14, #15; released cli-v0.10.1.
📌 Team update (2026-04-20T15:26:24Z): Blog post hero image generated (dogfooding t2i). Convention established: repo blog images at `images/{YYYYMMDD}-{slug}.png` + companion prompt doc. Azure MAI deployment named `MAI-Image-2e` (not `MAI-Image-2`). Kaylee reorganized 4 blog docs to `docs/blogs/`, fixed 8 links. Files staged for Bruno's review. — decided by River & Kaylee

### 2026-04-22: Multi-Concept Image Prompting for Marketing Assets

**Prompt Engineering for Complex Narratives:**
Designed hero image prompt for GPT-Image models + t2i skill announcement (`docs/blogs/20260422-gpt-images-and-skills-image-prompt.md`). Challenge: communicate three distinct concepts in one cohesive image: (1) AI agent integration in developer workflow, (2) multiple model pathways/choice, (3) skill as unifying mechanism.

**Composition Strategies:**
- **Spatial metaphors:** Left-to-right flow (IDE → agent → streams → integration) follows natural reading direction. Streams branching upward/outward = expansion, growth, capability increase.
- **Color coding:** Three streams with distinct colors (golden, electric blue, magenta) visually encodes "multiple options" without requiring text labels. Each color carries semantic weight (warmth, speed, creativity).
- **Symbolic anchors:** Puzzle piece icon for integration concept universally understood in tech UX. Placed at stream convergence = skill is the unifying layer.
- **Avoid text rendering:** Explicit "no readable text" + describing UI elements by glow/shape rather than pixel-accurate mockups. Diffusion models still struggle with legible glyphs; better to suggest silhouettes.

**16:9 Hero Image Constraints:**
- Call out aspect ratio both in prompt text ("16:9 framing") and in CLI args (`--width 1280 --height 768`) to guide model composition.
- Leave ~2/3 of canvas for "the magic happening" (right side) so the image works as a blog header crop without cutting off key elements.
- Deep backgrounds (space blue gradient, subtle patterns) + holographic/neon accents ensure readability on both light and dark blog themes.

**Prompt Remixability:**
Documented remix patterns in companion doc: change stream count for different feature scopes, swap integration icon for different concepts (lightning = speed, shield = security), adjust art direction keywords (holographic → watercolor → low-poly) to shift aesthetic without rewriting entire prompt structure.
