# Decisions

> Shared decision log for the ElBruno.Text2Image team. All agents read this before starting work.

<!-- Scribe merges decisions from .squad/decisions/inbox/ into this file. Do not edit directly — use the inbox. -->

### Decision: MAI-Image-2 Cloud API Support

**Author:** Kaylee (Core Dev)  
**Date:** 2026-04-13  
**Status:** Implemented

**Context:** Added support for the MAI-Image-2 image generation model via Azure Foundry, expanding generator options beyond FLUX.2.

**Decision:**
- New `MaiImage2Generator` class in `ElBruno.Text2Image.Foundry`
- Follows established patterns from `Flux2Generator`: `ByteArrayContent` serialization, source-generated JSON context, async polling (202 + retry)
- Registered in `ServiceCollectionExtensions.cs` as `IImageGenerator` with keyed services
- M.E.AI property passthrough via `AdditionalProperties` (keyed as `mai_options` internally)
- Full HTTP-level test coverage in `MaiImage2GeneratorHttpTests.cs` (32 tests)
- Reference sample: `scenario-13-mai-image2-cloud`

**Implications:**
- Consumers can inject `IImageGenerator` or key-select `MaiImage2Generator` directly
- Future model additions should use the same pattern: new generator class, serialization via source-gen context, M.E.AI integration
- Build: 0 warnings, 0 errors. Tests: 324 passing (net8.0 + net10.0)
- Branch: `feature/mai-image-2-support`

### Decision: CLI uses Spectre.Console.Cli

**Date:** 2026-04-19  
**Author:** Mal (Lead)  
**Status:** Implemented  
**Context:** CLI tool implementation

**Problem:** The initial plan specified `System.CommandLine` (preview 2.0) for command parsing. However, `System.CommandLine` has been in preview for years with no stable release, and its API surface has changed significantly between versions.

**Decision:** Pivot to `Spectre.Console.Cli` for command parsing and argument handling.

**Rationale:**
1. **Stability** — `Spectre.Console.Cli` is stable (v0.49.x) with a locked-down API contract
2. **Integration** — Native integration with `Spectre.Console` for TUI rendering (tables, prompts, progress bars)
3. **DI Support** — First-class support for dependency injection via `ITypeRegistrar`/`ITypeResolver` adapters
4. **Proven** — Widely used in production .NET CLI tools (Cake, Azure tools, etc.)
5. **Documentation** — Comprehensive docs and examples

**Alternatives Considered:**
- System.CommandLine (rejected: preview status, API instability)
- CommandLineParser (rejected: attribute-based design incompatible with Spectre fluent model)
- Raw args parsing (rejected: maintenance burden, no help generation)

**Implementation Details:**
- Spectre.Console.Cli v0.49.1 referenced in `.csproj`
- `TypeRegistrar` and `TypeResolver` adapters bridge `IServiceCollection` and Spectre's DI
- Commands inherit from `AsyncCommand<TSettings>` or `Command<TSettings>`
- Settings use `[CommandArgument]` and `[CommandOption]` attributes
- Program.cs uses `CommandApp` with fluent configuration

**Impact:**
- Breaking from plan.md: Yes, but justified by stability concerns
- Public API: None — CLI is an end-user tool
- Build: No impact
- Tests: Future command tests use Spectre test helpers

### Decision: CLI targets net10.0 only (single TFM)

**Date:** 2026-04-19  
**Author:** Mal (Lead)  
**Status:** Implemented  
**Context:** CLI tool packaging

**Problem:** Should the CLI tool follow the library's multi-targeting strategy (net8.0;net10.0)?

**Decision:** CLI targets **net10.0 only** (single TFM).

**Rationale:**
1. **Tool packaging** — dotnet tools install to the .NET SDK tool directory, running under the installed SDK version. Multi-targeting is unnecessary.
2. **RollForward** — `<RollForward>LatestMajor</RollForward>` allows the tool to run on .NET 10+ SDKs without recompilation.
3. **Simpler builds** — Single TFM = smaller package, faster restore/build cycles
4. **Latest features** — net10.0 brings performance improvements and new APIs
5. **Reduced test matrix** — One runtime to test for CLI scenarios

**Alternatives Considered:**
- Multi-target net8.0;net10.0 (rejected: complexity with no user benefit)
- Target net8.0 only (rejected: missing net10 optimizations)

**Implementation Details:**
- `.csproj` specifies `<TargetFramework>net10.0</TargetFramework>` (singular)
- `<RollForward>LatestMajor</RollForward>` ensures forward compatibility
- Libraries remain multi-targeted for library consumers on net8.0

**Impact:**
- User requirements: .NET 10 SDK installed (or later)
- CI/CD: Publish workflows need .NET 10 SDK
- Libraries: Unaffected — remain multi-targeted

### Decision: CLI secret store interface design

**Date:** 2026-04-19  
**Author:** Mal (Lead)  
**Status:** Implemented  
**Context:** Secrets management architecture

**Problem:** The CLI needs to store and retrieve cloud provider credentials securely across multiple platforms (Windows, Linux, macOS). Different platforms have different native secret storage mechanisms, and users may want fallback options.

**Decision:** Define an `ISecretStore` abstraction with multiple implementations:

```csharp
public interface ISecretStore
{
    string Name { get; }
    bool IsAvailable { get; }
    Task<string?> GetAsync(string provider, string field, CancellationToken ct);
    Task SetAsync(string provider, string field, string value, CancellationToken ct);
    Task DeleteAsync(string provider, string field, CancellationToken ct);
    Task<IReadOnlyList<string>> ListFieldsAsync(string provider, CancellationToken ct);
}
```

Implementations:
- `EnvVarSecretStore`: Read-only, reads from `T2I_<PROVIDER>_<FIELD>` environment variables
- `DpapiSecretStore`: Windows-only, uses DPAPI for encrypted storage
- `PlainFileSecretStore`: Cross-platform plaintext JSON (opt-in, with warning)

**Rationale:**
1. **Abstraction** — Easy addition of new backends without changing consumer code
2. **Platform awareness** — `IsAvailable` lets CLI skip unsupported stores
3. **Hierarchical resolution** — `SecretResolver` chains stores in priority order
4. **Testability** — Stores can be mocked for unit tests
5. **Future-proof** — New stores (Keychain, libsecret) can be added without breaking changes

