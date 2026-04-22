# GPT-Image-2 Integration

**Date:** 2026-04-21  
**Decided by:** Kaylee (Core Dev)  
**Status:** Implemented  

## Context

Bruno requested integration of the new GptImage2Generator (implemented by Wash in Foundry project) into the CLI tooling and sample scenarios.

## Decision

Integrated GPT-Image-2 as a full-featured provider in the CLI tool following established patterns:

### 1. CLI Provider Adapter
- **File:** `src/ElBruno.Text2Image.Cli/Providers/FoundryGptImage2Adapter.cs`
- **Provider ID:** `foundry-gpt-image-2`
- **Display Name:** "GPT-Image-2 (Azure OpenAI)"
- **Configuration:**
  - RequiredSecrets: `["apiKey"]` (stored in secret store)
  - RequiredFields: `["endpoint", "model"]` (stored in ConfigStore)
  - Default deployment name: `gpt-image-2`
  - Default model name: `GPT-Image-2`

### 2. DI Registration
- Registered in `ProviderServiceCollectionExtensions.cs` as singleton
- Added to ProviderRegistry alongside existing Foundry providers (FLUX.2, MAI-Image-2, GPT-Image-1.5)

### 3. Sample Scenario
- **Location:** `src/samples/scenario-16-gpt-image-2-cloud/`
- **Structure:** Program.cs, README.md, appsettings.json, .csproj
- **UserSecretsId:** `elbruno-text2image-gpt-image-2`
- **Demonstrates:** Three generation scenarios (1024×1024, 1792×1024 landscape, abstract art)
- **Configuration:** Supports user secrets, environment variables, and appsettings.json

## Implementation Pattern

Followed the exact pattern established by GPT-Image-1.5 (FoundryGptImage1p5Adapter):
1. Adapter reads endpoint from ConfigStore (with backward-compat fallback to SecretResolver)
2. Adapter reads model/deployment name from ConfigStore with sensible defaults
3. Adapter reads apiKey from SecretResolver
4. CheckAsync validates endpoint reachability with HEAD request
5. GenerateAsync instantiates GptImage2Generator, calls GenerateAsync, saves to output path

## Rationale

- **Consistency:** Identical structure to existing GPT-Image-1.5 integration ensures maintainability
- **Discoverability:** Provider appears in `t2i providers` list immediately after registration
- **Usability:** Sample scenario provides ready-to-run example for users with Azure OpenAI credentials
- **Security:** Follows established credential management patterns (secrets in SecretStore, config in ConfigStore)

## Verification

- ✅ CLI project builds successfully (`dotnet build --no-restore`)
- ✅ Provider appears in `t2i providers` output as `foundry-gpt-image-2`
- ✅ Sample scenario builds for both net8.0 and net10.0 targets
- ✅ Follows IProviderAdapter interface contract

## Impact

- **Users:** Can now select `foundry-gpt-image-2` as a provider via CLI
- **Developers:** Sample scenario demonstrates proper usage of GptImage2Generator
- **Maintainers:** Adapter follows established patterns, minimizing special-case code

## Notes

- GptImage2Generator itself was implemented by Wash (backend infrastructure)
- This work focused on exposing it through CLI and providing usage example
- Size constraints match GPT-Image-1.5: 1024×1024, 1792×1024, 1024×1792 (fixed aspect ratios)
