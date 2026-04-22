# Mal — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Core Context

### CLI Architecture Decisions (2026-04-19 to 2026-04-20)

- **Command Framework:** Spectre.Console.Cli v0.49.1 (stable, DI-first design, native Spectre.Console integration)
- **CLI Target:** net10.0 only (single TFM, RollForward=LatestMajor for forward compatibility, simpler packaging)
- **Provider Pattern:** IProviderAdapter abstraction for cloud (Foundry) and future local (CPU/CUDA/DirectML) providers
- **Secret Storage:** Multi-tier ISecretStore with EnvVarSecretStore, DpapiSecretStore (Windows), PlainFileSecretStore (fallback)
- **Config Pattern:** AppConfig + ProviderConfig JSON with source-generated contexts for AOT
- **CLI Editions:** Lite (cloud only, v0.10.0) ships first; Full edition (local + cloud) planned

### Key Features (2026-04-20)

1. **Configurable Model Names:** Models stored in ProviderConfig, defaults in adapter code, backward compatible
2. **Skill Files:** Aspire-inspired `t2i init` command writes `.github/skills/` and `.claude/skills/` for AI agents
3. **RequiredFields/RequiredSecrets Split:** Separate non-sensitive config (endpoint, model) from secrets (apiKey)

## Learnings

### 2026-04-20 — Configurable Model Names (v0.10.0)

- **Config schema extensibility:** `ProviderConfig.Model` property existed but unused — zero schema changes needed, only adapter implementation
- **Adapter defaults > schema defaults:** Keep config minimal (users only see Model if overriding); defaults ("MAI-Image-2", "FLUX.2-pro") in adapter code
- **Setup wizard transparency:** Prompt with defaults shown, blank input accepts default, write explicitly to config
- **Testing pattern:** Use `FakeHttpHandler` to verify model names flow through HTTP request bodies
- **Masking policy:** apiKey masked, endpoint+model plain text (URLs not secrets)

📌 Team update (2026-04-20T14:23:47Z): CLI v0.10.0 shipped with configurable model names (RequiredFields pattern). Users can set `t2i config set foundry-mai2.model MAI-Image-2e`. Backward compatible, 238 tests pass net10.0. — decided by Kaylee

### 2026-04-20 — Secret Storage Security Analysis

**Context:** Bruno raised concerns about the blog post over-promoting environment variables as "best for CI/CD" without explaining the security trade-offs for local development.

**Secret Storage Backends Inventory (from code):**
1. **EnvVarSecretStore** — reads `T2I_<PROVIDER>_<FIELD>` from process environment (read-only, no write support)
2. **DpapiSecretStore** (Windows only) — encrypts secrets with `DataProtectionScope.CurrentUser` via Windows DPAPI, stored at `%LOCALAPPDATA%\t2i\secrets.dpapi`
3. **PlainFileSecretStore** (all platforms) — stores secrets in `~/.t2i/secrets.json` with `0600` permissions (user-only read/write), prints warning on first use

**Resolution Chain (SecretResolver.cs):**
1. CLI flags (`--api-key`) — ephemeral, highest priority
2. Environment variables — CI/CD-first, inherited by child processes
3. DPAPI (Windows) or plaintext file (macOS/Linux) — **default write target** (line 78: prefers DPAPI if available)

**Environment Variable Risk Profile:**
- Process tree visibility (`/proc/<pid>/environ` on Linux, `Get-Process` on Windows)
- Shell history leakage (`export T2I_API_KEY=...` in `.bash_history`)
- Dotfile commits (`.bashrc`, `.zshrc` with hardcoded `export` statements)
- Docker layer leakage (`ENV` directives in Dockerfile)
- Accidental logging (debug output, CI/CD `set -x` mode)

**Blog Post Structure Issue:**
The blog post (`docs/20260420-introducing-t2i-cli.md`) presents secrets storage with **Environment Variables** first, labeled "Best for CI/CD," which misleads readers into thinking they're the recommended default for all scenarios (including local development).

**Recommendation:** Reorder the blog post to present **DPAPI (Windows) / plaintext file (macOS/Linux)** FIRST as the recommended default for local development, with environment variables clearly scoped to **CI/CD pipelines only**, including explicit warnings about local dev risks.

**Decision:** Created decision document at `.squad/decisions/inbox/mal-secret-storage-recommendation.md` with proposed blog post rewrite. Awaiting Bruno's approval before implementation.