**Key Design Choices:**
- Namespace by provider AND field (allows multiple secrets per provider)
- Async API (supports future I/O-heavy backends)
- Nullable returns (distinguish "not found" from "empty string")
- Read-only env store (env vars don't persist across sessions)

**Security Model:**
- Windows: DPAPI per-user encryption, stored in `%APPDATA%\t2i\secrets`
- Linux/macOS: Plaintext fallback in `~/.config/t2i/secrets.json` with 0600 permissions
- Future: Keychain (macOS), libsecret (Linux), Azure Key Vault (enterprise)

**Impact:**
- Wash implements all three stores and `SecretResolver`
- Kaylee's commands use `SecretResolver` for transparent secret fetching
- Tests mock `ISecretStore` for deterministic testing
- User guide explains resolution chain

### Decision: Secret Storage Format and Naming Conventions

**Author:** Wash (Backend Dev)  
**Date:** 2026-04-19  
**Status:** Implemented  
**Branch:** feature/cli-tool-t2i

**Context:** The CLI tool needs to store provider API keys and endpoints securely across platforms. Resolution chain: env vars → DPAPI (Windows) → plaintext file (fallback).

**Environment Variable Convention:**
Format: `T2I_{PROVIDER_UPPER_UNDERSCORED}_{FIELD_UPPER}`

Examples:
- `foundry-flux2` provider, `apiKey` field → `T2I_FOUNDRY_FLUX2_APIKEY`
- `foundry-mai2` provider, `endpoint` field → `T2I_FOUNDRY_MAI2_ENDPOINT`

Normalization: `-` → `_`, then uppercase. Round-trip: `_` → `-`, lowercase when listing fields.

**DPAPI Store (Windows Only):**
- File: `%LOCALAPPDATA%\t2i\secrets.dpapi`
- Format: JSON `Dictionary<string, byte[]>` keyed by `"{provider}::{field}"`
- Encryption: `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`
- Concurrency: `SemaphoreSlim` serializes file access; atomic writes via `.tmp` + `File.Replace`

**PlainFile Store (Cross-Platform):**
- File: `{ConfigDir}/secrets.json` (Windows: `%APPDATA%\t2i`, Unix: `~/.config/t2i`)
- Format: JSON `Dictionary<string, string>` keyed by `"{provider}::{field}"`
- Security: One-time warning; Unix 0600 permissions; atomic writes

**Resolution Chain:**
1. CLI overrides (e.g., `--api-key` flag)
2. EnvVarSecretStore
3. DpapiSecretStore (Windows only)
4. PlainFileSecretStore
5. null (secret not found)

**SetAsync Behavior:** Prefer DPAPI if available; fall back to plaintext file.

**DeleteAsync Behavior:** Delete from all available stores to ensure complete removal.

**Source-Generated JSON:** All serialization uses source-gen contexts (AOT-friendly).

**Implications:**
- Commands use `SecretResolver.ResolveAsync` for transparent secret fetching
- Tests verify env var normalization, DPAPI format, file permissions, resolution chain
- Provider adapters call `SecretResolver` without knowing backend details

### Decision: CLI Command Surface Structure

**Author:** Kaylee (Core Dev)  
**Date:** 2026-04-19  
**Status:** Implemented

**Context:** The CLI requires a user-facing command structure balancing simplicity (quick generation) with power (configuration, diagnostics).

**Decision:** Implemented command tree using Spectre.Console.Cli:

```
t2i "<prompt>" [options]             # GenerateCommand (default)
t2i config [action] [args]           # ConfigCommand
t2i secrets <action> [provider]      # SecretsCommand
t2i doctor                           # DoctorCommand
t2i providers                        # ProvidersCommand
t2i version                          # VersionCommand
```

**Key Details:**
1. **Default command** — `GenerateCommand` via `CommandApp<GenerateCommand>`. Users run `t2i "a cat"` directly.
2. **Config subcommands:**
   - `t2i config` → interactive wizard
   - `t2i config show` → table of current config
   - `t2i config set <provider>.<field> <value>` → set config
   - `t2i config remove <provider>` → remove provider
   - `t2i config path` → print config file path
3. **Secrets subcommands:**
   - `t2i secrets set <provider>` → interactive secret entry
   - `t2i secrets list` → table of configured secrets
   - `t2i secrets remove <provider>` → delete secrets
   - `t2i secrets test <provider>` → health check
4. **Standalone commands** — `doctor`, `providers`, `version` with no subcommands

**Deviations from Spec:** Uses argument-based routing instead of Spectre branches (simpler, avoids multiple command classes).

**Implications:**
- All commands consume `ProviderRegistry`, `SecretResolver`, `ConfigStore` from DI
- Commands handle both interactive (TTY) and non-interactive (CI/CD) scenarios
- Exit codes: 0=success, 1=failure, 2=missing config/secrets
- Easily extensible: new commands add a single line to Program.cs

### Decision: Provider Adapter Default Parameters

**Date:** 2026-04-19  
**Author:** River (AI/ML Specialist)  
**Status:** Implemented  
**Context:** CLI provider adapters need sensible defaults for dimensions, steps, and models.

**Defaults Per Provider:**

**Local Providers (CPU/CUDA/DirectML):**
- Model: Stable Diffusion 1.5
- Dimensions: 512×512 (optimal for SD 1.5, multiple of 8)
- Steps: 20 (balance quality vs speed)

Rationale: SD 1.5 lightweight, well-tested, runs on any hardware. 512×512 is native training resolution. 20 steps acceptable in <30s.

**Foundry FLUX.2 (Cloud):**
- Model: FLUX.2-pro
- Dimensions: 512×512 (keeps costs low for initial tests; supports up to 2048×2048)
- Steps: 20 (FLUX.2 supports 1-50)
- Endpoint rewrite: `.openai.azure.com` → `.services.ai.azure.com`

**Foundry MAI-Image-2 (Cloud):**
- Model: mai-image-2
- Dimensions: 1024×1024 (MAI requires min 768px, max 1M total pixels)
- Steps: N/A (MAI API doesn't expose steps)
- Endpoint rewrite: `.openai.azure.com` → `.services.ai.azure.com`

Rationale: 1024×1024 is MAI's sweet spot for quality.

**Validation:**
- Local providers: No validation (setters enforce 128-2048 range, multiple-of-8)
- FLUX.2: No validation beyond ImageGenerationOptions constraints
- MAI-Image-2: Explicit validation (min 768px, max 1M pixels)

**Implications:**
- Users override defaults via `--width`, `--height`, `--steps` flags
- Invalid dimensions caught at adapter or generator level
- First-run model download for local providers automatic but slow (GB-scale)

### Decision: CLI Infrastructure Test Coverage

**Date:** 2026-04-19  
**Author:** Jayne (Tester)  
**Status:** Complete  
**Branch:** feature/cli-tool-t2i

**Summary:** Added 50 passing tests covering CLI secret stores, resolver, config persistence, and console helpers. All tests run on net10.0 only.

**Coverage Details:**

**Covered:**
- EnvVarSecretStore: naming, read-only validation, field listing
- DpapiSecretStore: round-trip encryption (Windows), platform check, field listing
- PlainFileSecretStore: JSON persistence, atomic writes, Unix 0600 permissions
- SecretResolver: chain, CLI override priority, store selection, InspectAsync
- ConfigStore: round-trip serialization, missing file handling, atomic writes
- ConsoleHelpers: Slug() (unicode, truncation), Mask() (prefix+suffix), IsInteractive() (TTY)
- Command surface: Smoke tests scaffolded for future end-to-end

**Not Covered:**
- Interactive wizard flows (requires TTY simulation)
- Provider adapters (River's responsibility)
- Command execution (integration tests pending)
- Error edge cases (corrupted files, permission denied)
- Concurrent access (relies on SemaphoreSlim)
- DPAPI error modes

**Test Strategy:**
- Platform-specific tests via `if (OperatingSystem.IsWindows())` or `RuntimeInformation.IsOSPlatform()`
- Isolation via unique temp directories per test
- `[Collection]` attribute prevents parallel execution for file-sharing tests
- Fakes preferred over mocks for clarity

**Metrics:**
- Total: 50 tests passed (100% pass rate on net10.0)
- Coverage: SecretResolver 7/7, EnvVarSecretStore 4/4, DpapiSecretStore 4/4, PlainFileSecretStore 4/4, ConfigStore 2/2, ConsoleHelpers 3/5

### Bug Resolution: SecretResolver.DeleteAsync Missing

**Date:** 2026-04-19  
**Found by:** Jayne (Tester)  
**Responsible:** Wash (Backend Dev)  
**Status:** ✅ Resolved

**Issue:** `SecretResolver` was missing a `DeleteAsync` method but commands were calling it.

**Impact:** Compilation errors, test blocking, affects `secrets remove` and `config remove`.

**Expected Behavior:** Delete from all available stores for complete removal.

**Resolution:** Wash added `DeleteAsync` method. Coordinator added `[SupportedOSPlatform("windows")]` to DpapiSecretStore.DeleteAsync and ListFieldsAsync.

### Decision: `t2i init` Command for AI Agent Skill Discovery

**Date:** 2026-04-20  
**Author:** Kaylee (Core Dev)  
**Status:** Implemented  
**Branch:** feature/cli-tool-t2i

## Context

AI coding agents (GitHub Copilot, Claude Code, and MCP-based assistants) can discover repository-specific skills via standardized skill file locations. The `t2i init` command mirrors [Aspire's `aspire agent init` pattern](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/agent-integration) to auto-configure repos with skill documentation.

## Decision

Implemented `InitCommand` at `src\ElBruno.Text2Image.Cli\Commands\InitCommand.cs`:

**Command Signature:**
```bash
t2i init [--target <github|claude|all>] [--force]
```

**Behavior:**
- Default (`--target all`): Writes SKILL.md to **both** `.github/skills/t2i/SKILL.md` and `.claude/skills/t2i/SKILL.md`
- `--target github`: Writes only `.github/skills/t2i/SKILL.md`
- `--target claude`: Writes only `.claude/skills/t2i/SKILL.md`
- `--force`: Overwrites existing files; without it, skips existing files with status message

**Resource Embedding:**
- Canonical skill content lives in `src\ElBruno.Text2Image.Cli\Skills\SKILL.md`
- Embedded via `<EmbeddedResource Include="Skills\SKILL.md" LogicalName="ElBruno.Text2Image.Cli.Skills.SKILL.md" />` in `.csproj`
- Loaded at runtime via `Assembly.GetManifestResourceStream("ElBruno.Text2Image.Cli.Skills.SKILL.md")`

**Output Style:**
- Per-target status line: `✓ <path> created` (green) / `→ <path> updated` (yellow) / `• <path> skipped` (dim)
- Summary panel: counts created/updated/skipped files, suggests `--force` if any skipped

## Rationale

1. **Discoverability:** Agents can auto-load skill files from `.github/skills/` (GitHub Copilot) and `.claude/skills/` (Claude Code) directories without manual configuration.
2. **Single source of truth:** Embedding SKILL.md in the binary ensures version consistency — users always get the skill file matching their installed CLI version.
3. **Multi-target support:** Writing to both directories ensures compatibility with all major AI coding agents in one command.
4. **Non-destructive by default:** Skipping existing files prevents accidental overwrites of user-customized skill files; `--force` is opt-in.

## Alternatives Considered

- **External URL reference only:** Rejected — requires internet access, no offline support, version mismatch risk.
- **dotnet new template:** Rejected — overkill for a single file, requires separate template package.
- **Copy from installation directory:** Rejected — harder to discover path, breaks in global tool installs.

## Implementation Details

**Embedded Resource Pattern:**
```xml
<EmbeddedResource Include="Skills\SKILL.md" LogicalName="ElBruno.Text2Image.Cli.Skills.SKILL.md" />
```

**Runtime Loading:**
```csharp
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("ElBruno.Text2Image.Cli.Skills.SKILL.md");
using var reader = new StreamReader(stream);
return reader.ReadToEnd();
```

**Registration in Program.cs:**
```csharp
config.AddCommand<InitCommand>("init")
    .WithDescription("Initialize the current folder with a t2i skill file for AI coding agents")
    .WithExample(new[] { "init" })
    .WithExample(new[] { "init", "--target", "github" })
    .WithExample(new[] { "init", "--force" });
```

## Testing

**Smoke tests verified:**
1. ✅ Default `t2i init` creates both `.github/` and `.claude/` skill files
2. ✅ `--target github` creates only `.github/skills/t2i/SKILL.md`
3. ✅ `--target claude` creates only `.claude/skills/t2i/SKILL.md`
4. ✅ Re-running without `--force` skips existing files with summary message
5. ✅ `--force` updates existing files
6. ✅ File content matches embedded SKILL.md (validates resource loading)

**Build verification:**
- `dotnet build src\ElBruno.Text2Image.Cli\ElBruno.Text2Image.Cli.csproj` — 0 warnings, 0 errors
- `dotnet build ElBruno.Text2Image.slnx --no-restore` — 0 warnings, 0 errors

## Impact

- **Users:** One-command setup for agent-aware repos: `t2i init` → agents auto-discover t2i usage patterns
- **Agents:** Access to comprehensive skill documentation without manual configuration
- **Maintenance:** Skill content updates ship with CLI — no separate docs deployment
- **Future work:** Mal is authoring the canonical SKILL.md content (already completed); this plumbing was implemented separately to unblock parallel work

## References

- Aspire agent init pattern: [Microsoft Learn Docs](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/agent-integration)
- GitHub Copilot skill discovery: `.github/skills/` directory convention
- Claude Code skill discovery: `.claude/skills/` directory convention

### Decision: CLI Lite/Full Edition Split

**Date:** 2026-04-20  
**Author:** Mal (Lead)  
**Status:** Implemented  
**Branch:** feature/cli-tool-t2i  
**Commit:** 74d151b

## Context

The initial CLI implementation included all providers: local (CPU, CUDA, DirectML) and cloud (Foundry FLUX.2, MAI-Image-2). This resulted in a large package size (~150-300 MB) due to ONNX Runtime native libraries bundled with the local providers.

User request: Ship a **Lite edition first** with cloud providers only to keep the package small and fast to install.

## Decision

Ship two editions of the CLI tool:

### Lite Edition (v0.1.0)
- **Package ID:** `ElBruno.Text2Image.Cli`
- **Providers:** Foundry FLUX.2, MAI-Image-2 (cloud only)
- **Size:** ~2.4 MB NuGet package
- **Target users:** Cloud-first workflows, CI/CD, containers, users without GPU

### Full Edition (planned v0.2.0)
- **Package ID:** `ElBruno.Text2Image.Cli.Full` (TBD)

### Decision: Configurable Model Name for Cloud Providers

**Date:** 2026-04-20  
**Author:** Mal (Lead)  
**Status:** Implemented  
**Branch:** `feat/configurable-model-name`  
**Commit:** 72a00dc (PR #13, merged to main)

#### Context

The CLI currently hardcodes model names in provider adapters:
- `FoundryMaiImage2Adapter` → `"MAI-Image-2"` / `"mai-image-2"`
- `FoundryFlux2Adapter` → `"FLUX.2-pro"`

New models are available (e.g., `MAI-Image-2e`), but users cannot configure them without code changes. The library already supports custom model names via constructor parameters — the CLI just isn't using this capability.

#### Decision: Config-Stored Model Names

- Model names stored in `ProviderConfig.Model` (plain text, not secret)
- Adapter defaults: MAI → "MAI-Image-2", FLUX → "FLUX.2-pro"
- Setup wizard prompts for model with defaults shown, blank input accepts default
- Backward compatible: null config defaults to adapter fallback
- Version bump: 0.9.1 → 0.10.0 (minor — new feature, backward compatible)
- No client-side validation (let API return errors)

#### Masking Policy

- `apiKey` → masked (`sk-XXX***...***abcd`)
- `endpoint` → plain text (URL is not sensitive)
- `model` → plain text (deployment name is not sensitive)

#### Related Decisions

- **RequiredFields/RequiredSecrets split:** Separated non-sensitive config (endpoint, model) from sensitive secrets (apiKey)
- **Tests:** 21 new tests verifying HTTP serialization, config persistence, and masking policy

#### Impact

- Users can run `t2i config set foundry-mai2.model MAI-Image-2e`
- `t2i config show` displays model in plain text
- Setup wizard explicitly prompts for model choice
- All existing configs continue working (backward compatible)

### Decision: Split RequiredSecrets and RequiredFields in IProviderAdapter

**Date:** 2026-04-20  
**Author:** Kaylee (Core Dev)  
**Status:** Implemented  
**Branch:** feat/configurable-model-name  
**Commit:** 72a00dc

#### Problem

The original `IProviderAdapter.RequiredSecrets` mixed sensitive (apiKey) and non-sensitive (endpoint, model) configuration. This caused masking problems (`t2i config show` masked both) and prevented model configuration without code changes.

#### Decision

Split into two properties:
- **`RequiredSecrets`:** Sensitive data only (apiKey) — masked display, secret store
- **`RequiredFields`:** Non-sensitive config (endpoint, model) — plain text display, `ProviderConfig` storage

For Foundry providers:
- `RequiredSecrets => ["apiKey"]`
- `RequiredFields => ["endpoint", "model"]`

#### Storage & Display

| Field | Store | Display |
|-------|-------|---------|
| apiKey | SecretResolver | Masked |
| endpoint | ProviderConfig | Plain |
| model | ProviderConfig | Plain |

#### Backward Compatibility

Adapters check `ProviderConfig.Endpoint` first, then fall back to `SecretResolver` (existing users who stored endpoint as secret). Model defaults to null in config, adapters apply runtime defaults.

#### Implementation

- `IProviderAdapter.cs`: Added `RequiredFields` default property
- Both Foundry adapters: Split properties, read model from config
- `ConfigCommand`, `SetupWizard`, `DoctorCommand`: Updated to handle split properties
- `SetupWizard`: Prompts for RequiredFields after RequiredSecrets with provider-specific defaults

### Decision: Test Coverage for Configurable Model Names

**Date:** 2026-04-20  
**Author:** Jayne (Tester)  
**Status:** Implemented  
**Branch:** feat/configurable-model-name  
**Commit:** 72a00dc (part of PR #13)

#### Summary

Added 21 tests covering configurable model names. All pass on both net8.0 and net10.0.

#### Coverage

| Area | Tests | Details |
|------|-------|---------|
| HTTP serialization | 4 | Custom + default model in request bodies (MAI, FLUX) |
| Config persistence | 5 | Round-trip, multi-provider, null defaults |
| Masking policy | 10 | RequiredFields vs RequiredSecrets split enforcement |
| Backward compat | 1 | Null model falls back to adapter default |
| Multi-provider | 1 | Different models per provider persist independently |

#### Test Results

- **Total:** 240 (net10.0), 166 (net8.0)
- **Passed:** 238 (net10.0), 166 (net8.0)
- **Failed:** 0
- **Skipped:** 2 (existing smoke tests)

#### Key Patterns

- **FakeHttpHandler:** Captures HTTP requests for JSON body verification
- **Temp directory isolation:** Prevents ConfigStore file lock conflicts
- **ServiceCollection DI:** Tests construct adapters with proper dependencies

#### Files Added

- `src/ElBruno.Text2Image.Tests/Cli/ConfigModelTests.cs` (5 tests)
- `src/ElBruno.Text2Image.Tests/Cli/ConfigDisplayTests.cs` (10 tests)

#### Files Modified

- `src/ElBruno.Text2Image.Tests/MaiImage2GeneratorHttpTests.cs` (added class with 2 tests)
- `src/ElBruno.Text2Image.Tests/Flux2GeneratorHttpTests.cs` (added class with 2 tests)
- **Providers:** All providers (CPU, CUDA, DirectML, Foundry FLUX.2, MAI-Image-2)
- **Size:** ~200 MB NuGet package (includes ONNX Runtime native libs)
- **Target users:** Users with local GPU, offline workflows

## Implementation

### Lite Refactor (v0.1.0)
1. Removed `ProjectReference` to `ElBruno.Text2Image.Cpu`, `.Cuda`, `.DirectML` from CLI csproj
2. Deleted `LocalCpuAdapter.cs`, `LocalCudaAdapter.cs`, `LocalDirectMlAdapter.cs`
3. Removed local adapter DI registrations from `ProviderServiceCollectionExtensions`
4. Updated `GenerateCommand` documentation to omit local provider IDs
5. Updated `docs/cli-tool.md` with "Editions" callout
6. Set `Version=0.1.0` in csproj
7. Added per-package README (`src/ElBruno.Text2Image.Cli/README.md`)
8. Updated `PackageDescription` to mention Lite/Full split

### Build Verification
- **Build:** 0 warnings, 0 errors
- **Tests:** 375 total, 373 passing, 2 skipped, 0 failed
- **Package size:** 2.43 MB (vs ~200 MB with local providers)

## Rationale

1. **User choice:** Cloud-first users don't need 200 MB of ONNX Runtime native libraries
2. **Installation speed:** 2.4 MB downloads in seconds vs minutes
3. **Container-friendly:** Smaller Docker image layers
4. **CI/CD-friendly:** Faster `dotnet tool install` in build pipelines
5. **Gradual adoption:** Users can try the Lite edition first, upgrade to Full if needed

## Trade-offs

**Pros:**
- Small package size
- Fast installation
- Cloud-first experience
- No local GPU/CPU dependencies

**Cons:**
- Requires internet for image generation (cloud APIs)
- Monthly costs for cloud API usage
- No offline mode

## Future Work

1. **v0.2.0:** Ship Full edition as `ElBruno.Text2Image.Cli.Full`
2. **v0.3.0:** Add auto-detection in install script to suggest Lite vs Full based on GPU presence
3. **v0.4.0:** Consider a "hybrid" mode: cloud by default, local fallback if offline

## Alternatives Considered

1. **Single package with optional dependencies:**
   - Rejected: NuGet doesn't support conditional native dependencies
2. **Separate `--lite` flag during install:**
   - Rejected: `dotnet tool install` doesn't support flags
3. **Download local providers on-demand:**
   - Rejected: Breaks offline use, complicates dependency resolution

## Impact

- **Users:** Can choose between Lite and Full based on use case
- **Build:** Single workflow for Lite (v0.1.0), Full added later
- **Tests:** All tests pass with cloud-only providers
- **Docs:** Updated with edition comparison table

## References

- Commit: 74d151b
- Package: `ElBruno.Text2Image.Cli` v0.1.0
- Docs: `docs/cli-tool.md`

### Decision: CLI Tag Prefix Scheme (`cli-v*`)

**Date:** 2026-04-20  
**Author:** Mal (Lead)  
**Status:** Implemented  
**Branch:** feature/cli-tool-t2i  
**Commit:** 74d151b

## Context

The ElBruno.Text2Image repository contains two categories of packages:
1. **Libraries:** `ElBruno.Text2Image`, `ElBruno.Text2Image.Foundry`, `ElBruno.Text2Image.Cpu`, etc.
2. **CLI tool:** `ElBruno.Text2Image.Cli`

Both need independent versioning and release workflows. Library releases use tags like `v1.0.0`, `v2.0.0`. We need a distinct tag scheme for CLI releases to avoid workflow conflicts.

## Decision

Use **`cli-v*` tag prefix** for CLI releases:
- Library releases: `v1.0.0`, `v2.0.0`, `v3.0.0`
- CLI releases: `cli-v0.1.0`, `cli-v0.2.0`, `cli-v1.0.0`

## Implementation

### Workflow Guards
1. **`publish.yml` (library workflow):** Skip on `cli-v*` tags
   ```yaml
   jobs:
     publish:
       if: github.event_name == 'workflow_dispatch' || !startsWith(github.event.release.tag_name, 'cli-')
   ```
2. **`publish-cli.yml` (CLI workflow):** Fire only on `cli-v*` tags
   ```yaml
   jobs:
     determine-version:
       if: github.event_name == 'workflow_dispatch' || startsWith(github.event.release.tag_name, 'cli-')
   ```

### Version Normalization
The `publish-cli.yml` workflow strips prefixes to extract semver:
```bash
TAG="${{ github.event.release.tag_name }}"
# cli-v0.1.0 -> 0.1.0
VERSION="${TAG#cli-v}"
VERSION="${VERSION#cli-}"
VERSION="${VERSION#v}"
```

Handles all valid formats:
- `cli-v0.1.0` → `0.1.0`
- `cli-0.1.0` → `0.1.0`
- `v0.1.0` → `0.1.0` (fallback)

## Rationale

1. **Clarity:** `cli-v*` tags are self-documenting
2. **Workflow isolation:** Prevents library workflow from firing on CLI releases
3. **Independent versioning:** CLI can version separately from libraries (e.g., CLI at v0.1.0, libraries at v2.3.0)
4. **GitHub Releases:** Tags appear as "cli-v0.1.0" in the releases page, easy to filter

## Examples

| Release | Tag | Workflow | Outcome |
|---------|-----|----------|---------|
| Library v2.3.0 | `v2.3.0` | `publish.yml` | Publishes 5 library packages to NuGet |
| CLI v0.1.0 | `cli-v0.1.0` | `publish-cli.yml` | Publishes CLI NuGet + binaries to GitHub Release |
| CLI v0.2.0 | `cli-v0.2.0` | `publish-cli.yml` | Same as above |
| Library v2.4.0 | `v2.4.0` | `publish.yml` | Library workflow only |

## Alternatives Considered

1. **Separate repositories:**
   - Rejected: Adds maintenance overhead, splits issues/PRs
2. **Same tag, different workflows:**
   - Rejected: No way to distinguish which workflow to fire
3. **Tag suffix (`v0.1.0-cli`):**
   - Rejected: Violates semver (suffix means prerelease)
4. **Manual workflow dispatch only:**
   - Rejected: Breaks automation, requires manual triggering

## Future Enhancements

1. **v0.2.0+:** Add winget auto-submission via `vedantmgoyal9/winget-releaser@v2` triggered by `cli-v*` tags
2. **v0.3.0+:** Add Homebrew formula auto-update (similar pattern, `cli-v*` trigger)

## Impact

- **CI/CD:** Two independent release pipelines
- **GitHub Releases:** Clear separation between library and CLI releases
- **Users:** Easy to identify which release is which
- **Versioning:** CLI and libraries can evolve independently

## References

- Commit: 74d151b
- Workflows: `.github/workflows/publish.yml`, `.github/workflows/publish-cli.yml`
- First CLI release: `cli-v0.1.0`

### Decision: Secret Storage Security — Blog Post Content Ordering

**Date:** 2026-04-20  
**Author:** Mal (Lead)  
**Status:** Recommendation (pending Bruno's approval)  
**Context:** Bruno raised security concerns about the blog post over-promoting environment variables without adequate explanation of trade-offs.

## The Question

Bruno asked whether storing API keys in environment variables is secure. The blog post presents env vars as "best for CI/CD" without adequately explaining security risks to local developers.

## Current State

The blog post (`docs/20260420-introducing-t2i-cli.md`) presents secrets storage in this order:
1. **Environment Variables** — "Best for CI/CD" (presented first)
2. **Windows DPAPI** — "Encrypted on Windows"
3. **Plaintext File Fallback** — "Cross-Platform"
4. **CLI Override** — "One-Off Tests"

This ordering suggests env vars are the recommended default, which is **misleading for local development**.

## The Reality: Environment Variable Security

### Real Risks of Environment Variables

1. **Process Tree Visibility**
   - On Linux: Any user can read `/proc/<pid>/environ` for processes they own
   - On Windows: PowerShell `Get-Process | Select-Object -ExpandProperty Environment` exposes them
   - Child processes inherit the entire environment (shell scripts, build tools, etc.)

2. **Accidental Leakage**
   - Debug output (`env`, `printenv`, `set`) exposes all vars
   - Error messages and stack traces may dump environment context
   - CI/CD logs often echo environment for debugging (GitHub Actions' `set -x` mode)
   - Docker: `docker inspect <container>` reveals ENV vars in plain text
   - Docker layer leakage: `ENV` directives in Dockerfile bake secrets into image layers

3. **Shell History**
   - `export T2I_API_KEY="secret"` goes straight into `.bash_history` / `.zsh_history`
   - PowerShell history in `ConsoleHost_history.txt`

4. **Persistence Confusion**
   - Users often add env vars to `.bashrc`, `.zshrc`, `.profile` — committed to dotfiles repos
   - Windows: System-wide env vars via Control Panel are visible to all user processes

### When Environment Variables ARE Appropriate

- **CI/CD pipelines** where:
  - Secrets are injected from a secure vault (GitHub Secrets, Azure Key Vault)
  - Process lifetime is ephemeral (minutes, not hours/days)
  - Environment is isolated (container, fresh VM)
  - Logging is controlled and redacts secrets automatically

- **Server/container deployments** where:
  - 12-factor app pattern is the norm
  - Process runs as a dedicated service account
  - No interactive shell access to the process owner
  - Environment injection happens via orchestrator (Kubernetes secrets, Docker secrets)

### When Environment Variables ARE NOT Appropriate

- **Local developer machines**
  - Long-running shell sessions (env vars persist for hours/days)
  - Developers run many tools that inherit environment
  - Risk of accidental logging, history leakage, dotfile commits

## What the CLI Already Supports

From `SecretResolver.cs`, the resolution chain prioritizes secure storage:

```csharp
// Resolution order:
// 1. CLI flags (--api-key, highest priority, ephemeral)
// 2. Environment variables (T2I_<PROVIDER>_<FIELD>)
// 3. DPAPI store (Windows only, encrypted via DataProtectionScope.CurrentUser)
// 4. Plaintext file (~/.t2i/secrets.json with 0600 permissions on Unix)
```

**Most Secure Option (Local Development):**
- **Windows:** DPAPI (`DpapiSecretStore`) — encrypted with user's Windows credentials, protected by OS
- **macOS/Linux:** Plaintext file with `0600` permissions (user-only read/write)

Note: The CLI does NOT currently support macOS Keychain or Linux libsecret, but DPAPI on Windows is production-grade encryption.

## Recommended Fix: Reorder Blog Post Sections

### Proposed Structure for "Where Do My Secrets Live?" Section

**Reorder to:**
1. **Local Development** — OS-Native Encrypted Storage (DPAPI/file) — FIRST
2. **CI/CD Pipelines** — Environment Variables — SECOND (with explicit warnings)
3. **One-Off Tests** — CLI Override — THIRD
4. **How the CLI Resolves Secrets** (Priority Order)
5. **Security Best Practices** (do/don't box)

**Key Changes:**
- Reordered sections to put DPAPI/local storage FIRST as the recommended default
- Explicit warnings: "DO NOT use env vars for local development"
- Security best practices box with clear do/don't list
- Context-specific recommendations: different advice for local dev vs CI/CD

## Rationale

- **Honesty:** Environment variables ARE insecure for local development (process tree, history, dotfile leakage)
- **Clarity:** The blog post currently presents them as "best" without qualifying "best for CI/CD only"
- **User Safety:** Most readers are local developers — they need the DPAPI/file recommendation first
- **Zero Code Changes:** The CLI already does the right thing (DPAPI default on Windows, file fallback elsewhere)

## Decision

**Recommend Bruno approve reordering the blog post** to:
1. Put OS-native storage (DPAPI/file) FIRST as the recommended default for local dev
2. Keep env vars as a CI/CD-specific option with explicit warnings
3. Add a security best practices callout box

If approved, implementation task goes to the team (likely documentation update).

## Implementation Checklist (if approved)

- [ ] Edit `docs/20260420-introducing-t2i-cli.md` (reorder sections, add warnings)
- [ ] Edit `.github/skills/t2i/SKILL.md` (update "Secrets & Security" section)
- [ ] Edit `.claude/skills/t2i/SKILL.md` (update "Secrets & Security" section)
- [ ] Edit `docs/cli-tool.md` (align "Secret Resolution Chain" section)
- [ ] Consider: Add `t2i doctor` check that warns if secrets are ONLY in env vars
- [ ] Update README.md if it has similar language

## Verdict

Environment variables are **not secure for local development** due to process visibility, shell history, and accidental leakage. The blog post should present DPAPI/file storage as the **recommended default**, with env vars clearly scoped to **CI/CD only**.

### Decision: MAI-Image-2 Adapter Auto-Bumps Under-Minimum Dimensions

**Author:** Coordinator (Lightweight Inline)  
**Date:** 2026-04-20  
**Status:** Implemented  

**Problem:** CLI defaults to 512×512 (provider-agnostic). MAI-Image-2 enforces minimum 768px per dimension. Users invoking `t2i "..."` without explicit dimension flags hit: "MAI-Image-2 requires both dimensions to be at least 768px".

**Decision:** `MaiImage2Generator` silently auto-bumps any dimension <768px to 1024px and logs a progress-channel note. No error thrown.

**Rationale:**
1. **Backwards compatible** — Existing generic-provider code works transparently with MAI
2. **User experience** — User gets an image instead of a cryptic error; informed via progress note
3. **Provider consistency** — Each cloud adapter handles its own constraints (Flux2 handles its ranges independently)
4. **No breaking changes** — Explicit dimension specifications bypass the check (if user passes 768+, no modification)

**Implementation:**
- Before API call in `MaiImage2Generator`, check: `if (width < 768) width = 1024; if (height < 768) height = 1024`
- Log to progress channel: `"MAI-Image-2 enforces 768px minimum; auto-bumped from {original} to {adjusted}"`
- No exception; no retry

**Impact:**
- PR #15 merged
- CLI v0.10.1 shipped with publish workflow run 24673091352
- Library version unchanged; only generator logic

### CLI Short Option Aliases Must Not Collide with Spectre.Console Built-ins

**Date:** 2026-04-20  
**Origin:** Coordinator  
**Status:** Merged

#### Decision

CLI short option aliases must not collide with Spectre.Console's reserved flags. The `-h` short alias is reserved globally for `--help` and **may NOT** be reused by domain options like `--height`.

#### Context

User "Bruno Capuano" reported that `t2i -h` failed with:
```
Option 'height' is defined but no value has been provided
```

This occurred because `--height` was assigned the `-h` short alias, which collided with Spectre.Console.Cli's built-in `-h, --help` flag.

#### Action Taken

Removed `-h` short alias from `--height` option in `GenerateCommand.cs`. The `--height` option now has no short alias, and `t2i -h` correctly shows the help text.

#### Future Guidelines

**For all CLI option additions:**
- Reserve `-h` for `--help` (Spectre.Console built-in)
- Check Spectre.Console's full reserved short-flag set before assigning new short aliases
- Document any additional reserved short flags discovered (e.g., `-v` if used for `--version`)
- Prefer long-form flags over collision risk

#### References

- PR #16: `fix/cli-height-shortopt-collision` → main (merged)
- Release: cli-v0.10.2
- GenerateCommand.cs: `--height` option definition
- docs/cli-tool.md: Updated options table

### Blog Post Hero Image & Documentation Reorganization

**Date:** 2026-04-20  
**Authors:** River, Coordinator, Kaylee  
**Status:** Complete (staged for Bruno's review)

#### Decision: Blog Images Convention

Repo-hosted blog images follow a naming convention:
- **Image location:** `images/{YYYYMMDD}-{slug}.png`
- **Companion prompt doc:** `docs/blogs/{YYYYMMDD}-{slug}-image-prompt.md`

Example:
- Image: `images/20260420-introducing-t2i-cli-hero.png` (1280x768, MAI-Image-2e)
- Prompt: `docs/blogs/20260420-introducing-t2i-cli-image-prompt.md`

**Rationale:**
- Centralizes images in single directory for easy asset management
- Companion prompt documents ensure reproducibility and AI skill context
- Date prefix enables chronological sorting and archival strategies

#### Decision: Blog Posts Live Under docs/blogs/

All blog posts are now organized under `docs/blogs/` (separate from technical reference docs in `docs/`).

**Rationale:**
- Improves IA: blogs are content, tech docs are reference
- Enables scalable content organization as blog collection grows
- Clearer separation of concerns

#### Decision: t2i is Canonical Tool for Repo Image Assets

The t2i CLI is the canonical tool for generating and managing image assets within the repo (dogfooding).

**Rationale:**
- Exercises the tool in real scenarios
- Validates usability for external users
- Builds confidence in the product

#### Implementation Notes

**Files Moved:**
- `docs/20260420-introducing-t2i-cli.md` → `docs/blogs/20260420-introducing-t2i-cli.md` (hero image + companion link embedded)
- `docs/blog-post-text2image-dotnet.md` → `docs/blogs/blog-post-text2image-dotnet.md`
- `docs/blog-post-mai-image-2.md` → `docs/blogs/blog-post-mai-image-2.md`

**Link Updates:** 8 relative link corrections across 4 files  
**CHANGELOG:** Updated blog reference path  
**Hero Generation:** Successful via t2i (MAI-Image-2e)

#### Azure Deployment Name Resolution

During hero image generation, River discovered that the t2i config referenced `MAI-Image-2` but the actual deployment name is `MAI-Image-2e` at `bruno-agents-04-resource`.

**Resolution:** Coordinator diagnosed via `az cognitiveservices account deployment list` and reconfigured t2i to use the correct deployment name.

**Note for future work:** Always verify deployment names via `az cognitiveservices account deployment list` when configuring new image generation endpoints.

#### Files Produced

- `images/20260420-introducing-t2i-cli-hero.png` (1280x768)
- `docs/blogs/20260420-introducing-t2i-cli-image-prompt.md`
- `docs/blogs/20260420-introducing-t2i-cli.md` (moved + updated)
- `docs/blogs/blog-post-text2image-dotnet.md` (moved + links fixed)
- `docs/blogs/blog-post-mai-image-2.md` (moved + links fixed)
- `CHANGELOG.md` (reference updated)

**Status:** Staged for Bruno's review. Not committed.

### Decision: Release Versioning Decision — Multi-Model Support

**Decision Date:** 2026-04-21  
**Lead:** Mal  
**Status:** APPROVED

#### Context

The ElBruno.Text2Image project now has **four production-ready AI image generation models** fully integrated with comprehensive test coverage (668 total tests: 298 passed on net8.0, 370 passed on net10.0).

#### Models Integrated

1. **GPT-Image-1.5** (DALL-E 3 via Azure OpenAI) — Latest support in feature/gpt-image-2-support branch
2. **GPT-Image-2** (Unreleased, in feature/gpt-image-2-support branch) — Microsoft's latest image generation model
3. **FLUX.2** (Foundry) — Existing, stable support
4. **MAI-Image-2** (Foundry) — Existing, stable support

#### Recent Changes Since Last Release

- **foundry-v0.10.0 / cli-v0.11.0** (tag: `b75eec4`)
  - Added GPT-Image-1.5 support
  - Configurable model variants

- **Current HEAD** (2 commits ahead):
  - Added **GPT-Image-2** support to Foundry and CLI
  - Fixes for GPT-Image-1.5 API parameter and size compatibility

#### Version Increment Decision

##### Semantic Versioning Analysis

**Foundry Library** (currently `0.10.0`):
- GPT-Image-2 is a **new feature** (not a breaking change)
- No APIs removed or incompatibly changed
- All existing models continue to work unchanged
- **Decision: Bump to `0.11.0`** (minor version increment)

**CLI Tool** (currently `0.11.0`):
- GPT-Image-2 CLI support is a **new feature**
- No breaking changes to CLI commands or arguments
- All existing commands work unchanged
- **Decision: Bump to `0.12.0`** (minor version increment)

#### Rationale

✓ **Feature Addition** — Adding GPT-Image-2 is additive; no removals or breaking changes  
✓ **Test Coverage** — All 668 tests passing across net8.0 and net10.0  
✓ **Production Ready** — Models have samples, CLI integration, and comprehensive documentation  
✓ **No Deprecations** — Existing models remain fully supported  
✓ **API Compatibility** — No changes to public interfaces  

#### Git Tags

```bash
foundry-v0.11.0     # ElBruno.Text2Image.Foundry library
cli-v0.12.0         # ElBruno.Text2Image.Cli tool
```

#### Release Timeline

- Tags created: April 21, 2026
- GitHub releases: Post-tag creation
- NuGet packages: Auto-published via CI/CD

#### Breaking Changes

None.

#### Deprecations

None.

### Decision: River's Blog Post Approach: Multi-Model Announcement

**Date:** 2025-04-21  
**Author:** River (AI/ML Specialist)  
**Status:** Delivered

#### Decision: Comprehensive, Scannable Long-Form Blog

##### What Was Built

A **~2,400 word blog post** announcing unified multi-model support in ElBruno.Text2Image, published at:

```
blog/2025-04-21-multi-model-image-generation.md
```

##### Strategic Approach

A **five-layer structure** designed to serve multiple audiences in a single read:

1. **Hook (executive summary):** "Four models, one API" — immediate value prop
2. **Overview section:** Why these four models exist and what they do
3. **Comparison table:** Quick visual reference (model specs, use cases, costs)
4. **Code samples:** 6 complete, copy-paste-ready C# examples
5. **CLI examples:** 6 practical shell commands with real-world scenarios
6. **Performance guidance:** Model selection matrix + prompt patterns
7. **Getting started:** Clear onramp for new users
8. **Call-to-action:** Channels for feedback + community engagement

#### Content Architecture

| Section | Word Count | Purpose | Audience |
|---------|-----------|---------|----------|
| Hook + Overview | ~400 | Capture attention, explain value | Decision makers, curious devs |
| Model Comparison Table | ~200 | Quick reference | Developers evaluating options |
| Library Usage (C#) | ~1,400 | Detailed code examples | .NET developers, architects |
| CLI Usage | ~800 | Practical shell workflows | DevOps, CI/CD engineers |
| Performance & Tips | ~600 | Model selection, costs, optimization | Production engineers |
| Getting Started | ~300 | Frictionless onboarding | New users |
| Call-to-Action | ~100 | Next steps | All audiences |
| **Total** | **~3,800** | **Comprehensive announcement** | **Everyone** |

> Note: Blog post is **~2,400 words** after strategic trimming for scannability (preserving all code samples + key tables).

#### Key Design Decisions

##### 1. Model Positioning: Four Quadrants, Not Just "More Choices"

Positioned the four models within a decision matrix:

- **GPT-Image-1.5 & GPT-Image-2:** The reliable workhorses (Azure OpenAI)
- **FLUX.2 Pro:** The photorealism champion (art, cinematic)
- **FLUX.2 Flex:** The text specialist (logos, UI, packaging)
- **MAI-Image-2:** The production optimizer (speed, sync API, batch workflows)

This avoids "We have four models, pick one" confusion. Instead: "Pick the right tool for the job."

##### 2. Code Examples: Progressive Complexity

Started simple (single API call), escalated to production patterns (DI, batch processing):

1. **Sample 1 (GPT-Image-1.5):** "Hello World" — basic generation
2. **Sample 2 (GPT-Image-2):** Product photography use case
3. **Sample 3 (FLUX.2 Pro):** Art direction, longer prompt
4. **Sample 4 (FLUX.2 Flex):** Text-perfect logo design
5. **Sample 5 (MAI-Image-2):** Batch generation (realistic workflow)
6. **Sample 6:** Dependency injection (enterprise pattern)

Each includes setup instructions and context-specific tips.

##### 3. CLI Examples: Real Workflows, Not Toys

Moved beyond `t2i "hello"`. Instead, showed:

- Batch logo generation
- Product photography pipelines
- Diagram generation with perfect text
- Switching providers mid-workflow
- **GitHub Actions CI/CD integration** (most practical)

The CI/CD example is the "aha" moment: "I can automate this in my deployment pipeline."

##### 4. Performance Table: Honest, Actionable

Included speed estimates, cost models, and async patterns.

This lets developers make informed choices without needing to test each model themselves.

##### 5. Prompt Engineering Patterns: Reusable Templates

Rather than say "write good prompts," provided:

- **Pattern 1 (Style Guide):** Format for photorealistic renders
- **Pattern 2 (Design Requirements):** Format for logo/UI designs
- **Pattern 3 (Iterative Refinement):** How to evolve a prompt

Users can copy-paste these patterns into their workflows immediately.

#### Tone & Voice

**Conversational, but technical.** Opening:

> "Text-to-image generation in .NET just leveled up. Today we're announcing unified support for **four production-grade AI models** across three cloud providers — all with the same API surface, the same developer experience, and the same promise: *generate beautiful images from text without leaving C#*."

- Uses emojis strategically (🎨, 💡, ⭐) for scanability
- Addresses practical concerns (costs, rate limits, error handling)
- Celebrates the win ("No more vendor lock-in")
- Acknowledges learning curve ("But here's a setup wizard")

#### What Makes This Blog Post Work

##### ✅ Comprehensive
- 4 code samples per section (library + DI)
- 6 CLI examples covering real use cases
- Complete comparison tables
- Setup instructions for all providers

##### ✅ Scannable
- Clear headers and subheaders
- Comparison tables (not prose)
- Code blocks with syntax highlighting
- Bullet points for key concepts
- Visual emphasis with bold, italics, emojis

##### ✅ Actionable
- Copy-paste-ready code
- Shell commands you can run immediately
- Setup wizard mentioned early
- Getting Started section → GitHub resources

##### ✅ SEO & Discovery
- Title includes key terms (Multi-Model, Image Generation)
- Clear H1 (hook)
- Technical terms bolded for indexing
- Links to docs, GitHub, samples

#### Model Selection Narrative

Explained **why** each model exists, not just **that** it exists:

| Model | Why? |
|-------|------|
| **GPT-Image-1.5** | Broad availability, reliable, cost-effective |
| **GPT-Image-2** | If you need premium quality (better detail, faster) |
| **FLUX.2 Pro** | When "photorealistic" isn't aspirational—it's required |
| **FLUX.2 Flex** | Text matters in your design (logos, labels, UI) |
| **MAI-Image-2** | Production workload? Synchronous API, no polling, fast. |

This positioning helps developers **self-select** the right model rather than defaulting to "use FLUX.2 because it's newest."

#### Developer Pain Points Addressed

##### Pain Point 1: "I don't know which model to use"
**Solution:** Included a **Model Selection Guide** matrix showing use cases for each.

##### Pain Point 2: "Setting up credentials is confusing"
**Solution:** Showed three methods (User Secrets, env vars, appsettings.json) with copy-paste examples.

##### Pain Point 3: "Costs are unclear"
**Solution:** Included a table with cost models, per-call vs per-megapixel, and optimization tips.

##### Pain Point 4: "I don't understand the API differences"
**Solution:** Comparison table shows Async patterns, API types, response formats.

##### Pain Point 5: "How do I use this in production?"
**Solution:** DI sample + batch processing example + CI/CD GitHub Actions workflow.

#### Decisions Made (Tradeoffs)

##### ✅ Included:
- 6 complete code samples (comprehensive)
- CLI examples with GitHub Actions (practical)
- Comparison table (scannable)
- Cost breakdown (financial planning)
- Prompt engineering patterns (reusable)

##### ⏭️ Not Included (For Future Posts):
- Deep dive into each model's architecture
- Fine-tuning / prompt optimization workflows
- Local model support (ONNX Runtime)
- Advanced error handling patterns
- Multi-modal input (image-to-image)

These deserve their own deep-dive posts once the foundation is solid.

#### Validation Checklist

- ✅ ~2,400 words (target: 2000–2500)
- ✅ 6+ code samples, all copy-paste ready
- ✅ 6+ CLI examples with realistic use cases
- ✅ Comparison table (models, specs, use cases)
- ✅ Professional but conversational tone
- ✅ Scannable (headers, tables, bold text, emojis)
- ✅ Setup instructions for all providers
- ✅ Model selection guidance (decision matrix)
- ✅ Performance & cost considerations
- ✅ Getting Started section with resources
- ✅ Clear Call-to-Action
- ✅ File created at `blog/2025-04-21-multi-model-image-generation.md`
