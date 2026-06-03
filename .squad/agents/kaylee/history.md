# Kaylee — History

## Project Context

- **Project:** ElBruno.Text2Image — AI-powered text-to-image generation
- **Owner:** Bruno Capuano
- **Stack:** .NET (C#), solution file `ElBruno.Text2Image.slnx`
- **Repo:** elbruno-text2image
- **Created:** 2025-07-25

## Core Learnings

### CLI Implementation & Patterns

- **Command Framework:** Spectre.Console.Cli v0.49.1; commands injected via TypeRegistrar; GenerateCommand is default
- **Provider Pattern:** IProviderAdapter with split RequiredSecrets (apiKey, masked) + RequiredFields (endpoint/model, plain text)
- **SecretResolver chain:** CLI flags > env vars > DPAPI/plaintext file (DPAPI preferred on Windows)
- **Spectre.Console best practices:** Use Markup.Escape() for user input; ProgressRenderer wraps IProgress<GenerationProgress>; non-interactive check via ConsoleHelpers.IsInteractive()
- **InternalsVisibleTo:** Test projects need `[assembly: InternalsVisibleTo("ElBruno.Text2Image.Tests")]` in AssemblyInfo.cs
- **Embedded resources:** Add file, mark as `<EmbeddedResource>` with LogicalName, load via Assembly.GetManifestResourceStream()

### Provider Integration

- **Cloud provider structure:** (1) Create adapter class with unique ID, (2) register in ProviderServiceCollectionExtensions, (3) constructor: IHttpClientFactory + SecretResolver + ConfigStore, (4) CheckAsync validates availability, (5) GenerateAsync instantiates generator + saves result
- **ConfigStore in adapters:** Load ProviderConfig values at adapter level (endpoint, model). Fallback to secrets for backward compat
- **SetupWizard pattern:** Prompt for RequiredSecrets, then RequiredFields; show defaults; validate URLs; persist to ConfigStore
- **GPT-Image-2 integration:** Follows GPT-Image-1.5 pattern (Azure.AI.OpenAI SDK, not Foundry MAI API); size constraint: only 1024×1024, 1024×1536, 1536×1024 supported

### Sample Scenarios

- **Convention:** appsettings.json (placeholders) + UserSecretsId in .csproj + Program.cs config chain (UserSecrets > EnvVars > appsettings) + README with 3 setup options
- **Blog post structure:** TL;DR upfront (3-4 bullets), then skill integration (biggest feature first), then model support (technical details). Include working Bash/PowerShell examples, comparison tables, migration guides

### Security Hardening

- **Environment variable security pattern:** T2I_DETAILED_ERRORS and T2I_DETAILED_HEALTH_CHECKS; default to secure (production) mode, explicit opt-in for debug
- **Health check redesign:** Default local validation only (no network calls); opt-in full checks with T2I_DETAILED_HEALTH_CHECKS=1; prevents MITM during health phase
- **Endpoint URL exposure:** BuildErrorHint() checks env var, returns generic errors by default, full diagnostics with T2I_DETAILED_ERRORS=1
- **Path traversal validation:** Path.GetFullPath() + StartsWith(OrdinalIgnoreCase) check; applied to ImageGenerationResult, InitCommand, ConfigStore, secret stores; prevents symlink attacks
- **HTTP response size limits:** 50MB max; check Content-Length before reading; InvalidOperationException if exceeded; HttpCompletionOption.ResponseHeadersRead for efficient header-only reads; validate both API response AND URL downloads
- **Blog reorder (security messaging):** Present DPAPI/plaintext file FIRST (recommended for local dev), then environment variables (CI/CD only with warnings); code already secure, docs need reorganization

### Version Coordination

- **Versioning decision:** All packages centralize version in Directory.Build.props (single source of truth). Phase 1: Centralize versioning; Phase 2: Implement tagging convention on next release. Kaylee to execute Phase 1