**Learnings:**
- The CLI already does the right thing (DPAPI default on Windows, file fallback elsewhere) — this is a **documentation fix**, not a code fix
- Users conflate "presented first" with "recommended" — ordering matters in blog posts
- Security guidance must be **context-specific** (local dev vs CI/CD have opposite recommendations)
- Good opportunity to add a security best practices callout box (do/don't list)

### 2026-04-21 — GPT-Image-2 Architecture Analysis

**Context:** Bruno requested architecture guidance for GPT-Image-2 support. Investigation revealed implementation already exists but is incomplete.

**Key Findings:**

1. **Implementation Status (90% Complete):**
   - Core generator (`GptImage2Generator.cs`) exists and compiles
   - CLI adapter (`FoundryGptImage2Adapter.cs`) exists
   - Sample code (`scenario-16-gpt-image-2-cloud`) fully functional with README
   - Missing: DI extension method, CLI registration, unit tests

2. **Architecture Pattern:**
   - GPT-Image-2 uses **identical pattern** to GPT-Image-1.5 (Azure OpenAI SDK)
   - Does **NOT** use MAI-Image-2 pattern (Foundry MAI API with async polling)
   - Same endpoint structure: `https://{resource}.openai.azure.com/`
   - Same authentication: Azure Key Credential
   - Same API client: `Azure.AI.OpenAI.ImageClient`

3. **Size Constraints:**
   - Both GPT-Image-1.5 and GPT-Image-2 support only 3 fixed sizes: 1024×1024, 1024×1536, 1536×1024
   - Sample README incorrectly claims 1792×1024 support (open-source model supports it, Azure doesn't)
   - **Bug Found:** Both generators hardcode `GeneratedImageSize.W1024xH1024` on line 117, ignoring user's mapped size

4. **Differences from GPT-Image-1.5:**
   - Cosmetic only: display name, default deployment name, XML doc comments
   - No functional differences in code (byte-for-byte identical logic)
   - Separate models allow users to maintain distinct Azure deployments/quotas

5. **Completion Tasks:**
   - Add `AddGptImage2Generator()` to `ServiceCollectionExtensions.cs`
   - Register `FoundryGptImage2Adapter` in CLI DI container
   - Fix hardcoded size bug in both GPT-Image-1.5 and GPT-Image-2
   - Create `GptImage2GeneratorTests.cs` (copy from GPT-Image-1.5's 238 tests)

**Learnings:**
- **Discovered implementation before planning** — always grep for existing code first to avoid duplicate work
- **Pattern recognition accelerates analysis** — comparing MAI-Image-2 vs GPT-Image-2 revealed Azure OpenAI vs Foundry MAI API split
- **Hardcoded constants hide bugs** — the `GeneratedImageSize.W1024xH1024` was copied from GPT-Image-1.5 and never updated, affecting both generators
- **README accuracy matters** — sample docs claiming 1792×1024 support could mislead users when Azure API rejects the request
- **"Why?" drives architecture decisions** — understanding that GPT-Image-2 uses Azure OpenAI (not Foundry MAI) explains why it matches GPT-Image-1.5 pattern

**Decision:** Created comprehensive architecture document at `.squad/decisions/inbox/mal-gpt-image-2-architecture.md` covering implementation status, comparison matrix, size constraints, integration points, and completion tasks.

### 2026-04-21 — CLI Version Sync & Model Documentation Issue

**Context:** Bruno reported CLI package on NuGet is v0.11.0 while codebase is v0.16.0, and CLI doesn't advertise GPT-Image-1.5 or GPT-Image-2 support.

**Investigation:**
1. **Version Audit:** All packages at 0.16.0 in code; NuGet shows:
   - Main: 0.16.0 ✓
   - Foundry: 0.16.0 ✓
   - CPU/CUDA/DirectML: 0.16.0 ✓
   - CLI: **0.11.0** ✗ (missing 0.12.0–0.16.0)

2. **Root Cause — Workflow Architecture:**
   - Two publish workflows exist: `publish.yml` (main packages) and `publish-cli.yml` (CLI only)
   - `publish.yml` excludes tags starting with `cli-*` (line 15)
   - `publish-cli.yml` requires tags starting with `cli-*` to trigger
   - Versions 0.12.0 used `cli-v0.12.0` tag → CLI published ✓
   - Versions 0.13.0–0.16.0 used generic `v0.X.Y` tags → CLI never published ❌

3. **Model Documentation Audit:**
   - ✓ GPT-Image-1.5 and GPT-Image-2 generators exist and registered in CLI
   - ✓ Adapter classes (`FoundryGptImage1p5Adapter`, `FoundryGptImage2Adapter`) present
   - ✗ CLI package description mentions only FLUX.2, MAI-Image-2
   - ✗ SKILL.md omits GPT models from provider table
   - ✗ No configuration examples for Azure OpenAI endpoints

**Two-Part Fix:**

**Part A — Restore CLI to v0.16.0:**
- Create releases with tags `cli-v0.13.0`, `cli-v0.14.0`, `cli-v0.15.0`, `cli-v0.16.0`
- Each triggers `publish-cli.yml` automatically
- Coordinate with Kaylee to execute

**Part B — Document GPT Models:**
- Update `.csproj` description to mention GPT-Image-1.5 and GPT-Image-2
- Add GPT providers to SKILL.md table
- Add Azure OpenAI configuration section to SKILL.md

**Key Insight:**
The two-workflow design is correct — it allows independent CLI and main package release cadences. The issue was **tag naming discipline:** v0.13–0.16 should have had both `v0.X.Y` (for main packages) and `cli-v0.X.Y` (for CLI) tags.

**Decision:** Created detailed diagnosis and fix plan at `.squad/decisions/inbox/mal-cli-version-sync.md`. Routed Part A (publish missing CLI versions) to Kaylee via squad routing. Bruno to approve Part B documentation updates.

📌 Team update (2026-04-22T10:28:06Z): CLI version sync decision merged. Mal diagnosed missing cli-v0.13.0–0.16.0 releases; Kaylee updated NuGet package metadata with GPT models. Awaiting release publication. — decided by Scribe
